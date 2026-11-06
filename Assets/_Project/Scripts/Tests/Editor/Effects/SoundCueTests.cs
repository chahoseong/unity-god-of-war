using System.Collections.Generic;
using GodOfWar.Effects;
using NUnit.Framework;
using UnityEngine;

namespace GodOfWar.Tests.Effects
{
    public class SoundCueTests
    {
        // 무작위 선택을 여러 번 돌려, 규칙을 어기는 선택이 한 번이라도 나오면 실패하게 한다.
        private const int PickCount = 50;

        private readonly List<AudioClip> _clips = new List<AudioClip>();

        [TearDown]
        public void TearDown()
        {
            foreach (AudioClip clip in _clips)
            {
                Object.DestroyImmediate(clip);
            }

            _clips.Clear();
        }

        [Test]
        public void TryPick_WithNoClips_ReturnsFalse()
        {
            var cue = new SoundCue(new AudioClip[0], 1f, 1f, 1f);

            Assert.That(cue.TryPick(out _, out _), Is.False);
        }

        // 클론한 레포처럼 클립 파일이 없으면 참조가 빠져 null로 읽힌다. 그런 칸은 소리 없는 칸이어야 한다.
        [Test]
        public void TryPick_WithOnlyMissingClips_ReturnsFalse()
        {
            var cue = new SoundCue(new AudioClip[] { null, null }, 1f, 1f, 1f);

            Assert.That(cue.TryPick(out _, out _), Is.False);
        }

        [Test]
        public void TryPick_WithMissingClipAmongClips_PicksOnlyPresentClip()
        {
            AudioClip present = CreateClip("Present");
            var cue = new SoundCue(new[] { null, present, null }, 1f, 1f, 1f);

            for (int i = 0; i < PickCount; i++)
            {
                Assert.That(cue.TryPick(out AudioClip clip, out _), Is.True);
                Assert.That(clip, Is.SameAs(present));
            }
        }

        [Test]
        public void TryPick_WithSeveralClips_NeverPicksPreviousClipAgain()
        {
            var cue = new SoundCue(new[] { CreateClip("A"), CreateClip("B"), CreateClip("C") }, 1f, 1f, 1f);
            cue.TryPick(out AudioClip previous, out _);

            for (int i = 0; i < PickCount; i++)
            {
                cue.TryPick(out AudioClip clip, out _);
                Assert.That(clip, Is.Not.SameAs(previous));
                previous = clip;
            }
        }

        // 반복을 피할 다른 클립이 없으면 같은 클립이라도 내야 소리가 빠지지 않는다.
        [Test]
        public void TryPick_WithSingleClip_PicksItEveryTime()
        {
            AudioClip only = CreateClip("Only");
            var cue = new SoundCue(new[] { only }, 1f, 1f, 1f);

            for (int i = 0; i < PickCount; i++)
            {
                Assert.That(cue.TryPick(out AudioClip clip, out _), Is.True);
                Assert.That(clip, Is.SameAs(only));
            }
        }

        [Test]
        public void TryPick_WithPitchRange_ReturnsPitchWithinRange()
        {
            var cue = new SoundCue(new[] { CreateClip("A") }, 1f, 0.7f, 0.8f);

            for (int i = 0; i < PickCount; i++)
            {
                cue.TryPick(out _, out float pitch);
                Assert.That(pitch, Is.InRange(0.7f, 0.8f));
            }
        }

        private AudioClip CreateClip(string clipName)
        {
            AudioClip clip = AudioClip.Create(clipName, 441, 1, 44100, false);
            _clips.Add(clip);
            return clip;
        }
    }
}
