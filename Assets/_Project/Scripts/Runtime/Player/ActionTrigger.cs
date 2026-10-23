using System;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;

namespace GodOfWar.Player
{
    /// <summary>
    /// 상태가 스스로 선언하는 진입 계약 (ADR-004 1절).
    /// <see cref="Input"/>을 누르고 <see cref="CanStart"/>가 참이면 액션 라우터가 <see cref="Target"/>으로 전이한다.
    /// </summary>
    public readonly struct ActionTrigger
    {
        public ActionTrigger(
            PlayerAction input, IState target, Func<bool> canStart, int priority = 0,
            Action<PlayerActionEvent> prepare = null)
        {
            Input = input;
            Target = target ?? throw new ArgumentNullException(nameof(target));
            CanStart = canStart ?? throw new ArgumentNullException(nameof(canStart));
            Priority = priority;
            Prepare = prepare;
        }

        public PlayerAction Input { get; }
        public IState Target { get; }

        /// <summary>대상 상태의 시작 조건. 조건은 대상 상태가 소유한다.</summary>
        public Func<bool> CanStart { get; }

        /// <summary>같은 입력을 선언한 트리거끼리의 판정 순서. 클수록 먼저 판정한다.</summary>
        public int Priority { get; }

        /// <summary>전이 직전에 호출한다. 진입한 입력을 대상 상태에 넘길 때 쓴다. 없으면 null이다.</summary>
        public Action<PlayerActionEvent> Prepare { get; }
    }
}
