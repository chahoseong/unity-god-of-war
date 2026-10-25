using GodOfWar.Characters;
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
    /// </summary>
    public sealed class GuardState : IState, ICancellable, IChainable
    {
        private static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
        private static readonly int GuardBeginHash = Animator.StringToHash("Guard_Begin");
        private static readonly int GuardEndHash = Animator.StringToHash("Guard_End");

        private readonly PlayerController _owner;
        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly IPlayerInput _input;
        private readonly CharacterMotor _motor;
        private readonly Animator _animator;
        private readonly LocomotionSettings _locomotionSettings;
        private readonly GuardSettings _settings;

        private LocomotionState _locomotion;
        private int _guardLayer;

        public GuardState(PlayerController owner, StateMachine machine)
        {
            _owner = owner;
            _machine = machine;
            _locator = owner;
            _input = owner.GetRequiredComponent<IPlayerInput>();
            _motor = owner.GetRequiredComponent<CharacterMotor>();
            _animator = owner.Animator;
            _locomotionSettings = owner.LocomotionSettings;
            _settings = owner.GuardSettings;

            // Held는 눌려 있다는 사실 자체이므로 시작 조건이 없다. 누른 프레임에도 Held가 오므로 Pressed는 선언하지 않는다.
            owner.Router.Declare(new ActionTrigger(PlayerAction.Guard, this, () => true, phase: ActionPhase.Held));
        }

        public bool IsCancelWindowOpen => false;

        public void Enter()
        {
            // 생성 시점에는 다른 상태와 애니메이터 레이어가 아직 준비되지 않았을 수 있어 첫 진입에서 찾는다.
            _locomotion ??= _locator.Get<LocomotionState>();
            _guardLayer = _owner.GuardLayer;

            // 공격 후딜에서 캔슬되면 다리가 공격 클립에 남아 있으므로 이동 클립으로 되돌린다.
            // 이미 이동 클립이면 블렌드 트리를 처음부터 다시 틀지 않는다.
            if (_animator.GetCurrentAnimatorStateInfo(0).shortNameHash != LocomotionHash
                || _animator.IsInTransition(0))
            {
                _animator.CrossFadeInFixedTime(LocomotionHash, _settings.CrossFadeDuration, 0);
            }

            _animator.CrossFadeInFixedTime(GuardBeginHash, _settings.CrossFadeDuration, _guardLayer);
        }

        public void Tick(float deltaTime)
        {
            if (!_input.IsHeld(PlayerAction.Guard))
            {
                _machine.ChangeState(_locomotion);
                return;
            }

            float referenceYaw = _owner.MovementYaw;
            Vector2 move = _input.Move;

            Vector3 target = Quaternion.Euler(0f, referenceYaw, 0f)
                * new Vector3(move.x, 0f, move.y) * _settings.WalkSpeed;
            Vector3 current = _motor.HorizontalVelocity;
            float rate = target.sqrMagnitude > current.sqrMagnitude
                ? _locomotionSettings.Acceleration
                : _locomotionSettings.Deceleration;

            _motor.Move(Vector3.MoveTowards(current, target, rate * deltaTime), deltaTime);
            _motor.RotateTowards(referenceYaw, _locomotionSettings.TurnSpeed * deltaTime);
        }

        public void Exit()
        {
            // 해제든 캔슬이든 상체가 가드 자세에 남지 않게 내린다. 전신 클립으로 캔슬되면 레이어가 꺼져 보이지 않는다.
            _animator.CrossFadeInFixedTime(GuardEndHash, _settings.CrossFadeDuration, _guardLayer);
        }

        public bool TryChain(PlayerActionEvent e)
        {
            // 가드 중에 오는 Guard 입력은 이미 처리 중이다. 라우터로 넘기면 매 프레임 가드에 다시 진입한다.
            return e.Action == PlayerAction.Guard
                && (e.Phase == ActionPhase.Pressed || e.Phase == ActionPhase.Held);
        }
    }
}
