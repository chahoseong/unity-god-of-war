using System;
using System.Collections.Generic;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using GodOfWar.Player;
using NUnit.Framework;

namespace GodOfWar.Tests.Player
{
    public class ActionRouterTests
    {
        private List<string> _log;
        private StateMachine _machine;
        private FakePlayerInput _input;
        private ActionRouter _router;
        private FakeActionState _current;
        private FakeActionState _low;
        private FakeActionState _high;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _machine = new StateMachine();
            _input = new FakePlayerInput();
            _router = new ActionRouter(_machine, _input);
            _current = new FakeActionState("Current", _log);
            _low = new FakeActionState("Low", _log);
            _high = new FakeActionState("High", _log);

            _machine.ChangeState(_current);
            _log.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _router.Dispose();
        }

        // 연계 구간의 공격이 라우팅 테이블로 넘어가면 콤보가 1타부터 다시 시작된다 (ADR-004 대안 E).
        [Test]
        public void ActionTriggered_WithInputChainedByCurrentState_KeepsCurrentState()
        {
            _current.Chain = _ => true;
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _low, () => true));

            _input.Raise(PlayerAction.Dodge);

            Assert.That(_machine.CurrentState, Is.SameAs(_current));
        }

        [Test]
        public void ActionTriggered_WithCancelWindowClosed_KeepsCurrentState()
        {
            _current.IsCancelWindowOpen = false;
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _low, () => true));

            _input.Raise(PlayerAction.Dodge);

            Assert.That(_machine.CurrentState, Is.SameAs(_current));
        }

        // 낮은 우선순위를 먼저 선언해 선언 순서가 아니라 우선순위로 고르는지 확인한다.
        [Test]
        public void ActionTriggered_WithSatisfiedTriggersOfDifferentPriority_EntersHighestPriorityTarget()
        {
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _low, () => true, priority: 0));
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _high, () => true, priority: 1));

            _input.Raise(PlayerAction.Dodge);

            Assert.That(_machine.CurrentState, Is.SameAs(_high));
        }

        [Test]
        public void ActionTriggered_WithHigherPriorityConditionFalse_EntersLowerPriorityTarget()
        {
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _low, () => true, priority: 0));
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _high, () => false, priority: 1));

            _input.Raise(PlayerAction.Dodge);

            Assert.That(_machine.CurrentState, Is.SameAs(_low));
        }

        [Test]
        public void ActionTriggered_WithReleasedPhase_KeepsCurrentState()
        {
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _low, () => true));

            _input.Raise(PlayerAction.Dodge, ActionPhase.Released);

            Assert.That(_machine.CurrentState, Is.SameAs(_current));
        }

        // 연계 구간이 끝난 뒤 공격 버튼으로 새 1타를 시작하는 재진입이 막히면 안 된다 (ADR-004 2절).
        [Test]
        public void ActionTriggered_WithTriggerTargetingCurrentState_ReentersCurrentState()
        {
            _router.Declare(new ActionTrigger(PlayerAction.LightAttack, _current, () => true));

            _input.Raise(PlayerAction.LightAttack);

            Assert.That(_log, Is.EqualTo(new[] { "Current.Exit", "Current.Enter" }));
        }

        [Test]
        public void Declare_WithSamePriorityForSameInput_ThrowsInvalidOperationException()
        {
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _low, () => true, priority: 0));

            Assert.Throws<InvalidOperationException>(
                () => _router.Declare(new ActionTrigger(PlayerAction.Dodge, _high, () => true, priority: 0)));
        }
    }
}
