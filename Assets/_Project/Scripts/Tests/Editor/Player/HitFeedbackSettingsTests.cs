using System;
using GodOfWar.Combat;
using GodOfWar.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GodOfWar.Tests.Player
{
    // 칸을 잘못 연결해도 피드백 세기만 조금 달라 플레이로는 잘 드러나지 않아, 칸마다 다른 값을 넣고 어느 칸이 나오는지 확인한다.
    public class HitFeedbackSettingsTests
    {
        private HitFeedbackSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<HitFeedbackSettings>();
            // 런타임 타입에 쓰기 경로를 두지 않으려고 직렬화 필드에 쓴다. 칸마다 느려지는 시간을 다르게 둔다.
            var serialized = new SerializedObject(_settings);
            SetDuration(serialized, "_dealtHitLight", 0.01f);
            SetDuration(serialized, "_dealtHitHeavy", 0.02f);
            SetDuration(serialized, "_takenHitLight", 0.03f);
            SetDuration(serialized, "_takenHitHeavy", 0.04f);
            SetDuration(serialized, "_blocked", 0.05f);
            SetDuration(serialized, "_guardBroken", 0.06f);
            SetDuration(serialized, "_parriedLight", 0.07f);
            SetDuration(serialized, "_parriedHeavy", 0.08f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
        }

        [TestCase(HitStrength.Light, 0.01f)]
        [TestCase(HitStrength.Heavy, 0.02f)]
        public void TryGetDealt_ForHit_ReturnsSlotOfStrength(HitStrength strength, float expectedDuration)
        {
            _settings.TryGetDealt(HitOutcome.Hit, strength, out HitFeedback feedback);

            Assert.That(feedback.Duration, Is.EqualTo(expectedDuration));
        }

        [TestCase(HitOutcome.Hit, HitStrength.Light, 0.03f)]
        [TestCase(HitOutcome.Hit, HitStrength.Heavy, 0.04f)]
        [TestCase(HitOutcome.Blocked, HitStrength.Light, 0.05f)]
        [TestCase(HitOutcome.GuardBroken, HitStrength.Heavy, 0.06f)]
        [TestCase(HitOutcome.Parried, HitStrength.Light, 0.07f)]
        [TestCase(HitOutcome.Parried, HitStrength.Heavy, 0.08f)]
        public void TryGetTaken_ForReachableOutcome_ReturnsSlotOfOutcomeAndStrength(
            HitOutcome outcome, HitStrength strength, float expectedDuration)
        {
            _settings.TryGetTaken(outcome, strength, out HitFeedback feedback);

            Assert.That(feedback.Duration, Is.EqualTo(expectedDuration));
        }

        [Test]
        public void TryGetDealt_ForEvaded_ReturnsFalse()
        {
            Assert.That(_settings.TryGetDealt(HitOutcome.Evaded, HitStrength.Heavy, out _), Is.False);
        }

        [Test]
        public void TryGetTaken_ForEvaded_ReturnsFalse()
        {
            Assert.That(_settings.TryGetTaken(HitOutcome.Evaded, HitStrength.Heavy, out _), Is.False);
        }

        // 적이 막거나 튕겨 내게 되면 칸을 추가해야 한다는 것을 조용히 넘기지 않는다.
        [TestCase(HitOutcome.Blocked)]
        [TestCase(HitOutcome.GuardBroken)]
        [TestCase(HitOutcome.Parried)]
        public void TryGetDealt_ForDefendedOutcome_ThrowsArgumentOutOfRangeException(HitOutcome outcome)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _settings.TryGetDealt(outcome, HitStrength.Light, out _));
        }

        // 가드는 약한 타격만 막고 강한 타격에는 무너지므로, 반대 조합은 판정 규칙이 바뀐 경우에만 나온다.
        [TestCase(HitOutcome.Blocked, HitStrength.Heavy)]
        [TestCase(HitOutcome.GuardBroken, HitStrength.Light)]
        public void TryGetTaken_ForGuardOutcomeWithMismatchedStrength_ThrowsArgumentOutOfRangeException(
            HitOutcome outcome, HitStrength strength)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _settings.TryGetTaken(outcome, strength, out _));
        }

        private static void SetDuration(SerializedObject serialized, string slot, float value)
        {
            serialized.FindProperty(slot).FindPropertyRelative("_duration").floatValue = value;
        }
    }
}
