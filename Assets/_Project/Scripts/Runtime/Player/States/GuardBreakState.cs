using System;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 강공격을 가드로 막다 자세가 무너진 상태. 가드 브레이크 반응을 재생하며 정해진 만큼 밀려난 뒤
    /// <see cref="LocomotionState"/>로 돌아간다. 그동안 다른 액션이 끼어들 수 없고, 무방비라 다시 맞으면 피격한다.
    /// 가드 브레이크 판정으로만 진입하므로 진입 트리거를 선언하지 않는다.
    /// </summary>
    public sealed class GuardBreakState : IState, ICancellable, IDefensible
    {
        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly CharacterMotor _motor;
        private readonly PlayerAnimator _animator;
        private readonly HitReaction _reaction;
        private readonly ActionMovement _movement = new ActionMovement();

        private LocomotionState _locomotion;
        private bool _hasMovement;
        private float _elapsed;

        public GuardBreakState(IPlayerStateContext context, GuardSettings settings)
        {
            _machine = context.Machine;
            _locator = context.States;
            _motor = context.Motor;
            _animator = context.Animator;
            _reaction = settings.GuardBreak;
            if (_reaction == null || _reaction.Clip == null)
            {
                throw new InvalidOperationException(
                    $"'{settings.name}' has no {nameof(GuardSettings.GuardBreak)} clip assigned.");
            }
        }

        public bool IsCancelWindowOpen => false;

        public DefenseStance DefenseStance => DefenseStance.Open;

        public void Enter()
        {
            // 생성 시점에는 다른 상태가 아직 등록되지 않았을 수 있어 첫 진입에서 찾는다.
            _locomotion ??= _locator.Get<LocomotionState>();
            _elapsed = 0f;

            _animator.CrossFade(Animator.StringToHash(_reaction.StateName), _reaction.CrossFadeDuration);

            // 변위는 무너지기 시작한 순간의 몸 정면 기준으로 구워져 있다.
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
