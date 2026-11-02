using GodOfWar.Combat;
using NUnit.Framework;
using UnityEngine;

namespace GodOfWar.Tests.Combat
{
    // 구간 경계는 무기가 판정·궤적을 켜는 프레임을 정하므로, 시작은 포함하고 끝은 포함하지 않는지 양 끝에서 본다.
    public class SwingTimingTests
    {
        private const float HitStart = 1f;
        private const float HitEnd = 2f;
        private const float TrailStart = 0.5f;
        private const float TrailEnd = 2.5f;

        private WeaponTrailProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<WeaponTrailProfile>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_profile);
        }

        [TestCase(HitStart - 0.01f, false)]
        [TestCase(HitStart, true)]
        [TestCase(HitEnd - 0.01f, true)]
        [TestCase(HitEnd, false)]
        public void IsInHitWindow_AtElapsed_IncludesStartExcludesEnd(float elapsed, bool expected)
        {
            Assert.That(Timing(_profile).IsInHitWindow(elapsed), Is.EqualTo(expected));
        }

        [TestCase(TrailStart - 0.01f, false)]
        [TestCase(TrailStart, true)]
        [TestCase(TrailEnd - 0.01f, true)]
        [TestCase(TrailEnd, false)]
        public void IsInTrailWindow_AtElapsed_IncludesStartExcludesEnd(float elapsed, bool expected)
        {
            Assert.That(Timing(_profile).IsInTrailWindow(elapsed), Is.EqualTo(expected));
        }

        // 외형이 빈 공격은 궤적을 그리지 않는다.
        [Test]
        public void IsInTrailWindow_WithoutProfile_ReturnsFalse()
        {
            Assert.That(Timing(null).IsInTrailWindow(TrailStart), Is.False);
        }

        // 패링 카운터처럼 칼날 판정 없이 궤적만 켜는 스윙.
        // 시작 시각(0초)과 보통 공격의 판정 시각 모두에서 확인한다.
        [TestCase(0f)]
        [TestCase(HitStart)]
        public void IsInHitWindow_ForWithoutHitTiming_ReturnsFalse(float elapsed)
        {
            SwingTiming timing = SwingTiming.WithoutHit(TrailStart, TrailEnd, _profile);

            Assert.That(timing.IsInHitWindow(elapsed), Is.False);
        }

        private static SwingTiming Timing(WeaponTrailProfile profile)
        {
            return new SwingTiming(HitStart, HitEnd, HitStrength.Light, TrailStart, TrailEnd, profile);
        }
    }
}
