using System;
using System.Collections.Generic;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using GodOfWar.Player;

namespace GodOfWar.Tests.Player
{
    /// <summary>
    /// 캔슬 윈도우와 내부 연계 응답을 테스트가 정하는 상태 테스트 더블.
    /// 생명주기 호출을 공유 로그에 "{Name}.{Method}" 형식으로 기록한다.
    /// </summary>
    internal sealed class FakeActionState : IState, ICancellable, IChainable
    {
        private readonly List<string> _log;

        public FakeActionState(string name, List<string> log)
        {
            Name = name;
            _log = log;
        }

        public string Name { get; }

        public bool IsCancelWindowOpen { get; set; } = true;

        /// <summary><see cref="TryChain"/>의 응답. 기본값은 연계하지 않음이다.</summary>
        public Func<PlayerActionEvent, bool> Chain { get; set; } = _ => false;

        public void Enter() => _log.Add($"{Name}.Enter");

        public void Tick(float deltaTime)
        {
        }

        public void Exit() => _log.Add($"{Name}.Exit");

        public bool TryChain(PlayerActionEvent e) => Chain(e);

        public override string ToString() => Name;
    }
}
