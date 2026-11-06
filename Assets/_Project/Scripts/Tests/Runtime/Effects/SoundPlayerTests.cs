using GodOfWar.Effects;
using NUnit.Framework;
using UnityEngine;

namespace GodOfWar.Tests.Effects
{
    // 재생 중인지(isPlaying)는 플레이 모드에서만 바뀌므로 PlayMode에서 확인한다. 클립은 테스트 동안 끝나지 않도록 길게 만든다.
    public class SoundPlayerTests
    {
        // SoundPlayer의 보이스 상한. 넘치게 재생해 보이스를 빼앗는 규칙을 확인한다.
        private const int MaxVoices = 16;

        private AudioClip _first;
        private AudioClip _second;
        private AudioClip _third;

        [SetUp]
        public void SetUp()
        {
            _first = CreateClip("First");
            _second = CreateClip("Second");
            _third = CreateClip("Third");
        }

        [TearDown]
        public void TearDown()
        {
            // 컨테이너를 지우면 재생기는 다음 요청에서 보이스를 새로 만든다.
            Object.DestroyImmediate(GameObject.Find(nameof(SoundPlayer)));
            Object.DestroyImmediate(_first);
            Object.DestroyImmediate(_second);
            Object.DestroyImmediate(_third);
        }

        // 피치는 AudioSource마다 하나라, 같은 소스로 겹쳐 내면 앞 소리의 피치가 바뀐다. 소리마다 다른 보이스여야 한다.
        [Test]
        public void Play_WhileAnotherSoundPlays_KeepsEarlierSoundAndItsPitch()
        {
            AudioSource earlier = SoundPlayer.Play(Cue(_first, 0.5f), Vector3.zero, 0f);
            AudioSource later = SoundPlayer.Play(Cue(_second, 2f), Vector3.zero, 0f);

            Assert.That(later, Is.Not.SameAs(earlier));
            Assert.That(earlier.isPlaying, Is.True);
            Assert.That(earlier.clip, Is.SameAs(_first));
            Assert.That(earlier.pitch, Is.EqualTo(0.5f));
        }

        // 보이스는 미리 만들지 않고 겹친 만큼만 만든다. 끝난 소리의 보이스는 다음 소리가 다시 쓴다.
        [Test]
        public void Play_AfterEarlierSoundStopped_ReusesIdleVoice()
        {
            AudioSource earlier = SoundPlayer.Play(Cue(_first, 1f), Vector3.zero, 0f);
            earlier.Stop();

            AudioSource later = SoundPlayer.Play(Cue(_second, 1f), Vector3.zero, 0f);

            Assert.That(later, Is.SameAs(earlier));
        }

        [Test]
        public void Play_WhenAllVoicesBusy_ReplacesOldestSound()
        {
            AudioSource oldest = SoundPlayer.Play(Cue(_first, 1f), Vector3.zero, 0f);
            AudioSource newer = null;
            for (int i = 1; i < MaxVoices; i++)
            {
                newer = SoundPlayer.Play(Cue(_second, 1f), Vector3.zero, 0f);
            }

            AudioSource stolen = SoundPlayer.Play(Cue(_third, 1f), Vector3.zero, 0f);

            Assert.That(stolen, Is.SameAs(oldest));
            Assert.That(oldest.clip, Is.SameAs(_third));
            Assert.That(newer.isPlaying, Is.True);
        }

        // 3D 소리가 요청한 자리에서 나도록 보이스를 그 위치로 옮긴다.
        [Test]
        public void Play_WithPosition_PlaysAtPosition()
        {
            var position = new Vector3(3f, 1f, -2f);

            AudioSource voice = SoundPlayer.Play(Cue(_first, 1f), position, 1f);

            Assert.That(Vector3.Distance(voice.transform.position, position), Is.LessThan(1e-4f));
            Assert.That(voice.spatialBlend, Is.EqualTo(1f));
        }

        [Test]
        public void Play_WithCueWithoutClips_ReturnsNull()
        {
            AudioSource voice = SoundPlayer.Play(new SoundCue(new AudioClip[0], 1f, 1f, 1f), Vector3.zero, 0f);

            Assert.That(voice, Is.Null);
        }

        private static SoundCue Cue(AudioClip clip, float pitch)
        {
            return new SoundCue(new[] { clip }, 1f, pitch, pitch);
        }

        private static AudioClip CreateClip(string clipName)
        {
            // 무음 10초. 테스트 동안 재생이 끝나지 않는다.
            return AudioClip.Create(clipName, 44100 * 10, 1, 44100, false);
        }
    }
}
