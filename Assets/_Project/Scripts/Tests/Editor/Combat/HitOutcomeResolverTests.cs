using GodOfWar.Combat;
using NUnit.Framework;
using UnityEngine;

namespace GodOfWar.Tests.Combat
{
    public class HitOutcomeResolverTests
    {
        private const float GuardHalfAngle = 45f;

        // 원점에서 떨어뜨리고 정면을 비스듬히 두어, 위치 빼기나 정면 기준이 빠지면 결과가 달라지게 한다.
        private static readonly Vector3 DefenderPosition = new Vector3(4f, 1f, -3f);
        private static readonly Vector3 DefenderForward = Quaternion.Euler(0f, 30f, 0f) * Vector3.forward;

        private GameObject _attacker;

        [SetUp]
        public void SetUp()
        {
            _attacker = new GameObject("Attacker");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_attacker);
        }

        [TestCase(HitStrength.Light)]
        [TestCase(HitStrength.Heavy)]
        public void Resolve_WithOpenStanceAndAttackInFront_ReturnsHit(HitStrength strength)
        {
            Assert.That(Resolve(DefenseStance.Open, strength, AttackerAt(0f)), Is.EqualTo(HitOutcome.Hit));
        }

        // 등 뒤에서 온 강공격도 흘려, 회피에 방향·세기 조건이 섞이지 않았는지 본다.
        [Test]
        public void Resolve_WithEvadingStanceAndHeavyAttackFromBehind_ReturnsEvaded()
        {
            Assert.That(Resolve(DefenseStance.Evading, HitStrength.Heavy, AttackerAt(180f)),
                Is.EqualTo(HitOutcome.Evaded));
        }

        [Test]
        public void Resolve_WithGuardingStanceAndLightAttackInFront_ReturnsBlocked()
        {
            Assert.That(Resolve(DefenseStance.Guarding, HitStrength.Light, AttackerAt(0f)),
                Is.EqualTo(HitOutcome.Blocked));
        }

        [Test]
        public void Resolve_WithGuardingStanceAndHeavyAttackInFront_ReturnsGuardBroken()
        {
            Assert.That(Resolve(DefenseStance.Guarding, HitStrength.Heavy, AttackerAt(0f)),
                Is.EqualTo(HitOutcome.GuardBroken));
        }

        [TestCase(90f, TestName = "{m}(Side)")]
        [TestCase(180f, TestName = "{m}(Behind)")]
        public void Resolve_WithGuardingStanceAndAttackBeyondHalfAngle_ReturnsHit(float yaw)
        {
            Assert.That(Resolve(DefenseStance.Guarding, HitStrength.Light, AttackerAt(yaw)), Is.EqualTo(HitOutcome.Hit));
        }

        // 피격 방향의 정면 구역(±45°) 밖이라도 반각 안이면 막아야 한다. 시작값 45°에서는 두 기준이 같아 이 경우로만 구분된다.
        [Test]
        public void Resolve_WithGuardingStanceAndAttackOutsideFrontSectorButWithinWiderHalfAngle_ReturnsBlocked()
        {
            HitOutcome outcome = HitOutcomeResolver.Resolve(
                DefenseStance.Guarding, new AttackInfo(AttackerAt(60f), HitStrength.Light),
                DefenderPosition, DefenderForward, 90f);

            Assert.That(outcome, Is.EqualTo(HitOutcome.Blocked));
        }

        // 수평으로는 정면이지만 크게 높은 공격자. 3차원 각도로 재면 반각 밖이 된다.
        [Test]
        public void Resolve_WithGuardingStanceAndAttackerHighAboveInFront_ReturnsBlocked()
        {
            Transform attacker = AttackerAt(0f);
            attacker.position += Vector3.up * 5f;

            Assert.That(Resolve(DefenseStance.Guarding, HitStrength.Light, attacker), Is.EqualTo(HitOutcome.Blocked));
        }

        private static HitOutcome Resolve(DefenseStance stance, HitStrength strength, Transform attacker)
        {
            return HitOutcomeResolver.Resolve(
                stance, new AttackInfo(attacker, strength), DefenderPosition, DefenderForward, GuardHalfAngle);
        }

        /// <summary>피격자 정면 기준 <paramref name="yaw"/>(도, 오른쪽이 양수) 방향 2m에 공격자를 둔다.</summary>
        private Transform AttackerAt(float yaw)
        {
            _attacker.transform.position = DefenderPosition + Quaternion.Euler(0f, yaw, 0f) * DefenderForward * 2f;
            return _attacker.transform;
        }
    }
}
