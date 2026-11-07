using System;
using GodOfWar.Characters;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GodOfWar.Tests.Characters
{
    // 체력 바는 변경 알림만 보고 다시 그리므로, 값과 함께 알림이 언제 나가고 언제 나가지 않는지 확인한다.
    // 처음 체력을 Awake에서 채우므로 플레이 모드에서 돌린다.
    public class CharacterHealthTests
    {
        private const int MaxHealth = 100;

        private GameObject _gameObject;
        private CharacterHealth _health;
        private int _changedCount;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("Health");
            _health = _gameObject.AddComponent<CharacterHealth>();
            _changedCount = 0;
            _health.Changed += () => _changedCount++;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void Current_OnStart_EqualsMax()
        {
            Assert.That(_health.Max, Is.EqualTo(MaxHealth));
            Assert.That(_health.Current, Is.EqualTo(MaxHealth));
            Assert.That(_health.IsDepleted, Is.False);
        }

        [Test]
        public void TakeDamage_WithinRemaining_ReducesByAmountAndNotifiesOnce()
        {
            _health.TakeDamage(14);

            Assert.That(_health.Current, Is.EqualTo(MaxHealth - 14));
            Assert.That(_changedCount, Is.EqualTo(1));
        }

        // 처치 판단이 0과의 비교이므로, 남은 체력보다 큰 피해도 정확히 0에서 멈춰야 한다.
        [Test]
        public void TakeDamage_MoreThanRemaining_StopsAtZeroAndNotifiesOnce()
        {
            _health.TakeDamage(MaxHealth + 30);

            Assert.That(_health.Current, Is.EqualTo(0));
            Assert.That(_health.IsDepleted, Is.True);
            Assert.That(_changedCount, Is.EqualTo(1));
        }

        // 값이 그대로인데 알리면 체력 바의 잔상 연출이 헛돌므로 알리지 않는다.
        [Test]
        public void TakeDamage_ZeroAmount_DoesNotNotify()
        {
            _health.TakeDamage(0);

            Assert.That(_health.Current, Is.EqualTo(MaxHealth));
            Assert.That(_changedCount, Is.EqualTo(0));
        }

        [Test]
        public void TakeDamage_WhenDepleted_DoesNotNotify()
        {
            _health.TakeDamage(MaxHealth);
            _changedCount = 0;

            _health.TakeDamage(8);

            Assert.That(_health.Current, Is.EqualTo(0));
            Assert.That(_changedCount, Is.EqualTo(0));
        }

        // 음수 피해는 회복이 되어 버리므로 데이터 실수로 보고 막는다.
        [Test]
        public void TakeDamage_NegativeAmount_ThrowsAndKeepsCurrent()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _health.TakeDamage(-1));
            Assert.That(_health.Current, Is.EqualTo(MaxHealth));
            Assert.That(_changedCount, Is.EqualTo(0));
        }
    }
}
