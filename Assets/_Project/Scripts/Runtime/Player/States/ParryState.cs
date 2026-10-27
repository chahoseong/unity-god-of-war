using System;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 저스트 패링에 성공한 상태. 전신 패링 동작을 제자리에서 재생하고, 끝났을 때 Guard 버튼이 눌려 있으면
    /// 가드를 다시 올리지 않고 <see cref="GuardState"/>로, 아니면 <see cref="LocomotionState"/>로 돌아간다.
    /// 막는 자세 그대로라 동작 중에 들어온 공격은 가드처럼 막는다(다시 패링하지는 않는다).
    /// 그동안 다른 액션이 끼어들 수 없고, 누른 버튼은 선입력으로 남는다.
    /// 패링 판정으로만 진입하므로 진입 트리거를 선언하지 않는다.
    /// </summary>
    public sealed class ParryState : IState, ICancellable, IChainable, IDefensible
    {
        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly IPlayerInput _input;
        private readonly CharacterMotor _motor;
        private readonly PlayerAnimator _animator;
        private readonly HitReaction _reaction;

        private LocomotionState _locomotion;
        private GuardState _guard;
        private float _elapsed;

        public ParryState(IPlayerStateContext context, GuardSettings settings)
        {
            _machine = context.Machine;
            _locator = context.States;
            _input = context.Input;
            _motor = context.Motor;
            _animator = context.Animator;
            _reaction = settings.Parry;
            if (_reaction == null || _reaction.Clip == null)
            {
                throw new InvalidOperationException(
                    $"'{settings.name}' has no {nameof(GuardSettings.Parry)} clip assigned.");
            }
        }

        public bool IsCancelWindowOpen => false;

        public DefenseStance DefenseStance => DefenseStance.Guarding;

        public void Enter()
        {
            // 생성 시점에는 다른 상태가 아직 등록되지 않았을 수 있어 첫 진입에서 찾는다.
            _locomotion ??= _locator.Get<LocomotionState>();
            _guard ??= _locator.Get<GuardState>();
            _elapsed = 0f;

            _animator.CrossFade(Animator.StringToHash(_reaction.StateName), _reaction.CrossFadeDuration);
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            // 공격권을 가져온 순간이라 밀리지 않고 제자리에서 버틴다.
            _motor.Move(Vector3.zero, deltaTime);

            if (_elapsed < _reaction.Duration)
            {
                return;
            }

            if (_input.IsHeld(PlayerAction.Guard))
            {
                _guard.PrepareReturnFromParry();
                _machine.ChangeState(_guard);
                return;
            }

            _animator.ReturnToLocomotion(_reaction.CrossFadeDuration);
            _machine.ChangeState(_locomotion);
        }

        public void Exit()
        {
        }

        public bool TryChain(PlayerActionEvent e)
        {
            // 가드는 동작이 끝날 때 버튼 상태로 이어 간다. 라우터로 넘기면 받을 수 있게 된 순간 패링 윈도우가 열린 가드로 들어간다.
            return e.Action == PlayerAction.Guard
                && (e.Phase == ActionPhase.Pressed || e.Phase == ActionPhase.Held);
        }
    }
}
