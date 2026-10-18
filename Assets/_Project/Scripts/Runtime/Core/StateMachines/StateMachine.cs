using System;

namespace GodOfWar.Core.StateMachines
{
    /// <summary>
    /// 한 시점에 하나의 <see cref="IState"/>만 활성화하는 모듈형 FSM (ADR-001).
    /// </summary>
    public sealed class StateMachine
    {
        public IState CurrentState { get; private set; }

        /// <summary>
        /// 현재 상태를 <paramref name="next"/>로 전환한다.
        /// 순서: 현재 상태 Exit → CurrentState 교체 → 다음 상태 Enter.
        /// 같은 상태로의 전환도 Exit/Enter를 다시 호출한다.
        /// </summary>
        public void ChangeState(IState next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            CurrentState?.Exit();
            CurrentState = next;
            next.Enter();
        }

        public void Tick(float deltaTime)
        {
            if (CurrentState == null)
            {
                throw new InvalidOperationException(
                    "No current state. Call ChangeState before Tick.");
            }

            CurrentState.Tick(deltaTime);
        }
    }
}
