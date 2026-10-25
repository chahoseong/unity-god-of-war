// 직렬화 참조를 SerializedObject로 연결하므로 에디터에서만 실행한다.
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using GodOfWar.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GodOfWar.Tests.Combat
{
    public class HitboxTests
    {
        private const float BladeLength = 1f;
        private const float BladeRadius = 0.05f;
        private const float TargetRadius = 0.15f;

        private readonly List<GameObject> _objects = new List<GameObject>();
        private Transform _attacker;
        private Transform _blade;
        private Hitbox _hitbox;

        [SetUp]
        public void SetUp()
        {
            // 원점에서 떨어뜨려 오프셋 계산이 월드 원점에 기대지 않는지도 함께 확인한다.
            _attacker = Create("Attacker").transform;
            _attacker.position = new Vector3(10f, 0f, 5f);

            // Awake의 참조 검사보다 먼저 연결하도록 비활성 상태로 만든다.
            GameObject blade = Create("Blade");
            blade.SetActive(false);
            _blade = blade.transform;
            _blade.SetParent(_attacker, false);

            var tip = new GameObject("Tip").transform;
            tip.SetParent(_blade, false);
            tip.localPosition = Vector3.forward * BladeLength;

            _hitbox = blade.AddComponent<Hitbox>();
            var serialized = new SerializedObject(_hitbox);
            serialized.FindProperty("_base").objectReferenceValue = _blade;
            serialized.FindProperty("_tip").objectReferenceValue = tip;
            serialized.FindProperty("_radius").floatValue = BladeRadius;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            blade.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            // Destroy는 프레임 끝까지 미뤄져, 남은 칼이 다음 테스트의 대상을 같은 위치에서 칠 수 있다.
            foreach (GameObject gameObject in _objects)
            {
                Object.DestroyImmediate(gameObject);
            }

            _objects.Clear();
        }

        // 한 프레임에 120° 돌면, 두 시점만 검사하거나 칼끝을 직선으로 보간하면 호 위의 대상을 놓친다.
        [UnityTest]
        public IEnumerator Hitbox_WithBladeSweepingPastTargetInOneFrame_HitsTarget()
        {
            SpyHittable target = CreateTarget(AlongBlade(60f, 0.85f));
            _hitbox.Open(_attacker, HitStrength.Light);
            yield return null;

            _blade.localRotation = Quaternion.Euler(0f, 120f, 0f);
            yield return null;

            Assert.That(target.HitCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Hitbox_WithTargetOverlappingAcrossFrames_HitsTargetOnce()
        {
            SpyHittable target = CreateTarget(AlongBlade(0f, 0.5f));
            _hitbox.Open(_attacker, HitStrength.Light);
            yield return null;
            yield return null;
            yield return null;

            Assert.That(target.HitCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Hitbox_AfterReopen_HitsSameTargetAgain()
        {
            SpyHittable target = CreateTarget(AlongBlade(0f, 0.5f));
            _hitbox.Open(_attacker, HitStrength.Light);
            yield return null;

            _hitbox.Close();
            _hitbox.Open(_attacker, HitStrength.Light);
            yield return null;

            Assert.That(target.HitCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Hitbox_WithTargetAsChildOfAttacker_DoesNotHitTarget()
        {
            SpyHittable target = CreateTarget(AlongBlade(0f, 0.5f));
            target.transform.SetParent(_attacker, true);
            _hitbox.Open(_attacker, HitStrength.Light);
            yield return null;

            Assert.That(target.HitCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Hitbox_AfterClose_DoesNotHitTarget()
        {
            SpyHittable target = CreateTarget(AlongBlade(0f, 0.5f));
            _hitbox.Open(_attacker, HitStrength.Light);
            _hitbox.Close();
            yield return null;

            Assert.That(target.HitCount, Is.EqualTo(0));
        }

        private GameObject Create(string name)
        {
            var gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        private SpyHittable CreateTarget(Vector3 position)
        {
            GameObject target = Create("Target");
            target.transform.position = position;
            target.AddComponent<SphereCollider>().radius = TargetRadius;
            return target.AddComponent<SpyHittable>();
        }

        /// <summary>칼날 축(공격자 위치)에서 칼날 정면 기준 <paramref name="yaw"/>(도) 방향으로 <paramref name="distance"/>만큼 떨어진 위치.</summary>
        private Vector3 AlongBlade(float yaw, float distance)
        {
            return _attacker.position + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * distance;
        }
    }
}
#endif
