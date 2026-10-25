using System;
using System.Collections.Generic;

namespace GodOfWar.Core.StateMachines
{
    /// <summary>
    /// 상태를 타입당 하나씩 보관하고 타입으로 찾아 주는 <see cref="IStateLocator"/> 기본 구현.
    /// 상태 머신을 소유한 쪽이 비공개로 들고 등록하며, 상태에게는 <see cref="IStateLocator"/>로만 넘긴다.
    /// </summary>
    public sealed class StateRegistry : IStateLocator
    {
        private readonly Dictionary<Type, IState> _states = new Dictionary<Type, IState>();

        /// <summary><paramref name="state"/>를 그 실제 타입으로 등록한다. 같은 타입이 이미 있으면 예외를 던진다.</summary>
        public void Add(IState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Type type = state.GetType();
            if (_states.ContainsKey(type))
            {
                throw new InvalidOperationException($"State '{type.Name}' is already registered.");
            }

            _states.Add(type, state);
        }

        public T Get<T>() where T : class, IState
        {
            if (_states.TryGetValue(typeof(T), out IState state))
            {
                return (T)state;
            }

            throw new InvalidOperationException($"State '{typeof(T).Name}' is not registered.");
        }
    }
}
