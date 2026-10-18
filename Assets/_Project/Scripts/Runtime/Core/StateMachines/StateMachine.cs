using System;
using System.Collections.Generic;

namespace GodOfWar.Core.StateMachines
{
    /// <summary>
    /// 한 시점에 하나의 <see cref="IState"/>만 활성화하는 모듈형 FSM (ADR-001).
    /// 상태 콜백(Enter/Tick/Exit) 실행 중에 요청된 전환은 큐에 쌓았다가
    /// 가장 바깥 호출이 반환되기 직전에 FIFO 순서로 적용한다.
    /// </summary>
    public sealed class StateMachine
    {
        /// <summary>
        /// 바깥 호출 한 번에서 연쇄 처리할 수 있는 전환 횟수의 상한.
        /// 순환 전환이 무한 루프로 메인 스레드를 멈추지 않게 하는 버그 감지용 안전장치이며,
        /// 정상 흐름에서는 도달하지 않는다.
        /// </summary>
        private const int MaxChainedTransitions = 32;

        private readonly Queue<IState> _pendingStates = new Queue<IState>();
        private bool _isBusy;

        public IState CurrentState { get; private set; }

        /// <summary>
        /// 현재 상태를 <paramref name="next"/>로 전환한다.
        /// 순서: 현재 상태 Exit → CurrentState 교체 → 다음 상태 Enter.
        /// 같은 상태로의 전환도 Exit/Enter를 다시 호출한다.
        /// 상태 콜백 안에서 호출하면 진행 중인 콜백과 전환이 끝난 뒤에 적용된다.
        /// </summary>
        public void ChangeState(IState next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _pendingStates.Enqueue(next);
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            try
            {
                DrainPendingStates();
            }
            finally
            {
                // 콜백 예외로 중단된 경우 남은 요청이 이후 호출에서 실행되지 않도록 폐기한다.
                _isBusy = false;
                _pendingStates.Clear();
            }
        }

        public void Tick(float deltaTime)
        {
            if (CurrentState == null)
            {
                throw new InvalidOperationException(
                    "No current state. Call ChangeState before Tick.");
            }

            if (_isBusy)
            {
                throw new InvalidOperationException(
                    "Tick cannot be called from within a state callback.");
            }

            // Tick은 매 프레임 호출되므로 클로저 할당을 피하려고 try/finally를 헬퍼로 묶지 않는다.
            _isBusy = true;
            try
            {
                CurrentState.Tick(deltaTime);
                DrainPendingStates();
            }
            finally
            {
                _isBusy = false;
                _pendingStates.Clear();
            }
        }

        private void DrainPendingStates()
        {
            int transitionCount = 0;
            while (_pendingStates.Count > 0)
            {
                if (++transitionCount > MaxChainedTransitions)
                {
                    throw new InvalidOperationException(
                        $"Exceeded {MaxChainedTransitions} chained transitions in a single call. " +
                        "Check for cyclic ChangeState requests between states.");
                }

                IState next = _pendingStates.Dequeue();
                CurrentState?.Exit();
                CurrentState = next;
                next.Enter();
            }
        }
    }
}
