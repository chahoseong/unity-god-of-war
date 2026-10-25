using System;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using UnityEngine;

namespace GodOfWar.Enemies.States
{
    /// <summary>
    /// 피격 반응 상태. <see cref="Prepare"/>로 받은 반응 클립을 재생하고, 정해진 변위가 있으면 그만큼 밀려난 뒤
    /// <see cref="IdleState"/>로 돌아간다. 반응 중에 다시 맞으면 같은 상태로 재진입해 새 반응을 처음부터 재생한다.
    /// </summary>
    public sealed class HitReactState : IState
    {
        private static readonly int IdleHash = Animator.StringToHash("Idle");

        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly CharacterMotor _motor;
        private readonly Animator _animator;
        private readonly ActionMovement _movement = new ActionMovement();

        private IdleState _idle;

        // 다음 Enter에서 재생할 반응.
        private HitReaction _pendingReaction;

        private HitReaction _reaction;
        private bool _hasMovement;
        private float _elapsed;

        public HitReactState(ITrainingDummyStateContext context)
        {
            _machine = context.Machine;
            _locator = context.States;
            _motor = context.Motor;
            _animator = context.Animator;
        }

        /// <summary>다음 진입에서 재생할 반응을 정한다. 전이 직전에 호출한다.</summary>
        public void Prepare(HitReaction reaction)
        {
            _pendingReaction = reaction ?? throw new ArgumentNullException(nameof(reaction));
        }

        public void Enter()
        {
            // 생성 시점에는 다른 상태가 아직 등록되지 않았을 수 있어 첫 진입에서 찾는다.
            _idle ??= _locator.Get<IdleState>();

            _reaction = _pendingReaction ?? throw new InvalidOperationException(
                $"{nameof(HitReactState)} was entered without a reaction. Call {nameof(Prepare)} first.");
            _pendingReaction = null;
            _elapsed = 0f;

            _animator.CrossFadeInFixedTime(Animator.StringToHash(_reaction.StateName), _reaction.CrossFadeDuration);

            // 변위는 반응을 시작한 순간의 몸 정면 기준으로 구워져 있다.
            _hasMovement = _reaction.Movement != null;
            if (_hasMovement)
            {
                _movement.Begin(_reaction.Movement, _motor.transform.eulerAngles.y);
            }
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            _motor.Move(_hasMovement ? _movement.Tick(deltaTime) : Vector3.zero, deltaTime);

            if (_elapsed >= _reaction.Duration)
            {
                _animator.CrossFadeInFixedTime(IdleHash, _reaction.CrossFadeDuration);
                _machine.ChangeState(_idle);
            }
        }

        public void Exit()
        {
        }
    }
}
