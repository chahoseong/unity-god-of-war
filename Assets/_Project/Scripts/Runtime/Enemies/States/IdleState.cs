using GodOfWar.Characters;
using GodOfWar.Core.StateMachines;
using UnityEngine;

namespace GodOfWar.Enemies.States
{
    /// <summary>
    /// 제자리에 서 있는 대기 상태. 이동하지 않고 중력만 받는다.
    /// 대기 클립으로의 크로스페이드는 이 상태로 돌아오는 쪽이 자기 크로스페이드 시간으로 한다.
    /// </summary>
    public sealed class IdleState : IState
    {
        private readonly CharacterMotor _motor;

        public IdleState(TrainingDummy owner)
        {
            _motor = owner.Motor;
        }

        public void Enter()
        {
        }

        public void Tick(float deltaTime)
        {
            _motor.Move(Vector3.zero, deltaTime);
        }

        public void Exit()
        {
        }
    }
}
