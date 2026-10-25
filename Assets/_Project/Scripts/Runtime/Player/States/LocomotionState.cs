using GodOfWar.Characters;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 기본 이동 상태. 항상 이동 기준(카메라) 정면을 바라보며 8방향으로 스트레이프하고,
    /// 입력이 없으면 감속해 정지한다. 다른 행동이 끝나면 돌아오는 상태다.
    /// 언제든 다른 액션이 끼어들 수 있고, Sprint 입력은 같은 이동 계열인 질주로 잇는다.
    /// </summary>
    public sealed class LocomotionState : IState, ICancellable, IChainable
    {
        private readonly PlayerController _owner;
        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly IPlayerInput _input;
        private readonly CharacterMotor _motor;
        private readonly LocomotionSettings _settings;

        private SprintState _sprint;

        public LocomotionState(PlayerController owner, StateMachine machine)
        {
            _owner = owner;
            _machine = machine;
            _locator = owner;
            _input = owner.GetRequiredComponent<IPlayerInput>();
            _motor = owner.GetRequiredComponent<CharacterMotor>();
            _settings = owner.LocomotionSettings;
        }

        public bool IsCancelWindowOpen => true;

        public void Enter()
        {
            // 생성 시점에는 다른 상태가 아직 등록되지 않았을 수 있어 첫 진입에서 찾는다.
            _sprint ??= _locator.Get<SprintState>();
        }

        public void Tick(float deltaTime)
        {
            float referenceYaw = _owner.MovementYaw;
            Vector2 move = _input.Move;

            Vector3 target = Quaternion.Euler(0f, referenceYaw, 0f)
                * new Vector3(move.x, 0f, move.y) * _settings.RunSpeed;
            Vector3 current = _motor.HorizontalVelocity;
            float rate = target.sqrMagnitude > current.sqrMagnitude
                ? _settings.Acceleration
                : _settings.Deceleration;

            _motor.Move(Vector3.MoveTowards(current, target, rate * deltaTime), deltaTime);
            _motor.RotateTowards(referenceYaw, _settings.TurnSpeed * deltaTime);
        }

        public void Exit()
        {
        }

        public bool TryChain(PlayerActionEvent e)
        {
            // 누르는 순간에만 판정한다(래치). 떼는 입력은 스프린트에 영향을 주지 않는다.
            if (e.Action != PlayerAction.Sprint
                || e.Phase != ActionPhase.Pressed
                || !SprintState.CanSprint(_input.Move, _settings))
            {
                return false;
            }

            _machine.ChangeState(_sprint);
            return true;
        }
    }
}
