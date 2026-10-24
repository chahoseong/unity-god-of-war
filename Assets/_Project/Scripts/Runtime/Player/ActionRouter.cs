using System;
using System.Collections.Generic;
using System.Text;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;

namespace GodOfWar.Player
{
    /// <summary>
    /// 플레이어의 버튼 입력으로 일어나는 상태 전이를 중재한다 (ADR-004).
    /// 상태가 선언한 <see cref="ActionTrigger"/>를 모아 (입력, 단계)별 라우팅 테이블을 만들고, 입력이 들어오면
    /// 현재 상태의 내부 연계 → 캔슬 윈도우 → 트리거 순으로 판정한다.
    /// 캔슬 윈도우가 닫혀 있어 거절된 누름은 선입력으로 잠시 보관했다가, <see cref="Tick"/>에서 다시 판정한다.
    /// 이동 스틱 같은 축 입력과 피격 등 강제 전이는 다루지 않는다.
    /// </summary>
    public sealed class ActionRouter : IDisposable
    {
        private readonly StateMachine _machine;
        private readonly IPlayerInput _input;
        private readonly float _bufferDuration;

        // (입력, 단계)별 트리거 목록. 우선순위 내림차순으로 유지한다.
        private readonly Dictionary<(PlayerAction, ActionPhase), List<ActionTrigger>> _triggers =
            new Dictionary<(PlayerAction, ActionPhase), List<ActionTrigger>>();

        // 선입력은 마지막 누름 하나만 보관한다.
        private PlayerActionEvent _buffered;
        private bool _hasBuffered;
        private float _bufferRemaining;

        /// <param name="bufferDuration">선입력을 보관하는 시간(초). 0이면 보관하지 않는다.</param>
        public ActionRouter(StateMachine machine, IPlayerInput input, float bufferDuration)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _bufferDuration = bufferDuration;
            _input.ActionTriggered += OnActionTriggered;
        }

        /// <summary>보관 중인 선입력이 있는지.</summary>
        public bool HasBufferedInput => _hasBuffered;

        /// <summary>보관 중인 선입력의 버튼. <see cref="HasBufferedInput"/>이 거짓이면 의미가 없다.</summary>
        public PlayerAction BufferedAction => _buffered.Action;

        /// <summary>상태가 생성될 때 자신의 진입 트리거를 등록한다.</summary>
        /// <exception cref="InvalidOperationException">같은 입력·단계에 같은 우선순위가 이미 선언되어 있다.</exception>
        public void Declare(ActionTrigger trigger)
        {
            (PlayerAction, ActionPhase) key = (trigger.Input, trigger.Phase);
            if (!_triggers.TryGetValue(key, out List<ActionTrigger> list))
            {
                list = new List<ActionTrigger>();
                _triggers.Add(key, list);
            }

            int index = 0;
            while (index < list.Count && list[index].Priority >= trigger.Priority)
            {
                if (list[index].Priority == trigger.Priority)
                {
                    throw new InvalidOperationException(
                        $"'{trigger.Target.GetType().Name}' declared {trigger.Input} {trigger.Phase} with priority {trigger.Priority}, "
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
            foreach (KeyValuePair<(PlayerAction, ActionPhase), List<ActionTrigger>> entry in _triggers)
            {
                foreach (ActionTrigger trigger in entry.Value)
                {
                    builder.Append(
                        $"\n  {trigger.Input} {trigger.Phase} → {trigger.Target.GetType().Name} (priority {trigger.Priority})");
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// 보관한 선입력을 다시 판정하고 남은 시간을 줄인다.
        /// 상태 머신의 Tick 뒤에 호출해야 이번 프레임에 열린 캔슬 윈도우나 끝난 행동에 같은 프레임에 반응한다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_hasBuffered)
            {
                return;
            }

            // 받을 수 있게 된 순간 새로 누른 것처럼 판정하고, 캔슬 윈도우가 열린 채 거절되면 그 결과로 끝낸다.
            if (TryRoute(_buffered) || IsCancelWindowOpen())
            {
                Flush();
                return;
            }

            _bufferRemaining -= deltaTime;
            if (_bufferRemaining <= 0f)
            {
                Flush();
            }
        }

        /// <summary>보관한 선입력을 버린다. 피격 등 강제 전이 때 호출한다 (ADR-004 5절).</summary>
        public void Flush()
        {
            _hasBuffered = false;
        }

        public void Dispose()
        {
            _input.ActionTriggered -= OnActionTriggered;
        }

        private void OnActionTriggered(PlayerActionEvent e)
        {
            // 첫 상태에 진입하기 전의 입력은 받을 상태가 없다.
            if (_machine.CurrentState == null)
            {
                return;
            }

            // 뗌과 유지는 보관하지 않고, 보관한 누름도 건드리지 않는다. 유지는 눌려 있는 동안 매 프레임 다시 온다.
            if (e.Phase != ActionPhase.Pressed)
            {
                TryRoute(e);
                return;
            }

            // 새 누름이 앞선 선입력을 대신한다. 새 누름이 바로 처리되어도 앞선 선입력이 뒤늦게 나가지 않는다.
            Flush();

            // 시작 조건이 안 맞아 거절된 누름은 보관하지 않는다. 버퍼는 타이밍만 관용한다.
            if (!TryRoute(e) && !IsCancelWindowOpen() && _bufferDuration > 0f)
            {
                _buffered = e;
                _bufferRemaining = _bufferDuration;
                _hasBuffered = true;
            }
        }

        /// <summary>입력을 판정해 내부 연계나 전이가 일어났으면 참을 반환한다.</summary>
        private bool TryRoute(PlayerActionEvent e)
        {
            IState current = _machine.CurrentState;

            // 같은 계열의 후속 행동이 라우팅 테이블보다 먼저다. 그래야 연계 구간의 공격이 1타로 돌아가지 않는다.
            if (current is IChainable chainable && chainable.TryChain(e))
            {
                return true;
            }

            if (!GetCancellable(current).IsCancelWindowOpen)
            {
                return false;
            }

            if (!_triggers.TryGetValue((e.Action, e.Phase), out List<ActionTrigger> list))
            {
                return false;
            }

            foreach (ActionTrigger trigger in list)
            {
                if (!trigger.CanStart())
                {
                    continue;
                }

                trigger.Prepare?.Invoke(e);
                _machine.ChangeState(trigger.Target);
                return true;
            }

            return false;
        }

        private bool IsCancelWindowOpen()
        {
            return GetCancellable(_machine.CurrentState).IsCancelWindowOpen;
        }

        private static ICancellable GetCancellable(IState state)
        {
            return state as ICancellable ?? throw new InvalidOperationException(
                $"'{state.GetType().Name}' does not implement {nameof(ICancellable)}. Register it through PlayerController.");
        }
    }
}
