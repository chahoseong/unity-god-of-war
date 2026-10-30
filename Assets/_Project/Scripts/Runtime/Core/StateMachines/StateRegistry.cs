using System;
using System.Collections.Generic;

namespace GodOfWar.Core.StateMachines
{
    /// <summary>
    /// 상태를 타입당 하나씩 보관하고 타입으로 찾아 주는 <see cref="IStateLocator"/> 기본 구현.
    /// 상태 머신을 소유한 쪽이 비공개로 들고 등록하며, 상태에게는 <see cref="IStateLocator"/>로만 넘긴다.
    /// 소유자는 자기 OnEnable·OnDisable에서 같은 이름의 <see cref="OnEnable"/>, <see cref="OnDisable"/>을 호출해 등록된 모든 상태에 알린다.
    /// </summary>
    public sealed class StateRegistry : IStateLocator
    {
        private readonly Dictionary<Type, IState> _states = new Dictionary<Type, IState>();

        // 알림 순서가 실행마다 같도록 등록 순서를 따로 보관한다.
        private readonly List<IState> _registrationOrder = new List<IState>();

        private bool _isEnabled;

        /// <summary><paramref name="state"/>를 그 실제 타입으로 등록한다. 같은 타입이 이미 있으면 예외를 던진다.</summary>
        /// <exception cref="InvalidOperationException">이미 켜져 있다. 켜진 뒤 등록한 상태는 켜짐 알림을 받지 못한다.</exception>
        public void Add(IState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (_isEnabled)
            {
                throw new InvalidOperationException(
                    $"Cannot register '{state.GetType().Name}' after {nameof(OnEnable)}. Register states before the owner is enabled.");
            }

            Type type = state.GetType();
            if (_states.ContainsKey(type))
            {
                throw new InvalidOperationException($"State '{type.Name}' is already registered.");
            }

            _states.Add(type, state);
            _registrationOrder.Add(state);
        }

        public T Get<T>() where T : class, IState
        {
            if (_states.TryGetValue(typeof(T), out IState state))
            {
                return (T)state;
            }

            throw new InvalidOperationException($"State '{typeof(T).Name}' is not registered.");
        }

        /// <summary>등록된 모든 상태에 등록 순서대로 <see cref="IState.OnEnable"/>을 호출한다. 소유자의 OnEnable에서 호출한다.</summary>
        /// <exception cref="InvalidOperationException">이미 켜져 있다.</exception>
        public void OnEnable()
        {
            if (_isEnabled)
            {
                throw new InvalidOperationException($"{nameof(StateRegistry)} is already enabled.");
            }

            _isEnabled = true;
            foreach (IState state in _registrationOrder)
            {
                state.OnEnable();
            }
        }

        /// <summary>등록된 모든 상태에 등록 순서대로 <see cref="IState.OnDisable"/>을 호출한다. 소유자의 OnDisable에서 호출한다.</summary>
        /// <exception cref="InvalidOperationException">켜져 있지 않다.</exception>
        public void OnDisable()
        {
            if (!_isEnabled)
            {
                throw new InvalidOperationException($"{nameof(StateRegistry)} is not enabled.");
            }

            _isEnabled = false;
            foreach (IState state in _registrationOrder)
            {
                state.OnDisable();
            }
        }
    }
}
