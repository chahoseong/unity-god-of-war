using System;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 가드 상태. Guard 버튼이 눌려 있는 동안 유지되고, 떼면 <see cref="LocomotionState"/>로 돌아간다.
    /// 다리는 이동 클립으로 걷기 속도의 스트레이프를 하고, 상체는 Guard 레이어에서 가드 자세를 덮어쓴다.
    /// 항상 이동 기준(카메라) 정면을 바라본다.
    /// 눌려 있는 Guard 입력(Held)으로 진입하므로, 다른 행동 중에 누르고 있으면 받을 수 있게 된 순간 가드한다.
    /// 가드 중에는 다른 액션이 끼어들 수 없고, 그동안 누른 버튼은 선입력으로 남아 가드를 풀면 실행된다.
    /// 공격을 막으면 상태를 바꾸지 않고 <see cref="Block"/>으로 반응한다.
    /// </summary>
    public sealed class GuardState : IState, ICancellable, IChainable, IDefensible
    {

        private readonly IPlayerStateContext _context;
        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly IPlayerInput _input;
        private readonly CharacterMotor _motor;
        private readonly PlayerAnimator _animator;
        private readonly LocomotionSettings _locomotionSettings;
        private readonly GuardSettings _settings;

        // 이보다 가까운 공격자는 방향을 정할 수 없는 것으로 본다.
        private const float MinDirectionSqr = 0.0001f;

        private LocomotionState _locomotion;

        // 막는 동작이 끝날 때까지 남은 시간. 그동안은 이동 입력을 받지 않는다.
        private float _blockRemaining;

        // 막았을 때의 밀림. 경과 시간이 밀림 시간에 이르면 밀리지 않는다.
        private Vector3 _pushDirection;
        private float _pushElapsed;

        public GuardState(IPlayerStateContext context, GuardSettings settings, LocomotionSettings locomotionSettings)
        {
            _context = context;
            _machine = context.Machine;
            _locator = context.States;
            _input = context.Input;
            _motor = context.Motor;
            _animator = context.Animator;
            _locomotionSettings = locomotionSettings;
            _settings = settings;
            if (_settings.BlockClip == null)
            {
                throw new InvalidOperationException(
                    $"'{_settings.name}' has no {nameof(GuardSettings.BlockClip)} assigned.");
            }

            // Held는 눌려 있다는 사실 자체이므로 시작 조건이 없다. 누른 프레임에도 Held가 오므로 Pressed는 선언하지 않는다.
            context.Router.Declare(new ActionTrigger(PlayerAction.Guard, this, () => true, phase: ActionPhase.Held));
        }

        public bool IsCancelWindowOpen => false;

        // 가드를 올린 순간부터 막는다.
        public DefenseStance DefenseStance => DefenseStance.Guarding;

        public void Enter()
        {
            // 생성 시점에는 다른 상태가 아직 등록되지 않았을 수 있어 첫 진입에서 찾는다.
            _locomotion ??= _locator.Get<LocomotionState>();
            _blockRemaining = 0f;
            _pushElapsed = _settings.BlockPushDuration;
            _animator.RaiseGuard(_settings.CrossFadeDuration);
        }

        /// <summary>
        /// 공격을 막았을 때의 반응. 상체에 막는 동작을 재생하고, 공격 반대쪽으로 잠깐 밀려난 뒤
        /// 막는 동작이 끝날 때까지 제자리에서 버틴다. 가드 자세로 돌아가는 동작 중에는 다시 걸을 수 있다.
        /// 숄더뷰에서 몸과 카메라가 어긋나지 않도록 몸은 돌리지 않는다.
        /// 상태는 그대로라 계속 막을 수 있고, 반응 중에 다시 막으면 처음부터 다시 반응한다.
        /// </summary>
        public void Block(in AttackInfo attack)
        {
            _animator.PlayBlock(_settings.CrossFadeDuration);
            _blockRemaining = _settings.BlockClip.length;
            _pushElapsed = _settings.BlockPushDuration;

            Vector3 toAttacker = attack.Attacker.position - _motor.transform.position;
            toAttacker.y = 0f;
            // 공격자와 겹쳐 밀릴 방향을 정할 수 없으면 밀리지 않는다.
            if (toAttacker.sqrMagnitude < MinDirectionSqr)
            {
                return;
            }

            _pushDirection = -toAttacker.normalized;
            _pushElapsed = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (!_input.IsHeld(PlayerAction.Guard))
            {
                _machine.ChangeState(_locomotion);
                return;
            }

            float referenceYaw = _context.MovementYaw;
            _blockRemaining -= deltaTime;

            if (_pushElapsed < _settings.BlockPushDuration)
            {
                _motor.Move(TickPush(deltaTime), deltaTime);
            }
            else if (_blockRemaining > 0f)
            {
                // 밀림이 끝난 뒤에도 막는 동작이 끝날 때까지 제자리에서 버틴다.
                _motor.Move(Vector3.zero, deltaTime);
            }
            else
            {
                Vector2 move = _input.Move;
                Vector3 target = Quaternion.Euler(0f, referenceYaw, 0f)
                    * new Vector3(move.x, 0f, move.y) * _settings.WalkSpeed;
                Vector3 current = _motor.HorizontalVelocity;
                float rate = target.sqrMagnitude > current.sqrMagnitude
                    ? _locomotionSettings.Acceleration
                    : _locomotionSettings.Deceleration;

                _motor.Move(Vector3.MoveTowards(current, target, rate * deltaTime), deltaTime);
            }

            _motor.RotateTowards(referenceYaw, _locomotionSettings.TurnSpeed * deltaTime);
        }

        /// <summary>
        /// 이번 프레임의 밀림 속도. 시작이 가장 빠르고 끝에서 멈추도록 이동 거리를 d·(2u − u²) (u = 경과/시간) 곡선으로 나눈다.
        /// </summary>
        private Vector3 TickPush(float deltaTime)
        {
            float duration = _settings.BlockPushDuration;
            float from = PushProgress(_pushElapsed / duration);
            _pushElapsed = Mathf.Min(_pushElapsed + deltaTime, duration);
            float to = PushProgress(_pushElapsed / duration);

            return deltaTime > 0f
                ? _pushDirection * ((to - from) * _settings.BlockPushDistance / deltaTime)
                : Vector3.zero;
        }

        private static float PushProgress(float u)
        {
            return u * (2f - u);
        }

        public void Exit()
        {
            // 해제든 캔슬이든 상체가 가드 자세에 남지 않게 내린다.
            _animator.LowerGuard(_settings.CrossFadeDuration);
        }

        public bool TryChain(PlayerActionEvent e)
        {
            // 가드 중에 오는 Guard 입력은 이미 처리 중이다. 라우터로 넘기면 매 프레임 가드에 다시 진입한다.
            return e.Action == PlayerAction.Guard
                && (e.Phase == ActionPhase.Pressed || e.Phase == ActionPhase.Held);
        }
    }
}
