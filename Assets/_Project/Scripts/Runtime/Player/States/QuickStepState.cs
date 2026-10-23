using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 2단 회피의 1단계인 짧은 스텝 회피.
    /// 진입 순간의 이동 입력으로 클립 방향과 몸 Yaw를 정해 회피 동안 고정하고, 클립 루트 궤적대로 이동한 뒤
    /// <see cref="LocomotionState"/>로 돌아간다. 스텝 도중 Dodge를 다시 누르면 즉시 <see cref="DodgeRollState"/>로 이어진다.
    /// 이동 입력이 있을 때 Dodge로 진입하며, 스텝 동안 다른 액션은 끼어들 수 없다.
    /// </summary>
    public sealed class QuickStepState : IState, ICancellable, IChainable
    {
        private static readonly int LocomotionHash = Animator.StringToHash("Locomotion");

        // DodgeCardinal 순서(Forward, Backward, Left, Right)와 같아야 한다.
        private static readonly int[] StepHashes =
        {
            Animator.StringToHash("QuickStep_F"),
            Animator.StringToHash("QuickStep_B"),
            Animator.StringToHash("QuickStep_L"),
            Animator.StringToHash("QuickStep_R")
        };

        private readonly PlayerController _owner;
        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly IPlayerInput _input;
        private readonly CharacterMotor _motor;
        private readonly Animator _animator;
        private readonly DodgeSettings _settings;
        private readonly ActionMovement _movement = new ActionMovement();

        private LocomotionState _locomotion;
        private DodgeRollState _roll;

        public QuickStepState(PlayerController owner, StateMachine machine)
        {
            _owner = owner;
            _machine = machine;
            _locator = owner;
            _input = owner.GetRequiredComponent<IPlayerInput>();
            _motor = owner.GetRequiredComponent<CharacterMotor>();
            _animator = owner.Animator;
            _settings = owner.DodgeSettings;

            // 정지 중(이동 입력이 MinInput 미만)에는 회피하지 않는다.
            owner.Router.Declare(new ActionTrigger(
                PlayerAction.Dodge, this,
                () => DodgeDirection.TryFromInput(_input.Move, _settings.MinInput, out _)));
        }

        public bool IsCancelWindowOpen => false;

        /// <summary>마지막 스텝의 클립 방향. 무입력 구르기가 이어받는다.</summary>
        public DodgeCardinal Cardinal { get; private set; }

        /// <summary>마지막 스텝의 몸 월드 Yaw(도).</summary>
        public float BodyYaw { get; private set; }

        public void Enter()
        {
            // 생성 시점에는 다른 상태가 아직 등록되지 않았을 수 있어 첫 진입에서 찾는다.
            _locomotion ??= _locator.Get<LocomotionState>();
            _roll ??= _locator.Get<DodgeRollState>();

            // 진입 트리거의 같은 판정을 통과한 경우에만 들어오지만, 판정이 어긋나면 회피하지 않고 돌아간다.
            if (!DodgeDirection.TryFromInput(_input.Move, _settings.MinInput, out DodgeDirection direction))
            {
                _machine.ChangeState(_locomotion);
                return;
            }

            float referenceYaw = _owner.MovementYaw;
            Cardinal = direction.Cardinal;
            BodyYaw = referenceYaw + direction.ResidualYaw;

            // 최대 회전량 180°로 어떤 방향이든 즉시 맞춘다. 회피 동안에는 회전하지 않는다.
            // 클립의 로컬 이동을 몸 Yaw로 돌리면 입력 방향이 된다 (몸 Yaw + 클립 방향 = 입력 방향).
            _motor.RotateTowards(BodyYaw, 180f);
            _animator.CrossFadeInFixedTime(StepHashes[(int)Cardinal], _settings.CrossFadeDuration);
            _movement.Begin(_settings.QuickStep.GetRootMotion(Cardinal), BodyYaw);
        }

        public void Tick(float deltaTime)
        {
            _motor.Move(_movement.Tick(deltaTime), deltaTime);

            if (_movement.IsFinished)
            {
                // 복귀 크로스페이드는 시간이 다 된 경로에서만 한다. 구르기로 이어질 때는 구르기가 자기 클립으로 덮는다.
                _animator.CrossFadeInFixedTime(LocomotionHash, _settings.CrossFadeDuration);
                _machine.ChangeState(_locomotion);
            }
        }

        public void Exit()
        {
        }

        public bool TryChain(PlayerActionEvent e)
        {
            if (e.Action != PlayerAction.Dodge || e.Phase != ActionPhase.Pressed)
            {
                return false;
            }

            _machine.ChangeState(_roll);
            return true;
        }
    }
}
