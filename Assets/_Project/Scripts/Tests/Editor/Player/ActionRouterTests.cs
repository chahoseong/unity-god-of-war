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
        private const float BufferDuration = 0.2f;

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
            _router = new ActionRouter(_machine, _input, BufferDuration);
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
        public void Tick_WithBufferedPressAndCancelWindowOpened_EntersTarget()
        {
            _current.IsCancelWindowOpen = false;
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _low, () => true));
            _input.Raise(PlayerAction.Dodge);

            _current.IsCancelWindowOpen = true;
            _router.Tick(0f);

            Assert.That(_machine.CurrentState, Is.SameAs(_low));
        }

        // 연계 구간이 열리기 직전의 공격이 구간이 열리는 프레임에 다음 타로 이어지는 경우다.
        [Test]
        public void Tick_WithBufferedPressChainedByCurrentState_ConsumesBufferedPress()
        {
            int chainCount = 0;
            _current.IsCancelWindowOpen = false;
            _input.Raise(PlayerAction.LightAttack);

            _current.Chain = _ =>
            {
                chainCount++;
                return true;
            };
            _router.Tick(0f);
            _router.Tick(0f);

            Assert.That(chainCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_AfterBufferDurationElapsed_KeepsCurrentState()
        {
            _current.IsCancelWindowOpen = false;
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _low, () => true));
            _input.Raise(PlayerAction.Dodge);
            _router.Tick(BufferDuration);

            _current.IsCancelWindowOpen = true;
            _router.Tick(0f);

            Assert.That(_machine.CurrentState, Is.SameAs(_current));
        }

        [Test]
        public void Tick_AfterFlush_KeepsCurrentState()
        {
            _current.IsCancelWindowOpen = false;
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _low, () => true));
            _input.Raise(PlayerAction.Dodge);
            _router.Flush();

            _current.IsCancelWindowOpen = true;
            _router.Tick(0f);

            Assert.That(_machine.CurrentState, Is.SameAs(_current));
        }

        [Test]
        public void Tick_WithNewerPressBuffered_EntersNewerTarget()
        {
            _current.IsCancelWindowOpen = false;
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _low, () => true));
            _router.Declare(new ActionTrigger(PlayerAction.LightAttack, _high, () => true));
            _input.Raise(PlayerAction.Dodge);
            _input.Raise(PlayerAction.LightAttack);

            _current.IsCancelWindowOpen = true;
            _router.Tick(0f);

            Assert.That(_machine.CurrentState, Is.SameAs(_high));
        }

        // 새 누름이 바로 처리되면, 그보다 먼저 보관된 누름이 뒤늦게 나가면 안 된다.
        [Test]
        public void Tick_AfterNewerPressHandledImmediately_KeepsCurrentState()
        {
            _current.IsCancelWindowOpen = false;
            _current.Chain = e => e.Action == PlayerAction.LightAttack;
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _low, () => true));
            _input.Raise(PlayerAction.Dodge);
            _input.Raise(PlayerAction.LightAttack);

            _current.IsCancelWindowOpen = true;
            _router.Tick(0f);

            Assert.That(_machine.CurrentState, Is.SameAs(_current));
        }

        // 버퍼는 타이밍만 관용한다. 캔슬 윈도우가 열려 있는데 시작 조건이 안 맞은 누름은 보관하지 않는다.
        [Test]
        public void Tick_AfterPressRejectedByConditionWithCancelWindowOpen_KeepsCurrentState()
        {
            bool canStart = false;
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _low, () => canStart));
            _input.Raise(PlayerAction.Dodge);

            canStart = true;
            _router.Tick(0f);

            Assert.That(_machine.CurrentState, Is.SameAs(_current));
        }

        // 보관된 누름은 받을 수 있게 된 순간 한 번 판정되고, 거절되면 그 결과로 끝난다.
        [Test]
        public void Tick_AfterBufferedPressRejectedByConditionWithCancelWindowOpen_KeepsCurrentState()
        {
            bool canStart = false;
            _current.IsCancelWindowOpen = false;
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _low, () => canStart));
            _input.Raise(PlayerAction.Dodge);
            _current.IsCancelWindowOpen = true;
            _router.Tick(0f);

            canStart = true;
            _router.Tick(0f);

            Assert.That(_machine.CurrentState, Is.SameAs(_current));
        }

        [Test]
        public void ActionTriggered_WithHeldPhaseAndHeldTrigger_EntersTarget()
        {
            _router.Declare(new ActionTrigger(PlayerAction.Guard, _low, () => true, phase: ActionPhase.Held));

            _input.Raise(PlayerAction.Guard, ActionPhase.Held);

            Assert.That(_machine.CurrentState, Is.SameAs(_low));
        }

        // 버튼을 누르고 있는 것만으로 누르는 순간에 시작하는 행동이 반복되면 안 된다.
        [Test]
        public void ActionTriggered_WithHeldPhaseAndPressedTrigger_KeepsCurrentState()
        {
            _router.Declare(new ActionTrigger(PlayerAction.LightAttack, _low, () => true));

            _input.Raise(PlayerAction.LightAttack, ActionPhase.Held);

            Assert.That(_machine.CurrentState, Is.SameAs(_current));
        }

        [Test]
        public void ActionTriggered_WithPressedPhaseAndHeldTrigger_KeepsCurrentState()
        {
            _router.Declare(new ActionTrigger(PlayerAction.Guard, _low, () => true, phase: ActionPhase.Held));

            _input.Raise(PlayerAction.Guard);

            Assert.That(_machine.CurrentState, Is.SameAs(_current));
        }

        // 유지는 눌려 있는 동안 매 프레임 다시 오므로 보관하지 않는다.
        [Test]
        public void ActionTriggered_WithHeldPhaseAndCancelWindowClosed_DoesNotBuffer()
        {
            _current.IsCancelWindowOpen = false;
            _router.Declare(new ActionTrigger(PlayerAction.Guard, _low, () => true, phase: ActionPhase.Held));

            _input.Raise(PlayerAction.Guard, ActionPhase.Held);

            Assert.That(_router.HasBufferedInput, Is.False);
        }

        // 다른 버튼을 누르고 있어도 앞서 보관한 누름이 사라지면 안 된다. 보관한 누름을 대신하는 것은 새 누름뿐이다.
        [Test]
        public void ActionTriggered_WithHeldPhaseAndBufferedPress_KeepsBufferedPress()
        {
            _current.IsCancelWindowOpen = false;
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _low, () => true));
            _input.Raise(PlayerAction.Dodge);

            _input.Raise(PlayerAction.Guard, ActionPhase.Held);

            Assert.That((_router.HasBufferedInput, _router.BufferedAction), Is.EqualTo((true, PlayerAction.Dodge)));
        }

        [Test]
        public void Declare_WithSamePriorityForSameInput_ThrowsInvalidOperationException()
        {
            _router.Declare(new ActionTrigger(PlayerAction.Dodge, _low, () => true, priority: 0));

            Assert.Throws<InvalidOperationException>(
                () => _router.Declare(new ActionTrigger(PlayerAction.Dodge, _high, () => true, priority: 0)));
        }

        [Test]
        public void Declare_WithSamePriorityForSameInputAndDifferentPhase_DoesNotThrow()
        {
            _router.Declare(new ActionTrigger(PlayerAction.Guard, _low, () => true, priority: 0));

            Assert.DoesNotThrow(() => _router.Declare(new ActionTrigger(
                PlayerAction.Guard, _high, () => true, priority: 0, phase: ActionPhase.Held)));
        }
    }
}
