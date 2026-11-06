// 직렬화 참조를 SerializedObject로 연결하므로 에디터에서만 실행한다.
#if UNITY_EDITOR
using System.Collections.Generic;
using GodOfWar.Combat;
using GodOfWar.Effects;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GodOfWar.Tests.Combat
{
    // 전달 시스템을 거치지 않고 피격 입구를 직접 불러, 판정 알림을 받아 어떤 소리를 내는지만 본다.
    public class HitSoundsTests
    {
        private AudioClip _parried;
        private AudioClip _hitLight;
        private AudioClip _knockdown;
        private HitSoundSettings _settings;
        private GameObject _character;
        private GameObject _attacker;
        private HitTarget _hitTarget;
        private HitOutcome _outcome;

        [SetUp]
        public void SetUp()
        {
            // 무음 10초. 테스트 동안 재생이 끝나지 않는다.
            _parried = AudioClip.Create("Parried", 44100 * 10, 1, 44100, false);
            _hitLight = AudioClip.Create("HitLight", 44100 * 10, 1, 44100, false);
            _knockdown = AudioClip.Create("Knockdown", 44100 * 10, 1, 44100, false);
            _settings = ScriptableObject.CreateInstance<HitSoundSettings>();
            var settings = new SerializedObject(_settings);
            Fill(settings, "_parried", _parried);
            Fill(settings, "_hitLight", _hitLight);
            Fill(settings, "_knockdown", _knockdown);
            settings.ApplyModifiedPropertiesWithoutUndo();

            _attacker = new GameObject("Attacker");
            // Awake의 설정 검사보다 먼저 연결하도록 비활성 상태로 만든다.
            _character = new GameObject("Character");
            _character.SetActive(false);
            _hitTarget = _character.AddComponent<HitTarget>();
            _hitTarget.Bind((in AttackInfo attack) => new HitResult(_outcome));
            var sounds = new SerializedObject(_character.AddComponent<HitSounds>());
            sounds.FindProperty("_settings").objectReferenceValue = _settings;
            sounds.ApplyModifiedPropertiesWithoutUndo();
            _character.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_character);
            Object.DestroyImmediate(_attacker);
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(GameObject.Find(nameof(SoundPlayer)));
            Object.DestroyImmediate(_parried);
            Object.DestroyImmediate(_hitLight);
            Object.DestroyImmediate(_knockdown);
        }

        [Test]
        public void HitTaken_WithFilledSlot_PlaysSlotClip()
        {
            _outcome = HitOutcome.Parried;

            _hitTarget.ReceiveHit(Attack());

            Assert.That(FindPlaying(), Is.EquivalentTo(new[] { _parried }));
        }

        // 마지막 타처럼 넉다운시키는 타격도 칼에 베인 것은 같으므로, 적중 소리 위에 넉다운 소리를 덧붙인다.
        [Test]
        public void HitTaken_WithKnockdownHit_PlaysHitAndKnockdownSounds()
        {
            _outcome = HitOutcome.Hit;

            _hitTarget.ReceiveHit(Attack(knockdown: true));

            Assert.That(FindPlaying(), Is.EquivalentTo(new[] { _hitLight, _knockdown }));
        }

        // 소리는 재생기에 맡기므로 맞은 캐릭터가 사라져도 끊기지 않는다.
        [Test]
        public void HitTaken_ThenCharacterDestroyed_KeepsSoundPlaying()
        {
            _outcome = HitOutcome.Parried;
            _hitTarget.ReceiveHit(Attack());

            Object.DestroyImmediate(_character);

            Assert.That(FindPlaying(), Is.EquivalentTo(new[] { _parried }));
        }

        [Test]
        public void HitTaken_WithEmptySlot_PlaysNothing()
        {
            _outcome = HitOutcome.Blocked;

            _hitTarget.ReceiveHit(Attack());

            Assert.That(FindPlaying(), Is.Empty);
        }

        [Test]
        public void HitTaken_WhileDisabled_PlaysNothing()
        {
            _outcome = HitOutcome.Parried;
            _character.GetComponent<HitSounds>().enabled = false;

            _hitTarget.ReceiveHit(Attack());

            Assert.That(FindPlaying(), Is.Empty);
        }

        private AttackInfo Attack(bool knockdown = false)
        {
            return new AttackInfo(_attacker.transform, HitStrength.Light, knockdown, new ImpactPoint(Vector3.zero, Vector3.forward));
        }

        private static void Fill(SerializedObject settings, string slot, AudioClip clip)
        {
            SerializedProperty clips = settings.FindProperty($"{slot}._clips");
            clips.arraySize = 1;
            clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
        }

        // 재생기의 보이스 중 지금 재생 중인 클립들. 재생기가 아직 아무것도 만들지 않았으면 비어 있다.
        private static List<AudioClip> FindPlaying()
        {
            var playing = new List<AudioClip>();
            GameObject container = GameObject.Find(nameof(SoundPlayer));
            if (container == null)
            {
                return playing;
            }

            foreach (AudioSource source in container.GetComponentsInChildren<AudioSource>())
            {
                if (source.isPlaying)
                {
                    playing.Add(source.clip);
                }
            }

            return playing;
        }
    }
}
#endif
