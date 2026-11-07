using System;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using UnityEngine;

namespace GodOfWar.Enemies.States
{
    /// <summary>
    /// 처치된 몸을 보여 주는 상태. <see cref="Prepare"/>로 받은 처치 반응 클립을 재생하고, 끝나면 마지막 자세로 누워 있는다.
    /// 제자리에서 중력만 받으며 스스로 나가지 않는다. 처치에 따른 정리(타격 받기·락온·자동 공격)는 이 상태가 아니라 더미가 한다.
    /// 넉다운으로 쓰러지다 처치된 몸은 이 상태가 아니라 넉다운 상태가 누운 채로 둔다.
    /// </summary>
    public sealed class DeathState : IState
    {
        private readonly CharacterMotor _motor;
        private readonly Animator _animator;

        // 다음 Enter에서 재생할 반응.
        private DeathReaction _pendingReaction;

        public DeathState(ITrainingDummyStateContext context)
        {
            _motor = context.Motor;
            _animator = context.Animator;
        }

        /// <summary>다음 진입에서 재생할 처치 반응을 정한다. 전이 직전에 호출한다.</summary>
        public void Prepare(DeathReaction reaction)
        {
            _pendingReaction = reaction ?? throw new ArgumentNullException(nameof(reaction));
        }

        public void Enter()
        {
            DeathReaction reaction = _pendingReaction ?? throw new InvalidOperationException(
                $"{nameof(DeathState)} was entered without a reaction. Call {nameof(Prepare)} first.");
            _pendingReaction = null;

            _animator.CrossFadeInFixedTime(Animator.StringToHash(reaction.StateName), reaction.CrossFadeInDuration);
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
