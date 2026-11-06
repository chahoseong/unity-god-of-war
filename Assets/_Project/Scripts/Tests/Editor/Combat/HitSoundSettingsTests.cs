using System.Collections.Generic;
using GodOfWar.Combat;
using GodOfWar.Effects;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GodOfWar.Tests.Combat
{
    // 칸마다 칸 이름을 단 클립을 하나씩 넣어, 판정·세기·넉다운이 엉뚱한 칸을 고르면 실패하게 한다.
    public class HitSoundSettingsTests
    {
        private static readonly string[] Slots =
        {
            "_hitLight", "_hitHeavy", "_knockdown", "_blocked", "_guardBroken", "_parried"
        };

        private readonly List<AudioClip> _clips = new List<AudioClip>();
        private HitSoundSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<HitSoundSettings>();
            var serialized = new SerializedObject(_settings);
            foreach (string slot in Slots)
            {
                AudioClip clip = AudioClip.Create(slot, 441, 1, 44100, false);
                _clips.Add(clip);
                SerializedProperty clips = serialized.FindProperty($"{slot}._clips");
                clips.arraySize = 1;
                clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
            foreach (AudioClip clip in _clips)
            {
                Object.DestroyImmediate(clip);
            }

            _clips.Clear();
        }

        [TestCase(HitOutcome.Hit, HitStrength.Light, "_hitLight")]
        [TestCase(HitOutcome.Hit, HitStrength.Heavy, "_hitHeavy")]
        [TestCase(HitOutcome.Blocked, HitStrength.Light, "_blocked")]
        [TestCase(HitOutcome.GuardBroken, HitStrength.Heavy, "_guardBroken")]
        [TestCase(HitOutcome.Parried, HitStrength.Light, "_parried")]
        public void TryGet_WithFilledSlots_ReturnsCueOfMatchingSlot(HitOutcome outcome, HitStrength strength, string slot)
        {
            bool found = _settings.TryGet(outcome, strength, out SoundCue cue);

            Assert.That(found, Is.True);
            Assert.That(ClipName(cue), Is.EqualTo(slot));
        }

        // 흘린 타격은 칼이 몸에 닿지 않았으므로 맞은 쪽 소리가 없다. 들리는 것은 공격하는 쪽의 휘두르는 소리다.
        [Test]
        public void TryGet_WithEvadedOutcome_ReturnsFalse()
        {
            bool found = _settings.TryGet(HitOutcome.Evaded, HitStrength.Heavy, out _);

            Assert.That(found, Is.False);
        }

        // 넉다운 소리는 적중 소리를 대신하지 않고 덧붙는다. 적중 소리는 TryGet이 세기로 따로 고른다.
        [Test]
        public void TryGetKnockdown_WithKnockdownHit_ReturnsKnockdownCue()
        {
            bool found = _settings.TryGetKnockdown(HitOutcome.Hit, true, out SoundCue cue);

            Assert.That(found, Is.True);
            Assert.That(ClipName(cue), Is.EqualTo("_knockdown"));
        }

        [Test]
        public void TryGetKnockdown_WithHitWithoutKnockdown_ReturnsFalse()
        {
            Assert.That(_settings.TryGetKnockdown(HitOutcome.Hit, false, out _), Is.False);
        }

        // 넉다운은 막지 못하고 맞았을 때만 의미가 있어, 막거나 흘린 타격에는 덧붙이지 않는다.
        [TestCase(HitOutcome.GuardBroken)]
        [TestCase(HitOutcome.Evaded)]
        public void TryGetKnockdown_WithKnockdownAttackNotHit_ReturnsFalse(HitOutcome outcome)
        {
            Assert.That(_settings.TryGetKnockdown(outcome, true, out _), Is.False);
        }

        private static string ClipName(SoundCue cue)
        {
            Assert.That(cue.TryPick(out AudioClip clip, out _), Is.True);
            return clip.name;
        }
    }
}
