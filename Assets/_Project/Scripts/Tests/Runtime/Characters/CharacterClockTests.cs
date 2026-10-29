using System;
using System.Collections;
using GodOfWar.Characters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GodOfWar.Tests.Characters
{
    // 시계는 실제 프레임의 전역 시간으로 진행하므로, 전역 시간과 시계가 벌어진 양(지연)으로 슬로가 뒤처지게 한 시간을 잰다.
    // 슬로 하나는 시간 × (1 − 배율)만큼 뒤처지게 한다.
    // 프레임 길이는 일정하지 않아, 슬로가 프레임 중간에 끝나는 몫을 버리거나 한 프레임으로 올리면 지연이 어긋난다.
    public class CharacterClockTests
    {
        private const double Tolerance = 1e-4;

        private GameObject _gameObject;
        private CharacterClock _clock;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("Clock");
            _clock = _gameObject.AddComponent<CharacterClock>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [UnityTest]
        public IEnumerator Now_AcrossSlow_LagsGlobalTimeByDurationTimesSlowdown()
        {
            yield return null;
            double lagBefore = Lag();

            _clock.Slow(0.1f, 0.2f);
            yield return WaitGlobal(0.3f);

            Assert.That(Lag() - lagBefore, Is.EqualTo(0.1 * 0.8).Within(Tolerance));
        }

        // 앞선 슬로가 더 강해도 새 타격의 요청이 대신한다. 피격 반응이 최근 타격으로 바뀌는 것과 같다.
        [UnityTest]
        public IEnumerator Slow_InLaterFrameThanRunningSlow_ReplacesRunningSlow()
        {
            yield return null;
            double lagBefore = Lag();
            double start = Time.timeAsDouble;

            _clock.Slow(0.2f, 0f);
            yield return null;
            double elapsed = Time.timeAsDouble - start;
            Assume.That(elapsed, Is.LessThan(0.2), "The first slow must still be running.");
            _clock.Slow(0.05f, 0.5f);
            yield return WaitGlobal(0.3f);

            Assert.That(Lag() - lagBefore, Is.EqualTo(elapsed + 0.05 * 0.5).Within(Tolerance));
        }

        // 같은 프레임의 요청 순서는 의미가 없어, 더 많이 뒤처지게 하는 쪽을 따른다.
        [UnityTest]
        public IEnumerator Slow_InSameFrameAfterWeakerRequest_ReplacesWeakerRequest()
        {
            yield return null;
            double lagBefore = Lag();

            _clock.Slow(0.05f, 0f);
            _clock.Slow(0.2f, 0.5f);
            yield return WaitGlobal(0.4f);

            Assert.That(Lag() - lagBefore, Is.EqualTo(0.2 * 0.5).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator Slow_InSameFrameAfterStrongerRequest_KeepsStrongerRequest()
        {
            yield return null;
            double lagBefore = Lag();

            _clock.Slow(0.2f, 0.5f);
            _clock.Slow(0.05f, 0f);
            yield return WaitGlobal(0.4f);

            Assert.That(Lag() - lagBefore, Is.EqualTo(0.2 * 0.5).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator TimeScale_DuringSlow_EqualsRequestedTimeScale()
        {
            yield return null;

            _clock.Slow(0.5f, 0.25f);
            yield return null;

            Assert.That(_clock.TimeScale, Is.EqualTo(0.25f).Within(1e-5f));
        }

        [Test]
        public void Slow_WithNegativeDuration_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _clock.Slow(-0.1f, 0f));
        }

        [TestCase(-0.1f)]
        [TestCase(1.1f)]
        public void Slow_WithTimeScaleOutsideZeroToOne_ThrowsArgumentOutOfRangeException(float timeScale)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _clock.Slow(0.1f, timeScale));
        }

        private double Lag()
        {
            return Time.timeAsDouble - _clock.Now;
        }

        private static IEnumerator WaitGlobal(float seconds)
        {
            double end = Time.timeAsDouble + seconds;
            while (Time.timeAsDouble < end)
            {
                yield return null;
            }
        }
    }
}
