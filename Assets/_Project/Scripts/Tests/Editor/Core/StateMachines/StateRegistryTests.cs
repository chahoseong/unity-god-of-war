using System;
using System.Collections.Generic;
using GodOfWar.Core.StateMachines;
using NUnit.Framework;

namespace GodOfWar.Tests.Core.StateMachines
{
    public class StateRegistryTests
    {
        private List<string> _log;
        private StateRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _registry = new StateRegistry();
        }

        [Test]
        public void OnEnable_WithMultipleStates_CallsOnEnableOnAllInRegistrationOrder()
        {
            _registry.Add(new SpyState("A", _log));
            _registry.Add(new OtherSpyState("B", _log));

            _registry.OnEnable();

            Assert.That(_log, Is.EqualTo(new[] { "A.OnEnable", "B.OnEnable" }));
        }

        [Test]
        public void OnDisable_AfterEnabled_CallsOnDisableOnAllInRegistrationOrder()
        {
            _registry.Add(new SpyState("A", _log));
            _registry.Add(new OtherSpyState("B", _log));
            _registry.OnEnable();
            _log.Clear();

            _registry.OnDisable();

            Assert.That(_log, Is.EqualTo(new[] { "A.OnDisable", "B.OnDisable" }));
        }

        [Test]
        public void OnEnable_WithStateUsingDefaultHook_CallsImplementedHookOnce()
        {
            // 디폴트 인터페이스 메서드를 쓰는 상태와 섞여도 구현한 상태의 훅만 정확히 한 번 불려야 한다.
            _registry.Add(new DefaultHookState());
            _registry.Add(new SpyState("A", _log));

            _registry.OnEnable();

            Assert.That(_log, Is.EqualTo(new[] { "A.OnEnable" }));
        }

        [Test]
        public void OnDisable_WithStateUsingDefaultHook_CallsImplementedHookOnce()
        {
            _registry.Add(new DefaultHookState());
            _registry.Add(new SpyState("A", _log));
            _registry.OnEnable();
            _log.Clear();

            _registry.OnDisable();

            Assert.That(_log, Is.EqualTo(new[] { "A.OnDisable" }));
        }

        [Test]
        public void Add_WhenEnabled_ThrowsInvalidOperationException()
        {
            _registry.Add(new SpyState("A", _log));
            _registry.OnEnable();

            Assert.Throws<InvalidOperationException>(() => _registry.Add(new OtherSpyState("B", _log)));
        }

        [Test]
        public void OnEnable_WhenAlreadyEnabled_ThrowsWithoutCallingStates()
        {
            _registry.Add(new SpyState("A", _log));
            _registry.OnEnable();
            _log.Clear();

            Assert.Throws<InvalidOperationException>(() => _registry.OnEnable());
            Assert.That(_log, Is.Empty);
        }

        [Test]
        public void OnDisable_WhenNotEnabled_ThrowsWithoutCallingStates()
        {
            _registry.Add(new SpyState("A", _log));

            Assert.Throws<InvalidOperationException>(() => _registry.OnDisable());
            Assert.That(_log, Is.Empty);
        }

        // 레지스트리는 타입당 하나만 받으므로 같은 기록 방식의 두 번째 타입이 필요하다.
        private sealed class OtherSpyState : IState
        {
            private readonly string _name;
            private readonly List<string> _log;

            public OtherSpyState(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public void Enter() { }
            public void Tick(float deltaTime) { }
            public void Exit() { }
            public void OnEnable() => _log.Add($"{_name}.OnEnable");
            public void OnDisable() => _log.Add($"{_name}.OnDisable");
        }

        // 켜짐·꺼짐 훅을 구현하지 않아 기본 구현을 쓰는 상태.
        private sealed class DefaultHookState : IState
        {
            public void Enter() { }
            public void Tick(float deltaTime) { }
            public void Exit() { }
        }
    }
}
