using System;
using System.Collections.Generic;
using GodOfWar.Core.StateMachines;

namespace GodOfWar.Tests.Core.StateMachines
{
    /// <summary>
    /// 생명주기 호출을 공유 로그에 "{Name}.{Method}" 형식으로 기록하는 테스트 더블.
    /// 여러 상태가 같은 로그를 공유하므로 상태 간 호출 순서를 검증할 수 있다.
    /// </summary>
    internal sealed class SpyState : IState
    {
        private readonly List<string> _log;

        public SpyState(string name, List<string> log)
        {
            Name = name;
            _log = log;
        }

        public string Name { get; }

        public List<float> TickDeltaTimes { get; } = new List<float>();

        /// <summary>각 생명주기 메서드가 로그를 남긴 직후 실행할 추가 동작.</summary>
        public Action OnEnter { get; set; }
        public Action<float> OnTick { get; set; }
        public Action OnExit { get; set; }

        public void Enter()
        {
            _log.Add($"{Name}.Enter");
            OnEnter?.Invoke();
        }

        public void Tick(float deltaTime)
        {
            _log.Add($"{Name}.Tick");
            TickDeltaTimes.Add(deltaTime);
            OnTick?.Invoke(deltaTime);
        }

        public void Exit()
        {
            _log.Add($"{Name}.Exit");
            OnExit?.Invoke();
        }

        public override string ToString() => Name;
    }
}
