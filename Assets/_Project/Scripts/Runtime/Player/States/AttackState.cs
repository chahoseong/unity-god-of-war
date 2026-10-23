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
    /// 약공격·강공격 버튼으로 진입하며, 공격 동안 다른 액션은 끼어들 수 없다.
    /// </summary>
    public sealed class AttackState : IState, ICancellable, IChainable
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

            // 진입한 버튼이 콤보의 1타와 이후 연계를 정하므로, 전이 직전에 그 버튼으로 준비한다.
            owner.Router.Declare(new ActionTrigger(
                PlayerAction.LightAttack, this, () => true, prepare: e => Prepare(e.Action)));
            owner.Router.Declare(new ActionTrigger(
                PlayerAction.HeavyAttack, this, () => true, prepare: e => Prepare(e.Action)));
        }

        public bool IsCancelWindowOpen => false;

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

        /// <summary><paramref name="input"/> 버튼의 1타로 다음 진입을 준비한다. 진입 트리거가 전이 직전에 호출한다.</summary>
        private void Prepare(PlayerAction input)
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
        }

        public bool TryChain(PlayerActionEvent e)
        {
            // 연계 구간 밖의 공격과 회피 등 다른 입력은 라우터의 판정에 맡긴다.
            if (!IsAttackPress(e) || !_action.IsInComboWindow(_elapsed))
            {
                return false;
            }

            AttackAction next = ResolveFollowUp(_action, _chainInput, e.Action, _settings);
            if (_isReturning)
            {
                Chain(next, e.Action);
                return true;
            }

            // 공격 클립이 끝나야 다음 타의 시작 포즈와 맞물리므로 그때까지 미룬다. 나중 입력이 앞선 예약을 덮는다.
            _queuedAction = next;
            _queuedInput = e.Action;
            return true;
        }

        /// <summary>공격 버튼(약·강)을 누른 순간인지 판정한다.</summary>
        private static bool IsAttackPress(PlayerActionEvent e)
        {
            return e.Phase == ActionPhase.Pressed
                && (e.Action == PlayerAction.LightAttack || e.Action == PlayerAction.HeavyAttack);
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
