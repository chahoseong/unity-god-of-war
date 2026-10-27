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
    /// 무기 판정은 타격 구간 동안만 열고, 피격 등 어떤 경로로 상태를 나가든 닫는다.
    /// </summary>
    public sealed class AttackState : IState
    {
        private static readonly int IdleHash = Animator.StringToHash("Idle");

        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly CharacterMotor _motor;
        private readonly Animator _animator;
        private readonly Hitbox _hitbox;

        private IdleState _idle;

        // 다음 Enter에서 시작할 공격.
        private AttackAction _pendingAction;

        private AttackAction _action;
        private float _elapsed;
        private bool _isReturning;

        public AttackState(ITrainingDummyStateContext context)
        {
            _machine = context.Machine;
            _locator = context.States;
            _motor = context.Motor;
            _animator = context.Animator;
            _hitbox = context.Hitbox;
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

            _animator.CrossFadeInFixedTime(Animator.StringToHash(_action.StateName), _action.CrossFadeDuration);
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            UpdateHitbox();
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
            // 피격으로 끊겨도 판정이 열린 채 남지 않는다.
            _hitbox.Close();
        }

        /// <summary>타격 구간에 맞춰 무기 판정을 열고 닫는다. 경과 시간은 줄지 않으므로 한 타에서 판정은 한 번만 열린다.</summary>
        private void UpdateHitbox()
        {
            bool isInWindow = _action.IsInHitWindow(_elapsed);
            if (isInWindow && !_hitbox.IsOpen)
            {
                _hitbox.Open(_motor.transform, _action.HitStrength);
            }
            else if (!isInWindow && _hitbox.IsOpen)
            {
                _hitbox.Close();
            }
        }
    }
}
