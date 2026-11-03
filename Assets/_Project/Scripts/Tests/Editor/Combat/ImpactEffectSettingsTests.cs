using GodOfWar.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GodOfWar.Tests.Combat
{
    // 칸마다 서로 다른 원본을 넣어, 판정·세기가 엉뚱한 칸을 고르면 실패하게 한다.
    public class ImpactEffectSettingsTests
    {
        private ImpactEffectSettings _settings;
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<ImpactEffectSettings>();
            _root = new GameObject("Prefabs");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_root);
        }

        [TestCase(HitOutcome.Hit, HitStrength.Light, "_hitLight")]
        [TestCase(HitOutcome.Hit, HitStrength.Heavy, "_hitHeavy")]
        [TestCase(HitOutcome.Blocked, HitStrength.Light, "_blocked")]
        [TestCase(HitOutcome.GuardBroken, HitStrength.Heavy, "_guardBroken")]
        [TestCase(HitOutcome.Parried, HitStrength.Light, "_parried")]
        public void TryGet_WithFilledSlots_ReturnsPrefabOfMatchingSlot(HitOutcome outcome, HitStrength strength, string slot)
        {
            FillAllSlots();

            bool found = _settings.TryGet(outcome, strength, out ParticleSystem prefab);

            Assert.That(found, Is.True);
            Assert.That(prefab.name, Is.EqualTo(slot));
        }

        [Test]
        public void TryGet_WithEvadedOutcome_ReturnsFalse()
        {
            FillAllSlots();

            bool found = _settings.TryGet(HitOutcome.Evaded, HitStrength.Heavy, out _);

            Assert.That(found, Is.False);
        }

        // 빈 칸은 이 피격자에게 그 판정의 이펙트가 없다는 정상 설정이다.
        [Test]
        public void TryGet_WithEmptySlot_ReturnsFalse()
        {
            bool found = _settings.TryGet(HitOutcome.Blocked, HitStrength.Light, out _);

            Assert.That(found, Is.False);
        }

        private void FillAllSlots()
        {
            var serialized = new SerializedObject(_settings);
            foreach (string slot in new[] { "_hitLight", "_hitHeavy", "_blocked", "_guardBroken", "_parried" })
            {
                var prefab = new GameObject(slot);
                prefab.transform.SetParent(_root.transform, false);
                serialized.FindProperty(slot).objectReferenceValue = prefab.AddComponent<ParticleSystem>();
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
