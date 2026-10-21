using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 2단 회피의 2단계인 구르기 회피. <see cref="QuickStepState"/> 도중 Dodge 재입력으로만 진입한다.
    /// 진입 순간의 이동 입력으로 방향을 다시 정하고, 입력이 없으면 스텝의 방향을 그대로 잇는다.
    /// 구르기 중 입력은 받지 않으며, 끝나면 <see cref="LocomotionState"/>로 돌아간다.
    /// </summary>
    public sealed class DodgeRollState : IState
    {
        private static readonly int LocomotionHash = Animator.StringToHash("Locomotion");

        // DodgeCardinal 순서(Forward, Backward, Left, Right)와 같아야 한다.
        private static readonly int[] RollHashes =
        {
            Animator.StringToHash("DodgeRoll_F"),
            Animator.StringToHash("DodgeRoll_B"),
            Animator.StringToHash("DodgeRoll_L"),
            Animator.StringToHash("DodgeRoll_R")
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
        private QuickStepState _quickStep;

        public DodgeRollState(PlayerController owner, StateMachine machine)
        {
            _owner = owner;
            _machine = machine;
            _locator = owner;
            _input = owner.GetRequiredComponent<IPlayerInput>();
            _motor = owner.GetRequiredComponent<CharacterMotor>();
            _animator = owner.Animator;
            _settings = owner.DodgeSettings;
        }

        public void Enter()
        {
            _locomotion ??= _locator.Get<LocomotionState>();
            _quickStep ??= _locator.Get<QuickStepState>();

            DodgeCardinal cardinal;
            float bodyYaw;
            float moveYaw;
            if (DodgeDirection.TryFromInput(_input.Move, _settings.MinInput, out DodgeDirection direction))
            {
                float referenceYaw = _owner.MovementYaw;
                cardinal = direction.Cardinal;
                bodyYaw = referenceYaw + direction.ResidualYaw;
                moveYaw = referenceYaw + direction.InputYaw;
            }
            else
            {
                // 스텝 도중 카메라를 돌렸어도 스텝과 같은 월드 방향으로 이어 구른다.
                cardinal = _quickStep.Cardinal;
                bodyYaw = _quickStep.BodyYaw;
                moveYaw = _quickStep.MoveYaw;
            }

            _motor.RotateTowards(bodyYaw, 180f);
            _animator.CrossFadeInFixedTime(RollHashes[(int)cardinal], _settings.CrossFadeDuration);
            _movement.Begin(_settings.DodgeRoll.GetRootMotion(cardinal), moveYaw);
        }

        public void Tick(float deltaTime)
        {
            _motor.Move(_movement.Tick(deltaTime), deltaTime);

            if (_movement.IsFinished)
            {
                _animator.CrossFadeInFixedTime(LocomotionHash, _settings.CrossFadeDuration);
                _machine.ChangeState(_locomotion);
            }
        }

        public void Exit()
        {
        }
    }
}
