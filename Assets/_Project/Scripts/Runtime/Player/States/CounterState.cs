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
    /// 패링 카운터 상태. <see cref="ParryState"/>가 넘긴 상대에게 <see cref="CounterAction"/> 한 동작을 한다.
    /// 타격 시각까지 상대 쪽으로 돌며, 클립 이동에 더해 타격 시각에 닿는 거리가 되도록 다가간다(물러나지는 않는다).
    /// 타격 시각에 칼날 판정 없이 상대에게 직접 타격을 전하므로 반드시 맞는다.
    /// 연계 구간 안에서 공격 버튼을 누르면 클립이 끝날 때 그 버튼의 1타로 <see cref="AttackState"/>에 이어진다.
    /// 후딜레이부터는 회피 등으로 캔슬할 수 있고, 복귀 클립 없이 클립이 끝나면 이동으로 돌아간다.
    /// 연계 구간이 끝난 뒤 예약된 타 없이 이동 입력이 있으면 클립 끝을 기다리지 않고 이동으로 돌아간다.
    /// 패링 동작에서만 이어지므로 진입 트리거를 선언하지 않는다.
    /// </summary>
    public sealed class CounterState : IState, ICancellable, IChainable, IActionProgress, IDefensible
    {
        // 이보다 가까운 상대는 방향을 정할 수 없는 것으로 본다.
        private const float MinDirectionSqr = 0.0001f;

        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly IPlayerInput _input;
        private readonly CharacterMotor _motor;
        private readonly PlayerAnimator _animator;
        private readonly Hitbox _hitbox;
        private readonly CounterAction _action;
        private readonly float _minInput;
        private readonly ActionMovement _movement = new ActionMovement();

        private LocomotionState _locomotion;
        private AttackState _attack;

        // 다음 Enter에서 칠 상대.
        private Transform _pendingTarget;

        private Transform _target;
        private IHittable _hittable;
        private float _elapsed;
        private float _approachSpeed;
        private bool _hasStruck;
        private bool _hasQueuedFollowUp;
        private PlayerAction _queuedInput;

        public CounterState(IPlayerStateContext context, GuardSettings settings, AttackSettings attackSettings)
        {
            _machine = context.Machine;
            _locator = context.States;
            _input = context.Input;
            _motor = context.Motor;
            _animator = context.Animator;
            _hitbox = context.Hitbox;
            _action = settings.Counter;
            _minInput = attackSettings.MinInput;
            if (_action == null || _action.Clip == null || _action.Movement == null)
            {
                throw new InvalidOperationException(
                    $"'{settings.name}' has no {nameof(GuardSettings.Counter)} with a clip and movement assigned.");
            }
        }

        public bool IsCancelWindowOpen => _action.IsInRecovery(_elapsed);

        public DefenseStance DefenseStance => DefenseStance.Open;

        public string ActionName => _action.name;

        public float Elapsed => _elapsed;

        public float Duration => _action.Duration;

        /// <summary>다음 진입에서 칠 상대를 정한다. 전이 직전에 호출한다.</summary>
        public void Prepare(Transform target)
        {
            _pendingTarget = target != null ? target : throw new ArgumentNullException(nameof(target));
        }

        public void Enter()
        {
            // 생성 시점에는 다른 상태가 아직 등록되지 않았을 수 있어 첫 진입에서 찾는다.
            _locomotion ??= _locator.Get<LocomotionState>();
            _attack ??= _locator.Get<AttackState>();

            _target = _pendingTarget != null ? _pendingTarget : throw new InvalidOperationException(
                $"{nameof(CounterState)} was entered without a target. Call {nameof(Prepare)} first.");
            _pendingTarget = null;
            _hittable = _target.TryGetComponent(out IHittable hittable) ? hittable : null;

            _elapsed = 0f;
            _hasStruck = false;
            _hasQueuedFollowUp = false;

            _animator.CrossFade(Animator.StringToHash(_action.StateName), _action.CrossFadeDuration);
            _movement.Begin(_action.Movement, _motor.transform.eulerAngles.y);

            // 클립 이동만으로 닿지 않는 만큼을 타격 시각까지 고르게 나눠 다가간다.
            float shortfall = Mathf.Clamp(HorizontalOffsetToTarget().magnitude - _action.ContactDistance, 0f, _action.MaxApproach);
            _approachSpeed = _action.StrikeTime > 0f ? shortfall / _action.StrikeTime : 0f;
        }

        public void Tick(float deltaTime)
        {
            float previous = _elapsed;
            _elapsed += deltaTime;

            Vector3 velocity = TickTowardTarget(previous, deltaTime);
            _motor.Move(_movement.Tick(deltaTime) + velocity, deltaTime);

            if (!_hasStruck && _elapsed >= _action.StrikeTime)
            {
                Strike();
            }

            // 연계 구간의 마지막 순간까지는 공격 입력이 우선이므로, 구간이 끝난 뒤부터 이동으로 일찍 복귀한다.
            if (!_hasQueuedFollowUp
                && _elapsed > _action.ComboWindowEnd
                && _input.Move.magnitude >= _minInput)
            {
                ReturnToLocomotion();
                return;
            }

            if (_elapsed < _action.Duration)
            {
                return;
            }

            if (_hasQueuedFollowUp)
            {
                _attack.PrepareEntry(_queuedInput);
                _machine.ChangeState(_attack);
                return;
            }

            ReturnToLocomotion();
        }

        public void Exit()
        {
        }

        public bool TryChain(PlayerActionEvent e)
        {
            // 연계 구간 밖의 공격과 회피 등 다른 입력은 라우터의 판정에 맡긴다.
            if (e.Phase != ActionPhase.Pressed
                || (e.Action != PlayerAction.LightAttack && e.Action != PlayerAction.HeavyAttack)
                || !_action.IsInComboWindow(_elapsed))
            {
                return false;
            }

            // 카운터 클립이 끝나야 1타의 시작 포즈와 맞물리므로 그때까지 미룬다. 나중 입력이 앞선 예약을 덮는다.
            _hasQueuedFollowUp = true;
            _queuedInput = e.Action;
            return true;
        }

        /// <summary>
        /// 타격 시각 전이면 이번 프레임에 상대 쪽으로 돌고 다가갈 속도를 정한다. 몸은 타격 시각에 상대를 바라보도록 남은 각도를
        /// 남은 시간에 나눠 돈다. 상대가 계속 밀려나므로 방향은 매 프레임 다시 잡는다.
        /// </summary>
        private Vector3 TickTowardTarget(float previous, float deltaTime)
        {
            float remaining = _action.StrikeTime - previous;
            if (_target == null || remaining <= 0f || deltaTime <= 0f)
            {
                return Vector3.zero;
            }

            Vector3 offset = HorizontalOffsetToTarget();
            if (offset.sqrMagnitude < MinDirectionSqr)
            {
                return Vector3.zero;
            }

            float windup = Mathf.Min(deltaTime, remaining);
            float targetYaw = Quaternion.LookRotation(offset).eulerAngles.y;
            float angle = Mathf.Abs(Mathf.DeltaAngle(_motor.transform.eulerAngles.y, targetYaw));
            _motor.RotateTowards(targetYaw, angle * windup / remaining);
            // 클립 이동은 몸 정면 기준이라 돈 만큼 따라 돌린다.
            _movement.SetYaw(_motor.transform.eulerAngles.y);

            // 타격 시각을 넘긴 프레임은 넘기 전까지만 다가간다.
            return offset.normalized * (_approachSpeed * windup / deltaTime);
        }

        private void Strike()
        {
            _hasStruck = true;
            // 인터페이스 참조는 파괴된 Unity 오브젝트를 == null로 거를 수 없어 Unity 오브젝트로 확인한다.
            if (_hittable == null || (_hittable is UnityEngine.Object hittable && hittable == null))
            {
                return;
            }

            _hitbox.Strike(_hittable, _motor.transform, _action.HitStrength);
        }

        private Vector3 HorizontalOffsetToTarget()
        {
            if (_target == null)
            {
                return Vector3.zero;
            }

            Vector3 offset = _target.position - _motor.transform.position;
            offset.y = 0f;
            return offset;
        }

        private void ReturnToLocomotion()
        {
            _animator.ReturnToLocomotion(_action.CrossFadeDuration);
            _machine.ChangeState(_locomotion);
        }
    }
}
