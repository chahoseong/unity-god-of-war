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
    public class WeaponTests
    {
        private const float BladeLength = 1f;
        private const float BladeRadius = 0.05f;
        private const float TargetRadius = 0.15f;

        private readonly List<GameObject> _objects = new List<GameObject>();
        // 공격자가 무기에 맡긴 콜백으로 받은 판정.
        private readonly List<HitEvent> _dealt = new List<HitEvent>();
        private Transform _attacker;
        private Transform _blade;
        private Weapon _weapon;

        [SetUp]
        public void SetUp()
        {
            // 원점에서 떨어뜨려 오프셋 계산이 월드 원점에 기대지 않는지도 함께 확인한다.
            _attacker = Create("Attacker").transform;
            _attacker.position = new Vector3(10f, 0f, 5f);
            _weapon = CreateWeapon(_attacker);
            _blade = _weapon.transform;
            _dealt.Clear();
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
        public IEnumerator Weapon_WithBladeSweepingPastTargetInOneFrame_HitsTarget()
        {
            SpyHittable target = CreateTarget(AlongBlade(60f, 0.85f));
            _weapon.Open(HitStrength.Light);
            yield return null;

            _blade.localRotation = Quaternion.Euler(0f, 120f, 0f);
            yield return null;

            Assert.That(target.HitCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Weapon_WithTargetOverlappingAcrossFrames_HitsTargetOnce()
        {
            SpyHittable target = CreateTarget(AlongBlade(0f, 0.5f));
            _weapon.Open(HitStrength.Light);
            yield return null;
            yield return null;
            yield return null;

            Assert.That(target.HitCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Weapon_AfterReopen_HitsSameTargetAgain()
        {
            SpyHittable target = CreateTarget(AlongBlade(0f, 0.5f));
            _weapon.Open(HitStrength.Light);
            yield return null;

            _weapon.Close();
            _weapon.Open(HitStrength.Light);
            yield return null;

            Assert.That(target.HitCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Weapon_WithTargetAsChildOfAttacker_DoesNotHitTarget()
        {
            SpyHittable target = CreateTarget(AlongBlade(0f, 0.5f));
            target.transform.SetParent(_attacker, true);
            _weapon.Open(HitStrength.Light);
            yield return null;

            Assert.That(target.HitCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Weapon_AfterClose_DoesNotHitTarget()
        {
            SpyHittable target = CreateTarget(AlongBlade(0f, 0.5f));
            _weapon.Open(HitStrength.Light);
            _weapon.Close();
            yield return null;

            Assert.That(target.HitCount, Is.EqualTo(0));
        }

        // 첫 타격은 흘리고(무적 구간) 이후로는 맞는 대상. 칼날이 계속 걸쳐 있으면 같은 스윙이 다시 맞혀야 한다.
        [UnityTest]
        public IEnumerator Weapon_WithTargetEvadingFirstHit_HitsTargetAgainInSameSwing()
        {
            SpyHittable target = CreateTarget(AlongBlade(0f, 0.5f));
            target.Outcome = HitOutcome.Evaded;
            target.OnHit = () => target.Outcome = HitOutcome.Hit;
            _weapon.Open(HitStrength.Light);
            yield return null;
            yield return null;
            yield return null;

            Assert.That(target.HitCount, Is.EqualTo(2));
        }

        // 검출한 프레임의 끝에서 확인해, 전달이 다음 프레임으로 밀리면 실패한다.
        [UnityTest]
        public IEnumerator Weapon_WithTargetOnBlade_HitsTargetBeforeEndOfSameFrame()
        {
            SpyHittable target = CreateTarget(AlongBlade(0f, 0.5f));
            // Update가 끝난 뒤에 열어, 이번 프레임의 LateUpdate가 반드시 검사하게 한다.
            yield return null;

            _weapon.Open(HitStrength.Light);
            yield return new WaitForEndOfFrame();

            Assert.That(target.HitCount, Is.EqualTo(1));
        }

        // 서로의 칼날 위에 마주 선 두 공격자가 맞으면 자기 칼을 닫는다. 먼저 전해진 타격이 상대의 공격을 끊어도 둘 다 맞아야 한다.
        [UnityTest]
        public IEnumerator Weapon_WithOpposingWeaponClosedByFirstDeliveredHit_HitsBothAttackers()
        {
            Transform opponent = Create("Opponent").transform;
            opponent.position = AlongBlade(0f, 0.5f);
            opponent.rotation = Quaternion.LookRotation(_attacker.position - opponent.position);
            Weapon opponentWeapon = CreateWeapon(opponent);

            SpyHittable attackerBody = AddBody(_attacker.gameObject);
            SpyHittable opponentBody = AddBody(opponent.gameObject);
            attackerBody.OnHit = _weapon.Close;
            opponentBody.OnHit = opponentWeapon.Close;

            _weapon.Open(HitStrength.Light);
            opponentWeapon.Open(HitStrength.Light);
            yield return null;

            Assert.That(new[] { attackerBody.HitCount, opponentBody.HitCount }, Is.EqualTo(new[] { 1, 1 }));
        }

        // 먼저 전해진 쪽이 다른 쪽을 파괴하므로, 어느 쪽이 먼저 전해지든 파괴된 대상에게는 전하지 않아야 한다.
        [UnityTest]
        public IEnumerator Weapon_WithTargetDestroyedByEarlierDeliveredHit_SkipsDestroyedTarget()
        {
            SpyHittable first = CreateTarget(AlongBlade(0f, 0.3f));
            SpyHittable second = CreateTarget(AlongBlade(0f, 0.7f));
            first.OnHit = () => Object.DestroyImmediate(second.gameObject);
            second.OnHit = () => Object.DestroyImmediate(first.gameObject);

            _weapon.Open(HitStrength.Light);
            yield return null;

            Assert.That(first.HitCount + second.HitCount, Is.EqualTo(1));
        }

        // 두 칼이 떨어진 곳에서 각자 다른 대상을 치고, 맞은 대상이 다른 칼의 공격자를 파괴한다.
        // 어느 쪽이 먼저 전해지든 공격자가 사라진 타격은 전하지 않아야 한다.
        [UnityTest]
        public IEnumerator Weapon_WithAttackerDestroyedByEarlierDeliveredHit_SkipsHitFromDestroyedAttacker()
        {
            Transform otherAttacker = Create("OtherAttacker").transform;
            otherAttacker.position = _attacker.position + Vector3.right * 3f;
            Weapon otherWeapon = CreateWeapon(otherAttacker);

            SpyHittable target = CreateTarget(AlongBlade(0f, 0.5f));
            SpyHittable otherTarget = CreateTarget(otherAttacker.position + Vector3.forward * 0.5f);
            Transform attacker = _attacker;
            target.OnHit = () => Object.DestroyImmediate(otherAttacker.gameObject);
            otherTarget.OnHit = () => Object.DestroyImmediate(attacker.gameObject);

            _weapon.Open(HitStrength.Light);
            otherWeapon.Open(HitStrength.Light);
            yield return null;

            Assert.That(target.HitCount + otherTarget.HitCount, Is.EqualTo(1));
        }

        // 두 대상 모두 맞으면 예외를 던지므로, 어느 쪽이 먼저 전해지든 나머지 대상까지 전해졌는지 확인할 수 있다.
        // 한 프레임 더 진행해 이미 전한 타격이 다시 전해지지 않는지도 함께 본다.
        [UnityTest]
        public IEnumerator Weapon_WithTargetsThrowingOnHit_DeliversToEachTargetOnce()
        {
            SpyHittable first = CreateTarget(AlongBlade(0f, 0.3f));
            SpyHittable second = CreateTarget(AlongBlade(0f, 0.7f));
            first.OnHit = () => throw new System.InvalidOperationException("first target failed");
            second.OnHit = () => throw new System.InvalidOperationException("second target failed");
            LogAssert.Expect(LogType.Exception, new Regex("first target failed"));
            LogAssert.Expect(LogType.Exception, new Regex("second target failed"));

            _weapon.Open(HitStrength.Light);
            yield return null;
            yield return null;

            Assert.That(new[] { first.HitCount, second.HitCount }, Is.EqualTo(new[] { 1, 1 }));
        }

        // 기본값(Hit)이 아닌 판정을 돌려줘, 무기가 판정을 바꾸거나 지어내지 않고 그대로 넘기는지 본다.
        [UnityTest]
        public IEnumerator Weapon_WithTargetOnBlade_PassesOutcomeToOwner()
        {
            SpyHittable target = CreateTarget(AlongBlade(0f, 0.5f));
            target.Outcome = HitOutcome.Blocked;
            _weapon.Open(HitStrength.Light);
            yield return null;

            Assert.That(_dealt.Count, Is.EqualTo(1));
            Assert.That(_dealt[0].Target, Is.SameAs(target));
            Assert.That(_dealt[0].Result.Outcome, Is.EqualTo(HitOutcome.Blocked));
        }

        // 지정 타격은 칼날 검출을 거치지 않으므로, 판정이 소유자에게 돌아오는 경로를 따로 확인한다.
        [UnityTest]
        public IEnumerator Strike_WithClosedWeapon_PassesOutcomeToOwner()
        {
            // 칼날에서 떨어진 곳에 두어, 칼날 검출로는 맞을 수 없게 한다.
            SpyHittable target = CreateTarget(_attacker.position + Vector3.back * 3f);
            target.Outcome = HitOutcome.Parried;
            _weapon.Strike(target, HitStrength.Heavy);
            yield return null;

            Assert.That(_dealt.Count, Is.EqualTo(1));
            Assert.That(_dealt[0].Target, Is.SameAs(target));
            Assert.That(_dealt[0].Attack.Attacker, Is.SameAs(_attacker));
            Assert.That(_dealt[0].Result.Outcome, Is.EqualTo(HitOutcome.Parried));
        }

        private GameObject Create(string name)
        {
            var gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        /// <summary>
        /// <paramref name="attacker"/>의 정면으로 뻗은 칼날 판정을 만들어 그 공격자에게 맡긴다. 칼날 축은 공격자 위치다.
        /// 받은 판정은 <see cref="_dealt"/>에 모은다.
        /// </summary>
        private Weapon CreateWeapon(Transform attacker)
        {
            // Awake의 참조 검사보다 먼저 연결하도록 비활성 상태로 만든다.
            GameObject blade = Create("Blade");
            blade.SetActive(false);
            blade.transform.SetParent(attacker, false);

            var tip = new GameObject("Tip").transform;
            tip.SetParent(blade.transform, false);
            tip.localPosition = Vector3.forward * BladeLength;

            var weapon = blade.AddComponent<Weapon>();
            var serialized = new SerializedObject(weapon);
            serialized.FindProperty("_base").objectReferenceValue = blade.transform;
            serialized.FindProperty("_tip").objectReferenceValue = tip;
            serialized.FindProperty("_radius").floatValue = BladeRadius;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            blade.SetActive(true);
            weapon.Bind(attacker, _dealt.Add);
            return weapon;
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
