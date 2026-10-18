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
    }
}
