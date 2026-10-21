using System;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 약공격·강공격 콤보 상태. 공격 한 타마다 이 상태에 다시 진입한다.
    /// 공격 클립 → 복귀 클립 순으로 진행하고, 연계 구간 안에서 공격 버튼을 누르면 다음 타로 이어진다.
    /// 공격 클립이 끝나기 전의 입력은 예약했다가 클립 끝에서, 끝난 뒤의 입력은 즉시 잇는다.
    /// </summary>
    public sealed class AttackState : IState
    {
        private static readonly int LocomotionHash = Animator.StringToHash("Locomotion");

        private readonly PlayerController _owner;
        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly IPlayerInput _input;
        private readonly CharacterMotor _motor;
        private readonly Animator _animator;
        private readonly AttackSettings _settings;
        private readonly ActionMovement _movement = new ActionMovement();

        private LocomotionState _locomotion;

        // 다음 Enter에서 시작할 액션과 그 액션을 연 버튼.
        private AttackAction _pendingAction;
        private PlayerAction _pendingInput;

        private AttackAction _action;
        private PlayerAction _chainInput;
        private float _elapsed;
        private float _targetYaw;
        private bool _isReturning;
        private AttackAction _queuedAction;
        private PlayerAction _queuedInput;

        public AttackState(PlayerController owner, StateMachine machine)
        {
            _owner = owner;
            _machine = machine;
            _locator = owner;
            _input = owner.GetRequiredComponent<IPlayerInput>();
            _motor = owner.GetRequiredComponent<CharacterMotor>();
            _animator = owner.Animator;
            _settings = owner.AttackSettings;
        }

        /// <summary>공격 버튼(약·강)을 누른 순간인지 판정한다.</summary>
        public static bool IsAttackPress(PlayerActionEvent e)
        {
            return e.Phase == ActionPhase.Pressed
                && (e.Action == PlayerAction.LightAttack || e.Action == PlayerAction.HeavyAttack);
        }

        /// <summary>
        /// 연계 구간 안에서 <paramref name="pressed"/>를 눌렀을 때 이어질 액션을 고른다.
        /// 지금 콤보를 연 버튼과 같고 다음 타가 있으면 다음 타로, 아니면 누른 버튼의 1타로 시작한다.
        /// </summary>
        public static AttackAction ResolveFollowUp(
            AttackAction current, PlayerAction chainInput, PlayerAction pressed, AttackSettings settings)
        {
            if (pressed == chainInput && current.Next != null)
            {
                return current.Next;
            }

            return settings.GetEntry(pressed);
        }

        /// <summary><paramref name="input"/> 버튼의 1타로 다음 진입을 준비한다. 진입 측이 호출한 뒤 이 상태로 전환한다.</summary>
        public void Prepare(PlayerAction input)
        {
            AttackAction entry = _settings.GetEntry(input);
            if (entry == null)
            {
                throw new InvalidOperationException(
                    $"'{_owner.name}' has no entry attack for {input}. Assign it in Attack Settings.");
            }

            SetPending(entry, input);
        }

        public void Enter()
        {
            // 생성 시점에는 다른 상태가 아직 등록되지 않았을 수 있어 첫 진입에서 찾는다.
            _locomotion ??= _locator.Get<LocomotionState>();

            _action = _pendingAction ?? throw new InvalidOperationException(
                $"{nameof(AttackState)} was entered without an action. Call {nameof(Prepare)} first.");
            _chainInput = _pendingInput;
            _pendingAction = null;

            _elapsed = 0f;
            _isReturning = false;
            _queuedAction = null;

            // 타마다 그 순간의 입력으로 방향을 다시 정해, 타격 사이에 방향을 틀 수 있게 한다.
            _targetYaw = GetTargetYaw();
            _animator.CrossFadeInFixedTime(Animator.StringToHash(_action.StateName), _action.CrossFadeDuration);
            _movement.Begin(_action.Movement, _targetYaw);

            _input.ActionTriggered += OnActionTriggered;
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;

            if (_elapsed < _action.TrackingEnd)
            {
                _motor.RotateTowards(_targetYaw, _settings.TurnSpeed * deltaTime);
            }

            _motor.Move(_movement.Tick(deltaTime), deltaTime);

            if (_isReturning)
            {
                if (_elapsed >= _action.Duration + _action.ReturnDuration)
                {
                    _animator.CrossFadeInFixedTime(LocomotionHash, _action.CrossFadeDuration);
                    _machine.ChangeState(_locomotion);
                }

                return;
            }

            if (_elapsed >= _action.Duration)
            {
                if (_queuedAction != null)
                {
                    Chain(_queuedAction, _queuedInput);
                    return;
                }

                _isReturning = true;
                _animator.CrossFadeInFixedTime(
                    Animator.StringToHash(_action.ReturnStateName), _action.CrossFadeDuration);
                // 복귀 클립은 공격을 마친 순간의 캐릭터 정면을 기준으로 재생된다.
                _movement.Begin(_action.ReturnMovement, _motor.transform.eulerAngles.y);
            }
        }

        public void Exit()
        {
            _input.ActionTriggered -= OnActionTriggered;
        }

        private void OnActionTriggered(PlayerActionEvent e)
        {
            // 구간 밖 입력은 버리고, 회피·가드 등 다른 입력도 받지 않는다.
            if (!IsAttackPress(e) || !_action.IsInComboWindow(_elapsed))
            {
                return;
            }

            AttackAction next = ResolveFollowUp(_action, _chainInput, e.Action, _settings);
            if (_isReturning)
            {
                Chain(next, e.Action);
                return;
            }

            // 공격 클립이 끝나야 다음 타의 시작 포즈와 맞물리므로 그때까지 미룬다. 나중 입력이 앞선 예약을 덮는다.
            _queuedAction = next;
            _queuedInput = e.Action;
        }

        private void Chain(AttackAction next, PlayerAction input)
        {
            SetPending(next, input);
            _machine.ChangeState(this);
        }

        private void SetPending(AttackAction action, PlayerAction input)
        {
            _pendingAction = action;
            _pendingInput = input;
        }

        /// <summary>이동 입력이 충분하면 카메라 기준 입력 방향, 아니면 카메라 정면의 월드 Yaw(도).</summary>
        private float GetTargetYaw()
        {
            float referenceYaw = _owner.MovementYaw;
            Vector2 move = _input.Move;
            if (move.magnitude < _settings.MinInput)
            {
                return referenceYaw;
            }

            return referenceYaw + Mathf.Atan2(move.x, move.y) * Mathf.Rad2Deg;
        }
    }
}
