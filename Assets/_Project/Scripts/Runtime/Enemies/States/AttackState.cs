using System;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using UnityEngine;

namespace GodOfWar.Enemies.States
{
    /// <summary>
    /// 테스트용 공격 상태. <see cref="Prepare"/>로 받은 공격 한 타를 공격 클립 → 복귀 클립 순으로 재생하고
    /// <see cref="IdleState"/>로 돌아간다. 콤보로 이어지지 않는다.
    /// 공격 위치가 매번 바뀌지 않도록 공격 데이터의 전진 변위는 쓰지 않고 제자리에서 휘두른다.
    /// 한 타가 무기의 스윙 하나다. 매 프레임 그 타의 <see cref="SwingTiming"/>과 경과 시간을 무기에 넘기고,
    /// 피격 등 어떤 경로로 상태를 나가든 스윙을 끝낸다.
    /// </summary>
    public sealed class AttackState : IState
    {
        private static readonly int IdleHash = Animator.StringToHash("Idle");

        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly CharacterMotor _motor;
        private readonly Animator _animator;
        private readonly Weapon _weapon;

        private IdleState _idle;

        // 다음 Enter에서 시작할 공격.
        private AttackAction _pendingAction;

        private AttackAction _action;
        private SwingTiming _swingTiming;
        private float _elapsed;
        private bool _isReturning;

        public AttackState(ITrainingDummyStateContext context)
        {
            _machine = context.Machine;
            _locator = context.States;
            _motor = context.Motor;
            _animator = context.Animator;
            _weapon = context.Weapon;
        }

        /// <summary>다음 진입에서 할 공격을 정한다. 전이 직전에 호출한다.</summary>
        public void Prepare(AttackAction action)
        {
            _pendingAction = action != null ? action : throw new ArgumentNullException(nameof(action));
        }

        public void Enter()
        {
            // 생성 시점에는 다른 상태가 아직 등록되지 않았을 수 있어 첫 진입에서 찾는다.
            _idle ??= _locator.Get<IdleState>();

            _action = _pendingAction ?? throw new InvalidOperationException(
                $"{nameof(AttackState)} was entered without an action. Call {nameof(Prepare)} first.");
            _pendingAction = null;
            _elapsed = 0f;
            _isReturning = false;

            _swingTiming = _action.SwingTiming;
            _weapon.BeginSwing();

            _animator.CrossFadeInFixedTime(Animator.StringToHash(_action.StateName), _action.CrossFadeDuration);
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            _weapon.UpdateSwing(_swingTiming, _elapsed);
            _motor.Move(Vector3.zero, deltaTime);

            if (!_isReturning)
            {
                if (_elapsed >= _action.Duration)
                {
                    _isReturning = true;
                    _animator.CrossFadeInFixedTime(
                        Animator.StringToHash(_action.ReturnStateName), _action.CrossFadeDuration);
                }

                return;
            }

            if (_elapsed >= _action.Duration + _action.ReturnDuration)
            {
                _animator.CrossFadeInFixedTime(IdleHash, _action.CrossFadeDuration);
                _machine.ChangeState(_idle);
            }
        }

        public void Exit()
        {
            // 피격으로 끊겨도 판정이 열린 채 남지 않고, 궤적도 새 기록을 멈춘다.
            _weapon.EndSwing();
        }
    }
}
