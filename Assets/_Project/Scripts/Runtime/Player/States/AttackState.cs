using System;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using GodOfWar.Player.Diagnostics;
using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 약공격·강공격 콤보 상태. 공격 한 타마다 이 상태에 다시 진입한다.
    /// 공격은 항상 카메라 정면으로 나가며, 선딜레이 동안 카메라를 돌려 조준을 고칠 수 있다.
    /// 공격 클립 → 복귀 클립 순으로 진행하고, 연계 구간 안에서 공격 버튼을 누르면 다음 타로 이어진다.
    /// 공격 클립이 끝나기 전의 입력은 예약했다가 클립 끝에서, 끝난 뒤의 입력은 즉시 잇는다.
    /// 연계 구간이 열리기 전의 입력은 액션 라우터의 선입력이 맡고, 이 예약은 이미 받은 다음 타의 실행 시점만 미룬다.
    /// 약공격·강공격 버튼으로 진입한다. 선딜레이·스윙 동안은 다른 액션이 끼어들 수 없고,
    /// 후딜레이(<see cref="AttackAction.RecoveryStart"/> 이후)에는 회피 등으로 캔슬할 수 있다.
    /// 연계 구간이 끝난 뒤 예약된 타 없이 이동 입력이 있으면 복귀 클립을 끝까지 기다리지 않고 이동으로 돌아간다.
    /// 무기 판정은 타격 구간(<see cref="AttackAction.HitWindowStart"/>~<see cref="AttackAction.HitWindowEnd"/>) 동안만 열고,
    /// 어떤 경로로 상태를 나가든 닫는다.
    /// </summary>
    public sealed class AttackState : IState, ICancellable, IChainable, IComboWindow, IActionProgress
    {
        private static readonly int LocomotionHash = Animator.StringToHash("Locomotion");

        private readonly IPlayerStateContext _context;
        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly IPlayerInput _input;
        private readonly CharacterMotor _motor;
        private readonly Animator _animator;
        private readonly AttackSettings _settings;
        private readonly Hitbox _hitbox;
        private readonly ActionMovement _movement = new ActionMovement();

        private LocomotionState _locomotion;

        // 다음 Enter에서 시작할 액션과 그 액션을 연 버튼.
        private AttackAction _pendingAction;
        private PlayerAction _pendingInput;

        private AttackAction _action;
        private PlayerAction _chainInput;
        private float _elapsed;
        private bool _isReturning;
        private AttackAction _queuedAction;
        private PlayerAction _queuedInput;

        public AttackState(IPlayerStateContext context, AttackSettings settings)
        {
            _context = context;
            _machine = context.Machine;
            _locator = context.States;
            _input = context.Input;
            _motor = context.Motor;
            _animator = context.Animator;
            _settings = settings;
            RequireEntry(PlayerAction.LightAttack);
            RequireEntry(PlayerAction.HeavyAttack);
            _hitbox = context.Hitbox;

            // 진입한 버튼이 콤보의 1타와 이후 연계를 정하므로, 전이 직전에 그 버튼으로 준비한다.
            context.Router.Declare(new ActionTrigger(
                PlayerAction.LightAttack, this, () => true, prepare: e => Prepare(e.Action)));
            context.Router.Declare(new ActionTrigger(
                PlayerAction.HeavyAttack, this, () => true, prepare: e => Prepare(e.Action)));
        }

        // 예약된 다음 타가 있어도 연다. 회피는 연계가 아니라 라우터로 넘어가, 상태를 나가면서 예약이 사라진다.
        public bool IsCancelWindowOpen => _action.IsInRecovery(_elapsed);

        public bool IsComboWindowOpen => _action.IsInComboWindow(_elapsed);

        public bool HasQueuedFollowUp => _queuedAction != null;

        public string ActionName => _action.name;

        public float Elapsed => _elapsed;

        // 이어지는 입력이 없을 때 복귀 클립까지 재생한 길이다.
        public float Duration => _action.Duration + _action.ReturnDuration;

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
            // 1타가 비어 있으면 생성 시점에 이미 실패했다.
            SetPending(_settings.GetEntry(input), input);
        }

        private void RequireEntry(PlayerAction input)
        {
            if (_settings.GetEntry(input) == null)
            {
                throw new InvalidOperationException($"'{_settings.name}' has no entry attack for {input}.");
            }
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

            _animator.CrossFadeInFixedTime(Animator.StringToHash(_action.StateName), _action.CrossFadeDuration);
            // 공격 궤적은 몸 정면 기준으로 구워져 있으므로 몸이 바라보는 방향으로 내딛는다.
            _movement.Begin(_action.Movement, _motor.transform.eulerAngles.y);
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            UpdateHitbox();

            // 선딜레이 동안은 카메라 정면을 계속 따라가 조준을 고칠 수 있고, 스윙에 들어가면 방향이 고정된다.
            // 몸은 회전 속도만큼 늦게 따라가므로, 궤적은 카메라가 아니라 몸의 현재 정면에 맞춘다.
            if (_elapsed < _action.TrackingEnd)
            {
                _motor.RotateTowards(_context.MovementYaw, _settings.TurnSpeed * deltaTime);
                _movement.SetYaw(_motor.transform.eulerAngles.y);
            }

            _motor.Move(_movement.Tick(deltaTime), deltaTime);

            // 연계 구간의 마지막 순간까지는 공격 입력이 우선이므로, 구간이 끝난 뒤부터 이동으로 일찍 복귀한다.
            if (_queuedAction == null
                && _elapsed > _action.ComboWindowEnd
                && _input.Move.magnitude >= _settings.MinInput)
            {
                ReturnToLocomotion();
                return;
            }

            if (_isReturning)
            {
                if (_elapsed >= _action.Duration + _action.ReturnDuration)
                {
                    ReturnToLocomotion();
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
            // 캔슬·연계·복귀 모두 여기를 지나므로, 판정 구간 도중에 나가도 판정이 열린 채 남지 않는다.
            _hitbox.Close();
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

        /// <summary>
        /// 타격 구간에 맞춰 무기 판정을 열고 닫는다. 실제 검사는 애니메이션이 칼을 옮긴 뒤 히트박스가 한다.
        /// 경과 시간은 줄지 않으므로 한 타에서 판정은 한 번만 열린다.
        /// </summary>
        private void UpdateHitbox()
        {
            bool isInWindow = _action.IsInHitWindow(_elapsed);
            if (isInWindow && !_hitbox.IsOpen)
            {
                _hitbox.Open(_motor.transform, _action.HitStrength);
            }
            else if (!isInWindow && _hitbox.IsOpen)
            {
                _hitbox.Close();
            }
        }

        /// <summary>공격 버튼(약·강)을 누른 순간인지 판정한다.</summary>
        private static bool IsAttackPress(PlayerActionEvent e)
        {
            return e.Phase == ActionPhase.Pressed
                && (e.Action == PlayerAction.LightAttack || e.Action == PlayerAction.HeavyAttack);
        }

        private void ReturnToLocomotion()
        {
            _animator.CrossFadeInFixedTime(LocomotionHash, _action.CrossFadeDuration);
            _machine.ChangeState(_locomotion);
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
    }
}
