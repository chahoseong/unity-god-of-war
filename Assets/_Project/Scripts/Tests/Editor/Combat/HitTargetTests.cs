using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GodOfWar.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GodOfWar.Tests.Combat
{
    // 전달 시스템을 거치지 않고 입구를 직접 불러, 판정 위임과 알림 계약만 본다.
    public class HitTargetTests
    {
        private readonly List<string> _log = new List<string>();
        private GameObject _owner;
        private GameObject _attacker;
        private HitTarget _target;

        [SetUp]
        public void SetUp()
        {
            _log.Clear();
            _owner = new GameObject("Owner");
            _attacker = new GameObject("Attacker");
            _target = _owner.AddComponent<HitTarget>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_attacker);
        }

        [Test]
        public void ReceiveHit_WithBoundHandler_ReturnsHandlerResult()
        {
            _target.Bind(HandlerReturning(HitOutcome.Blocked));

            HitResult result = _target.ReceiveHit(Attack());

            Assert.That(result.Outcome, Is.EqualTo(HitOutcome.Blocked));
        }

        [Test]
        public void ReceiveHit_WithSubscriber_NotifiesEventOfThisTargetAndHandlerResult()
        {
            _target.Bind(HandlerReturning(HitOutcome.Blocked));
            HitEvent? notified = null;
            _target.HitTaken += hitEvent => notified = hitEvent;

            _target.ReceiveHit(Attack());

            Assert.That(notified.HasValue, Is.True);
            Assert.That(notified.Value.Target, Is.SameAs(_target));
            Assert.That(notified.Value.Result.Outcome, Is.EqualTo(HitOutcome.Blocked));
        }

        // 구독자가 상태 전이까지 끝난 결과를 보도록, 판정이 끝난 뒤에 알린다.
        [Test]
        public void ReceiveHit_WithSubscriber_NotifiesAfterHandler()
        {
            _target.Bind((in AttackInfo _) =>
            {
                _log.Add("Handler");
                return new HitResult(HitOutcome.Hit);
            });
            _target.HitTaken += _ => _log.Add("HitTaken");

            _target.ReceiveHit(Attack());

            Assert.That(_log, Is.EqualTo(new[] { "Handler", "HitTaken" }));
        }

        // 연출 하나의 오류가 다른 연출로 번지지 않는다.
        [Test]
        public void ReceiveHit_WithThrowingSubscriber_NotifiesLaterSubscribers()
        {
            _target.Bind(HandlerReturning(HitOutcome.Hit));
            _target.HitTaken += _ => throw new InvalidOperationException("Broken subscriber");
            _target.HitTaken += _ => _log.Add("Later");
            LogAssert.Expect(LogType.Exception, new Regex("Broken subscriber"));

            _target.ReceiveHit(Attack());

            Assert.That(_log, Is.EqualTo(new[] { "Later" }));
        }

        // 연출의 오류가 공격자에게 돌아갈 판정까지 막지 않는다.
        [Test]
        public void ReceiveHit_WithThrowingSubscriber_ReturnsHandlerResult()
        {
            _target.Bind(HandlerReturning(HitOutcome.Blocked));
            _target.HitTaken += _ => throw new InvalidOperationException("Broken subscriber");
            LogAssert.Expect(LogType.Exception, new Regex("Broken subscriber"));

            HitResult result = _target.ReceiveHit(Attack());

            Assert.That(result.Outcome, Is.EqualTo(HitOutcome.Blocked));
        }

        // 판정을 믿을 수 없으면 알릴 결과도 없다. 예외는 전달 시스템이 기록한다.
        [Test]
        public void ReceiveHit_WithThrowingHandler_DoesNotNotify()
        {
            _target.Bind((in AttackInfo _) => throw new InvalidOperationException("Broken handler"));
            _target.HitTaken += _ => _log.Add("HitTaken");

            Assert.Catch<InvalidOperationException>(() => _target.ReceiveHit(Attack()));

            Assert.That(_log, Is.Empty);
        }

        [Test]
        public void ReceiveHit_WithoutHandler_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => _target.ReceiveHit(Attack()));
        }

        [Test]
        public void Bind_WhenAlreadyBound_ThrowsInvalidOperationException()
        {
            _target.Bind(HandlerReturning(HitOutcome.Hit));

            Assert.Throws<InvalidOperationException>(() => _target.Bind(HandlerReturning(HitOutcome.Hit)));
        }

        private AttackInfo Attack()
        {
            return new AttackInfo(_attacker.transform, HitStrength.Light, false, 0, default);
        }

        private static HitHandler HandlerReturning(HitOutcome outcome)
        {
            return (in AttackInfo _) => new HitResult(outcome);
        }
    }
}
