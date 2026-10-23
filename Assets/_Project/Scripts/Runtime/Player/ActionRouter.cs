using System;
using System.Collections.Generic;
using System.Text;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;

namespace GodOfWar.Player
{
    /// <summary>
    /// 플레이어의 버튼 입력으로 일어나는 상태 전이를 중재한다 (ADR-004).
    /// 상태가 선언한 <see cref="ActionTrigger"/>를 모아 라우팅 테이블을 만들고, 입력이 들어오면
    /// 현재 상태의 내부 연계 → 캔슬 윈도우 → 트리거 순으로 판정한다.
    /// 이동 스틱 같은 유지 입력과 피격 등 강제 전이는 다루지 않는다.
    /// </summary>
    public sealed class ActionRouter : IDisposable
    {
        private readonly StateMachine _machine;
        private readonly IPlayerInput _input;

        // 입력별 트리거 목록. 우선순위 내림차순으로 유지한다.
        private readonly Dictionary<PlayerAction, List<ActionTrigger>> _triggers =
            new Dictionary<PlayerAction, List<ActionTrigger>>();

        public ActionRouter(StateMachine machine, IPlayerInput input)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _input.ActionTriggered += OnActionTriggered;
        }

        /// <summary>상태가 생성될 때 자신의 진입 트리거를 등록한다.</summary>
        /// <exception cref="InvalidOperationException">같은 입력에 같은 우선순위가 이미 선언되어 있다.</exception>
        public void Declare(ActionTrigger trigger)
        {
            if (!_triggers.TryGetValue(trigger.Input, out List<ActionTrigger> list))
            {
                list = new List<ActionTrigger>();
                _triggers.Add(trigger.Input, list);
            }

            int index = 0;
            while (index < list.Count && list[index].Priority >= trigger.Priority)
            {
                if (list[index].Priority == trigger.Priority)
                {
                    throw new InvalidOperationException(
                        $"'{trigger.Target.GetType().Name}' declared {trigger.Input} with priority {trigger.Priority}, "
                        + $"which '{list[index].Target.GetType().Name}' already uses. Give them different priorities.");
                }

                index++;
            }

            list.Insert(index, trigger);
        }

        /// <summary>수집한 라우팅 테이블을 사람이 읽을 수 있는 문자열로 만든다.</summary>
        public string Describe()
        {
            var builder = new StringBuilder($"[{nameof(ActionRouter)}] Routing table");
            foreach (KeyValuePair<PlayerAction, List<ActionTrigger>> entry in _triggers)
            {
                foreach (ActionTrigger trigger in entry.Value)
                {
                    builder.Append($"\n  {entry.Key} → {trigger.Target.GetType().Name} (priority {trigger.Priority})");
                }
            }

            return builder.ToString();
        }

        public void Dispose()
        {
            _input.ActionTriggered -= OnActionTriggered;
        }

        private void OnActionTriggered(PlayerActionEvent e)
        {
            // 첫 상태에 진입하기 전의 입력은 받을 상태가 없다.
            IState current = _machine.CurrentState;
            if (current == null)
            {
                return;
            }

            // 같은 계열의 후속 행동이 라우팅 테이블보다 먼저다. 그래야 연계 구간의 공격이 1타로 돌아가지 않는다.
            if (current is IChainable chainable && chainable.TryChain(e))
            {
                return;
            }

            if (e.Phase != ActionPhase.Pressed || !GetCancellable(current).IsCancelWindowOpen)
            {
                return;
            }

            if (!_triggers.TryGetValue(e.Action, out List<ActionTrigger> list))
            {
                return;
            }

            foreach (ActionTrigger trigger in list)
            {
                if (!trigger.CanStart())
                {
                    continue;
                }

                trigger.Prepare?.Invoke(e);
                _machine.ChangeState(trigger.Target);
                return;
            }
        }

        private static ICancellable GetCancellable(IState state)
        {
            return state as ICancellable ?? throw new InvalidOperationException(
                $"'{state.GetType().Name}' does not implement {nameof(ICancellable)}. Register it through PlayerController.");
        }
    }
}
