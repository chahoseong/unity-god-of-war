// 직렬화 참조를 SerializedObject로 연결하므로 에디터에서만 실행한다.
#if UNITY_EDITOR
using System.Collections;
using GodOfWar.Combat;
using GodOfWar.Effects;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GodOfWar.Tests.Combat
{
    // 전달 시스템을 거치지 않고 피격 입구를 직접 불러, 판정 알림을 받아 무엇을 어디에 띄우는지만 본다.
    public class ImpactEffectsTests
    {
        private static readonly Vector3 ImpactPosition = new Vector3(1f, 1.2f, 0.3f);

        private GameObject _prefabs;
        private ParticleSystem _hitHeavy;
        private ImpactEffectSettings _settings;
        private GameObject _character;
        private GameObject _attacker;
        private HitTarget _hitTarget;
        private HitOutcome _outcome;

        [SetUp]
        public void SetUp()
        {
            _prefabs = new GameObject("Prefabs");
            _prefabs.SetActive(false);
            _hitHeavy = CreatePrefab("HitHeavy");
            _settings = ScriptableObject.CreateInstance<ImpactEffectSettings>();
            var settings = new SerializedObject(_settings);
            settings.FindProperty("_hitHeavy").objectReferenceValue = _hitHeavy;
            settings.ApplyModifiedPropertiesWithoutUndo();

            _attacker = new GameObject("Attacker");
            // Awake의 설정 검사보다 먼저 연결하도록 비활성 상태로 만든다.
            _character = new GameObject("Character");
            _character.SetActive(false);
            _hitTarget = _character.AddComponent<HitTarget>();
            _hitTarget.Bind((in AttackInfo attack) => new HitResult(_outcome));
            var effects = new SerializedObject(_character.AddComponent<ImpactEffects>());
            effects.FindProperty("_settings").objectReferenceValue = _settings;
            effects.ApplyModifiedPropertiesWithoutUndo();
            _character.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_character);
            Object.DestroyImmediate(_attacker);
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_prefabs);
            Object.DestroyImmediate(GameObject.Find(nameof(EffectPool)));
        }

        [UnityTest]
        public IEnumerator HitTaken_WithFilledSlot_PlaysSlotPrefabAtImpactPointFacingNormal()
        {
            _outcome = HitOutcome.Hit;

            _hitTarget.ReceiveHit(Attack(HitStrength.Heavy, Vector3.left));
            yield return null;

            ParticleSystem played = FindPlaying(_hitHeavy.name);
            Assert.That(played, Is.Not.Null);
            Assert.That(Vector3.Distance(played.transform.position, ImpactPosition), Is.LessThan(1e-4f));
            Assert.That(Vector3.Distance(played.transform.forward, Vector3.left), Is.LessThan(1e-4f));
        }

        // 흘린 타격은 몸에 닿지 않았으므로 칸과 관계없이 아무것도 띄우지 않는다.
        [UnityTest]
        public IEnumerator HitTaken_WithEvadedOutcome_PlaysNothing()
        {
            _outcome = HitOutcome.Evaded;

            _hitTarget.ReceiveHit(Attack(HitStrength.Heavy, Vector3.left));
            yield return null;

            Assert.That(FindPlaying(_hitHeavy.name), Is.Null);
        }

        [UnityTest]
        public IEnumerator HitTaken_WhileDisabled_PlaysNothing()
        {
            _outcome = HitOutcome.Hit;
            _character.GetComponent<ImpactEffects>().enabled = false;

            _hitTarget.ReceiveHit(Attack(HitStrength.Heavy, Vector3.left));
            yield return null;

            Assert.That(FindPlaying(_hitHeavy.name), Is.Null);
        }

        private AttackInfo Attack(HitStrength strength, Vector3 normal)
        {
            return new AttackInfo(_attacker.transform, strength, false, new ImpactPoint(ImpactPosition, normal));
        }

        private ParticleSystem CreatePrefab(string prefabName)
        {
            var prefab = new GameObject(prefabName);
            prefab.transform.SetParent(_prefabs.transform, false);
            ParticleSystem system = prefab.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.duration = 1f;
            return system;
        }

        // 풀의 컨테이너 아래에서 지금 켜져 있는 인스턴스를 찾는다. 미리 만든 인스턴스는 꺼져 있어 걸리지 않는다.
        private static ParticleSystem FindPlaying(string prefabName)
        {
            GameObject container = GameObject.Find(nameof(EffectPool));
            if (container == null)
            {
                return null;
            }

            foreach (Transform child in container.transform)
            {
                if (child.name == prefabName && child.gameObject.activeSelf)
                {
                    return child.GetComponent<ParticleSystem>();
                }
            }

            return null;
        }
    }
}
#endif
