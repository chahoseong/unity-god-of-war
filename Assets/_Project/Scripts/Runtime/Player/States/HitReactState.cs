using System;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 무방비로 맞았을 때의 피격 반응 상태. 공격자가 있는 쪽과 타격 세기에 맞는 반응 클립을 재생하고,
    /// 정해진 변위가 있으면 그만큼 밀려난 뒤 <see cref="LocomotionState"/>로 돌아간다.
    /// 반응 중에 다시 맞으면 같은 상태로 재진입해 새 반응을 처음부터 재생한다. 공격자를 향해 돌지 않는다.
    /// 반응 중에는 다른 액션이 끼어들 수 없고, 다시 맞으면 그대로 피격한다.
    /// 피격으로만 진입하므로 진입 트리거를 선언하지 않는다.
    /// </summary>
    public sealed class HitReactState : IState, ICancellable, IDefensible
    {
        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly CharacterMotor _motor;
        private readonly PlayerAnimator _animator;
        private readonly HitReactionSet _reactions;
        private readonly ActionMovement _movement = new ActionMovement();

        private LocomotionState _locomotion;

        // 다음 Enter에서 재생할 반응.
        private HitReaction _pendingReaction;

        private HitReaction _reaction;
        private bool _hasMovement;
        private float _elapsed;

        public HitReactState(IPlayerStateContext context, HitReactionSet reactions)
        {
            _machine = context.Machine;
            _locator = context.States;
            _motor = context.Motor;
            _animator = context.Animator;
            _reactions = reactions;
        }

        public bool IsCancelWindowOpen => false;

        public DefenseStance DefenseStance => DefenseStance.Open;

        /// <summary>
        /// <paramref name="attack"/>의 공격자가 있는 쪽과 세기로 다음 진입에서 재생할 반응을 고른다. 전이 직전에 호출한다.
        /// </summary>
        public void Prepare(in AttackInfo attack)
        {
            Transform body = _motor.transform;
            HitDirection direction = HitDirectionResolver.Resolve(body.position, body.forward, attack.Attacker.position);
            _pendingReaction = _reactions.Get(attack.Strength, direction);
        }

        public void Enter()
        {
            // 생성 시점에는 다른 상태가 아직 등록되지 않았을 수 있어 첫 진입에서 찾는다.
            _locomotion ??= _locator.Get<LocomotionState>();

            _reaction = _pendingReaction ?? throw new InvalidOperationException(
                $"{nameof(HitReactState)} was entered without a reaction. Call {nameof(Prepare)} first.");
            _pendingReaction = null;
            _elapsed = 0f;

            _animator.CrossFade(Animator.StringToHash(_reaction.StateName), _reaction.CrossFadeDuration);

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
                _animator.ReturnToLocomotion(_reaction.CrossFadeDuration);
                _machine.ChangeState(_locomotion);
            }
        }

        public void Exit()
        {
        }
    }
}
