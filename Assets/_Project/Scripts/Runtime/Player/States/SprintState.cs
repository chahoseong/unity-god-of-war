using GodOfWar.Characters;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 전방 질주 상태. 이 상태에 있는 것 자체가 스프린트 래치이며,
    /// <see cref="CanSprint"/>를 만족하지 못하면 <see cref="LocomotionState"/>로 돌아간다.
    /// 질주 중에는 입력 방향으로 이동하고 이동 방향을 바라본다.
    /// <see cref="LocomotionState"/>의 연계로만 진입하며, 질주 중 Sprint 재입력은 받을 곳이 없어 무시된다
    /// (게임패드 L3 오입력·키 연타로 질주가 끊기지 않게). 회피·공격은 언제든 끼어들 수 있다.
    /// </summary>
    public sealed class SprintState : IState, ICancellable
    {
        private readonly PlayerController _owner;
        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly IPlayerInput _input;
        private readonly CharacterMotor _motor;
        private readonly LocomotionSettings _settings;

        private LocomotionState _locomotion;

        public SprintState(PlayerController owner, StateMachine machine)
        {
            _owner = owner;
            _machine = machine;
            _locator = owner;
            _input = owner.GetRequiredComponent<IPlayerInput>();
            _motor = owner.GetRequiredComponent<CharacterMotor>();
            _settings = owner.Settings;
        }

        public bool IsCancelWindowOpen => true;

        /// <summary>
        /// 이동 입력이 전방 콘 안에 있고 충분히 큰지 판정한다.
        /// 콘은 입력 공간의 위쪽(0, 1)을 기준으로 좌우 <see cref="LocomotionSettings.SprintMaxAngle"/>도까지다.
        /// </summary>
        public static bool CanSprint(Vector2 move, LocomotionSettings settings)
        {
            // 크기를 먼저 걸러 영벡터의 각도(NaN)를 계산하지 않는다.
            float magnitude = move.magnitude;
            if (magnitude < settings.SprintMinInput || magnitude <= 0f)
            {
                return false;
            }

            return Vector2.Angle(Vector2.up, move) <= settings.SprintMaxAngle;
        }

        public void Enter()
        {
            _locomotion ??= _locator.Get<LocomotionState>();
        }

        public void Tick(float deltaTime)
        {
            Vector2 move = _input.Move;
            if (!CanSprint(move, _settings))
            {
                _machine.ChangeState(_locomotion);
                return;
            }

            float referenceYaw = _owner.MovementYaw;
            Vector3 direction = Quaternion.Euler(0f, referenceYaw, 0f)
                * new Vector3(move.x, 0f, move.y).normalized;

            Vector3 target = direction * _settings.SprintSpeed;
            Vector3 current = _motor.HorizontalVelocity;
            _motor.Move(Vector3.MoveTowards(current, target, _settings.Acceleration * deltaTime), deltaTime);

            float moveYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            _motor.RotateTowards(moveYaw, _settings.SprintTurnSpeed * deltaTime);
        }

        public void Exit()
        {
        }
    }
}
