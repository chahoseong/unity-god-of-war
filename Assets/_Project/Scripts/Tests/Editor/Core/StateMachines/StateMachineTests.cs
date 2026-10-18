using System;
using System.Collections.Generic;
using GodOfWar.Core.StateMachines;
using NUnit.Framework;

namespace GodOfWar.Tests.Core.StateMachines
{
    public class StateMachineTests
    {
        private List<string> _log;
        private StateMachine _machine;
        private SpyState _a;
        private SpyState _b;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _machine = new StateMachine();
            _a = new SpyState("A", _log);
            _b = new SpyState("B", _log);
        }

        [Test]
        public void CurrentState_BeforeFirstChange_IsNull()
        {
            Assert.That(_machine.CurrentState, Is.Null);
        }

        [Test]
        public void ChangeState_FromNoState_CallsOnlyEnter()
        {
            _machine.ChangeState(_a);

            Assert.That(_log, Is.EqualTo(new[] { "A.Enter" }));
            Assert.That(_machine.CurrentState, Is.SameAs(_a));
        }

        [Test]
        public void ChangeState_ToDifferentState_CallsOldExitBeforeNewEnter()
        {
            _machine.ChangeState(_a);
            _log.Clear();

            _machine.ChangeState(_b);

            Assert.That(_log, Is.EqualTo(new[] { "A.Exit", "B.Enter" }));
            Assert.That(_machine.CurrentState, Is.SameAs(_b));
        }

        [Test]
        public void ChangeState_DuringTransition_CurrentStateIsOldInExitAndNewInEnter()
        {
            IState currentDuringExit = null;
            IState currentDuringEnter = null;
            _a.OnExit = () => currentDuringExit = _machine.CurrentState;
            _b.OnEnter = () => currentDuringEnter = _machine.CurrentState;
            _machine.ChangeState(_a);

            _machine.ChangeState(_b);

            Assert.That(currentDuringExit, Is.SameAs(_a));
            Assert.That(currentDuringEnter, Is.SameAs(_b));
        }

        [Test]
        public void ChangeState_ToSameState_ReentersWithExitThenEnter()
        {
            _machine.ChangeState(_a);
            _log.Clear();

            _machine.ChangeState(_a);

            Assert.That(_log, Is.EqualTo(new[] { "A.Exit", "A.Enter" }));
            Assert.That(_machine.CurrentState, Is.SameAs(_a));
        }

        [Test]
        public void ChangeState_WithNull_ThrowsArgumentNullExceptionAndKeepsCurrentState()
        {
            _machine.ChangeState(_a);
            _log.Clear();

            Assert.Throws<ArgumentNullException>(() => _machine.ChangeState(null));

            Assert.That(_log, Is.Empty);
            Assert.That(_machine.CurrentState, Is.SameAs(_a));
        }

        [Test]
        public void Tick_WithActiveState_ForwardsDeltaTimeToCurrentStateOnly()
        {
            _machine.ChangeState(_a);
            _machine.ChangeState(_b);
            _log.Clear();

            _machine.Tick(0.016f);

            Assert.That(_log, Is.EqualTo(new[] { "B.Tick" }));
            Assert.That(_b.TickDeltaTimes, Is.EqualTo(new[] { 0.016f }));
            Assert.That(_a.TickDeltaTimes, Is.Empty);
        }

        [Test]
        public void Tick_WithoutState_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => _machine.Tick(0.016f));
        }

        [Test]
        public void ChangeState_DuringEnter_AppliesRequestAfterEnterCompletes()
        {
            _a.OnEnter = () =>
            {
                _machine.ChangeState(_b);
                _log.Add("A.EnterEnd");
            };

            _machine.ChangeState(_a);

            Assert.That(_log, Is.EqualTo(new[] { "A.Enter", "A.EnterEnd", "A.Exit", "B.Enter" }));
            Assert.That(_machine.CurrentState, Is.SameAs(_b));
        }

        [Test]
        public void ChangeState_DuringExit_AppliesRequestAfterCurrentTransitionCompletes()
        {
            SpyState c = CreateState("C");
            _machine.ChangeState(_a);
            _a.OnExit = () => _machine.ChangeState(c);
            _log.Clear();

            _machine.ChangeState(_b);

            Assert.That(_log, Is.EqualTo(new[] { "A.Exit", "B.Enter", "B.Exit", "C.Enter" }));
            Assert.That(_machine.CurrentState, Is.SameAs(c));
        }

        [Test]
        public void ChangeState_DuringTick_AppliesRequestAfterTickCompletes()
        {
            _machine.ChangeState(_a);
            _a.OnTick = _ =>
            {
                _machine.ChangeState(_b);
                _log.Add("A.TickEnd");
            };
            _log.Clear();

            _machine.Tick(0.016f);

            Assert.That(_log, Is.EqualTo(new[] { "A.Tick", "A.TickEnd", "A.Exit", "B.Enter" }));
            Assert.That(_machine.CurrentState, Is.SameAs(_b));
        }

        [Test]
        public void ChangeState_WithMultipleRequestsInCallback_AppliesInFifoOrder()
        {
            SpyState c = CreateState("C");
            _machine.ChangeState(_a);
            _a.OnTick = _ =>
            {
                _machine.ChangeState(_b);
                _machine.ChangeState(c);
            };
            _log.Clear();

            _machine.Tick(0.016f);

            Assert.That(_log, Is.EqualTo(new[] { "A.Tick", "A.Exit", "B.Enter", "B.Exit", "C.Enter" }));
            Assert.That(_machine.CurrentState, Is.SameAs(c));
        }

        [Test]
        public void ChangeState_WithCyclicTransitions_ThrowsInvalidOperationException()
        {
            _a.OnEnter = () => _machine.ChangeState(_b);
            _b.OnEnter = () => _machine.ChangeState(_a);

            Assert.Throws<InvalidOperationException>(() => _machine.ChangeState(_a));
        }

        [Test]
        public void ChangeState_WithChainWithinLimit_AppliesAllTransitions()
        {
            SpyState c = CreateState("C");
            SpyState d = CreateState("D");
            _a.OnEnter = () => _machine.ChangeState(_b);
            _b.OnEnter = () => _machine.ChangeState(c);
            c.OnEnter = () => _machine.ChangeState(d);

            _machine.ChangeState(_a);

            Assert.That(_log, Is.EqualTo(new[]
            {
                "A.Enter", "A.Exit", "B.Enter", "B.Exit", "C.Enter", "C.Exit", "D.Enter"
            }));
            Assert.That(_machine.CurrentState, Is.SameAs(d));
        }

        [Test]
        public void ChangeState_WithRepeatedChainsAcrossCalls_DoesNotAccumulateTowardLimit()
        {
            SpyState c = CreateState("C");
            SpyState d = CreateState("D");
            _a.OnEnter = () => _machine.ChangeState(_b);
            _b.OnEnter = () => _machine.ChangeState(c);
            c.OnEnter = () => _machine.ChangeState(d);

            // 호출마다 최대 4회 전환(D→A→B→C→D)을 반복해 누적 횟수가 상한을 훨씬 넘어도
            // 카운터가 바깥 호출마다 초기화되므로 예외가 나지 않아야 한다.
            const int repeatCount = 50;
            for (int i = 0; i < repeatCount; i++)
            {
                Assert.DoesNotThrow(() => _machine.ChangeState(_a));
            }

            Assert.That(_machine.CurrentState, Is.SameAs(d));
        }

        [Test]
        public void ChangeState_AfterStateEnterThrows_DiscardsPendingRequestsAndRemainsUsable()
        {
            SpyState c = CreateState("C");
            _a.OnEnter = () =>
            {
                _machine.ChangeState(c);
                throw new StateCallbackException();
            };
            Assert.Throws<StateCallbackException>(() => _machine.ChangeState(_a));
            _log.Clear();

            _machine.ChangeState(_b);

            Assert.That(_log, Is.EqualTo(new[] { "A.Exit", "B.Enter" }));
            Assert.That(_machine.CurrentState, Is.SameAs(_b));
        }

        [Test]
        public void ChangeState_AfterCyclicTransitionsThrow_RemainsUsable()
        {
            SpyState c = CreateState("C");
            _a.OnEnter = () => _machine.ChangeState(_b);
            _b.OnEnter = () => _machine.ChangeState(_a);
            Assert.Throws<InvalidOperationException>(() => _machine.ChangeState(_a));
            _a.OnEnter = null;
            _b.OnEnter = null;
            _log.Clear();

            _machine.ChangeState(c);

            // 상한 도달 시점의 상태(A 또는 B)는 상한 값에 따라 달라지므로 Exit 대상은 검증하지 않는다.
            Assert.That(_log, Has.Count.EqualTo(2));
            Assert.That(_log[1], Is.EqualTo("C.Enter"));
            Assert.That(_machine.CurrentState, Is.SameAs(c));
        }

        [Test]
        public void ChangeState_AfterStateEnterThrows_KeepsNewStateAsCurrent()
        {
            _machine.ChangeState(_a);
            _b.OnEnter = () => throw new StateCallbackException();

            Assert.Throws<StateCallbackException>(() => _machine.ChangeState(_b));

            Assert.That(_machine.CurrentState, Is.SameAs(_b));
        }

        [Test]
        public void ChangeState_AfterStateExitThrows_KeepsPreviousStateAsCurrent()
        {
            _machine.ChangeState(_a);
            _a.OnExit = () => throw new StateCallbackException();
            _log.Clear();

            Assert.Throws<StateCallbackException>(() => _machine.ChangeState(_b));

            Assert.That(_log, Is.EqualTo(new[] { "A.Exit" }));
            Assert.That(_machine.CurrentState, Is.SameAs(_a));
        }

        [Test]
        public void Tick_AfterStateTickThrows_DiscardsPendingRequestsAndRemainsUsable()
        {
            SpyState c = CreateState("C");
            _machine.ChangeState(_a);
            _a.OnTick = _ =>
            {
                _machine.ChangeState(c);
                throw new StateCallbackException();
            };
            Assert.Throws<StateCallbackException>(() => _machine.Tick(0.016f));
            _a.OnTick = null;
            _log.Clear();

            _machine.Tick(0.016f);

            Assert.That(_log, Is.EqualTo(new[] { "A.Tick" }));
            Assert.That(_machine.CurrentState, Is.SameAs(_a));
        }

        [Test]
        public void Tick_DuringCallback_ThrowsInvalidOperationException()
        {
            _machine.ChangeState(_a);
            Exception nestedTickException = null;
            _a.OnTick = _ =>
            {
                try
                {
                    _machine.Tick(0.016f);
                }
                catch (Exception e)
                {
                    nestedTickException = e;
                }
            };

            _machine.Tick(0.016f);

            Assert.That(nestedTickException, Is.TypeOf<InvalidOperationException>());
            Assert.That(_a.TickDeltaTimes, Has.Count.EqualTo(1));
        }

        private SpyState CreateState(string name) => new SpyState(name, _log);

        /// <summary>상태 머신 자체가 던지는 예외와 구분하기 위한, 상태 콜백 전용 예외.</summary>
        private sealed class StateCallbackException : Exception
        {
        }
    }
}
