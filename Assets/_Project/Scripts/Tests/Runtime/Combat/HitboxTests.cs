// 직렬화 참조를 SerializedObject로 연결하므로 에디터에서만 실행한다.
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
            _hitbox = CreateHitbox(_attacker);
            _blade = _hitbox.transform;
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

        // 첫 타격은 흘리고(무적 구간) 이후로는 맞는 대상. 칼날이 계속 걸쳐 있으면 같은 스윙이 다시 맞혀야 한다.
        [UnityTest]
        public IEnumerator Hitbox_WithTargetEvadingFirstHit_HitsTargetAgainInSameSwing()
        {
            SpyHittable target = CreateTarget(AlongBlade(0f, 0.5f));
            target.Outcome = HitOutcome.Evaded;
            target.OnHit = () => target.Outcome = HitOutcome.Hit;
            _hitbox.Open(_attacker, HitStrength.Light);
            yield return null;
            yield return null;
            yield return null;

            Assert.That(target.HitCount, Is.EqualTo(2));
        }

        // 검출한 프레임의 끝에서 확인해, 전달이 다음 프레임으로 밀리면 실패한다.
        [UnityTest]
        public IEnumerator Hitbox_WithTargetOnBlade_HitsTargetBeforeEndOfSameFrame()
        {
            SpyHittable target = CreateTarget(AlongBlade(0f, 0.5f));
            // Update가 끝난 뒤에 열어, 이번 프레임의 LateUpdate가 반드시 검사하게 한다.
            yield return null;

            _hitbox.Open(_attacker, HitStrength.Light);
            yield return new WaitForEndOfFrame();

            Assert.That(target.HitCount, Is.EqualTo(1));
        }

        // 서로의 칼날 위에 마주 선 두 공격자가 맞으면 자기 칼을 닫는다. 먼저 전해진 타격이 상대의 공격을 끊어도 둘 다 맞아야 한다.
        [UnityTest]
        public IEnumerator Hitbox_WithOpposingHitboxClosedByFirstDeliveredHit_HitsBothAttackers()
        {
            Transform opponent = Create("Opponent").transform;
            opponent.position = AlongBlade(0f, 0.5f);
            opponent.rotation = Quaternion.LookRotation(_attacker.position - opponent.position);
            Hitbox opponentHitbox = CreateHitbox(opponent);

            SpyHittable attackerBody = AddBody(_attacker.gameObject);
            SpyHittable opponentBody = AddBody(opponent.gameObject);
            attackerBody.OnHit = _hitbox.Close;
            opponentBody.OnHit = opponentHitbox.Close;

            _hitbox.Open(_attacker, HitStrength.Light);
            opponentHitbox.Open(opponent, HitStrength.Light);
            yield return null;

            Assert.That(new[] { attackerBody.HitCount, opponentBody.HitCount }, Is.EqualTo(new[] { 1, 1 }));
        }

        // 먼저 전해진 쪽이 다른 쪽을 파괴하므로, 어느 쪽이 먼저 전해지든 파괴된 대상에게는 전하지 않아야 한다.
        [UnityTest]
        public IEnumerator Hitbox_WithTargetDestroyedByEarlierDeliveredHit_SkipsDestroyedTarget()
        {
            SpyHittable first = CreateTarget(AlongBlade(0f, 0.3f));
            SpyHittable second = CreateTarget(AlongBlade(0f, 0.7f));
            first.OnHit = () => Object.DestroyImmediate(second.gameObject);
            second.OnHit = () => Object.DestroyImmediate(first.gameObject);

            _hitbox.Open(_attacker, HitStrength.Light);
            yield return null;

            Assert.That(first.HitCount + second.HitCount, Is.EqualTo(1));
        }

        // 두 칼이 떨어진 곳에서 각자 다른 대상을 치고, 맞은 대상이 다른 칼의 공격자를 파괴한다.
        // 어느 쪽이 먼저 전해지든 공격자가 사라진 타격은 전하지 않아야 한다.
        [UnityTest]
        public IEnumerator Hitbox_WithAttackerDestroyedByEarlierDeliveredHit_SkipsHitFromDestroyedAttacker()
        {
            Transform otherAttacker = Create("OtherAttacker").transform;
            otherAttacker.position = _attacker.position + Vector3.right * 3f;
            Hitbox otherHitbox = CreateHitbox(otherAttacker);

            SpyHittable target = CreateTarget(AlongBlade(0f, 0.5f));
            SpyHittable otherTarget = CreateTarget(otherAttacker.position + Vector3.forward * 0.5f);
            Transform attacker = _attacker;
            target.OnHit = () => Object.DestroyImmediate(otherAttacker.gameObject);
            otherTarget.OnHit = () => Object.DestroyImmediate(attacker.gameObject);

            _hitbox.Open(_attacker, HitStrength.Light);
            otherHitbox.Open(otherAttacker, HitStrength.Light);
            yield return null;

            Assert.That(target.HitCount + otherTarget.HitCount, Is.EqualTo(1));
        }

        // 두 대상 모두 맞으면 예외를 던지므로, 어느 쪽이 먼저 전해지든 나머지 대상까지 전해졌는지 확인할 수 있다.
        // 한 프레임 더 진행해 이미 전한 타격이 다시 전해지지 않는지도 함께 본다.
        [UnityTest]
        public IEnumerator Hitbox_WithTargetsThrowingOnHit_DeliversToEachTargetOnce()
        {
            SpyHittable first = CreateTarget(AlongBlade(0f, 0.3f));
            SpyHittable second = CreateTarget(AlongBlade(0f, 0.7f));
            first.OnHit = () => throw new System.InvalidOperationException("first target failed");
            second.OnHit = () => throw new System.InvalidOperationException("second target failed");
            LogAssert.Expect(LogType.Exception, new Regex("first target failed"));
            LogAssert.Expect(LogType.Exception, new Regex("second target failed"));

            _hitbox.Open(_attacker, HitStrength.Light);
            yield return null;
            yield return null;

            Assert.That(new[] { first.HitCount, second.HitCount }, Is.EqualTo(new[] { 1, 1 }));
        }

        private GameObject Create(string name)
        {
            var gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        /// <summary><paramref name="attacker"/>의 정면으로 뻗은 칼날 판정을 만든다. 칼날 축은 공격자 위치다.</summary>
        private Hitbox CreateHitbox(Transform attacker)
        {
            // Awake의 참조 검사보다 먼저 연결하도록 비활성 상태로 만든다.
            GameObject blade = Create("Blade");
            blade.SetActive(false);
            blade.transform.SetParent(attacker, false);

            var tip = new GameObject("Tip").transform;
            tip.SetParent(blade.transform, false);
            tip.localPosition = Vector3.forward * BladeLength;

            var hitbox = blade.AddComponent<Hitbox>();
            var serialized = new SerializedObject(hitbox);
            serialized.FindProperty("_base").objectReferenceValue = blade.transform;
            serialized.FindProperty("_tip").objectReferenceValue = tip;
            serialized.FindProperty("_radius").floatValue = BladeRadius;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            blade.SetActive(true);
            return hitbox;
        }

        private SpyHittable CreateTarget(Vector3 position)
        {
            GameObject target = Create("Target");
            target.transform.position = position;
            return AddBody(target);
        }

        /// <summary>칼에 맞을 수 있도록 <paramref name="owner"/>에 몸 콜라이더와 피격 대상을 붙인다.</summary>
        private static SpyHittable AddBody(GameObject owner)
        {
            owner.AddComponent<SphereCollider>().radius = TargetRadius;
            return owner.AddComponent<SpyHittable>();
        }

        /// <summary>칼날 축(공격자 위치)에서 칼날 정면 기준 <paramref name="yaw"/>(도) 방향으로 <paramref name="distance"/>만큼 떨어진 위치.</summary>
        private Vector3 AlongBlade(float yaw, float distance)
        {
            return _attacker.position + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * distance;
        }
    }
}
#endif
