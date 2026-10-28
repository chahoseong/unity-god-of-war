using System;
using System.Collections;
using GodOfWar.Characters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GodOfWar.Tests.Characters
{
    // 시계는 실제 프레임의 전역 시간으로 진행하므로, 전역 시간과 시계가 벌어진 양(지연)으로 멈춘 시간을 잰다.
    // 프레임 길이는 일정하지 않아, 정지가 프레임 중간에 끝나는 몫을 버리거나 한 프레임으로 올리면 지연이 정지 시간과 어긋난다.
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
        public IEnumerator Now_AcrossFreeze_LagsGlobalTimeByFreezeDuration()
        {
            yield return null;
            double lagBefore = Lag();

            _clock.Freeze(0.1f);
            yield return WaitGlobal(0.3f);

            Assert.That(Lag() - lagBefore, Is.EqualTo(0.1).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator Freeze_DuringLongerFreeze_KeepsRemainingFreeze()
        {
            yield return null;
            double lagBefore = Lag();
            double start = Time.timeAsDouble;

            _clock.Freeze(0.2f);
            yield return null;
            Assume.That(Time.timeAsDouble - start, Is.LessThan(0.15), "The first frame must leave more than the new request.");
            _clock.Freeze(0.05f);
            yield return WaitGlobal(0.4f);

            Assert.That(Lag() - lagBefore, Is.EqualTo(0.2).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator Freeze_DuringShorterFreeze_ExtendsToNewDuration()
        {
            yield return null;
            double lagBefore = Lag();
            double start = Time.timeAsDouble;

            _clock.Freeze(0.1f);
            yield return null;
            double elapsed = Time.timeAsDouble - start;
            Assume.That(elapsed, Is.LessThan(0.1), "The first freeze must still be running.");
            _clock.Freeze(0.2f);
            yield return WaitGlobal(0.4f);

            Assert.That(Lag() - lagBefore, Is.EqualTo(elapsed + 0.2).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator TimeScale_DuringFreeze_IsZero()
        {
            yield return null;

            _clock.Freeze(0.5f);
            yield return null;

            Assert.That(_clock.TimeScale, Is.Zero);
        }

        [Test]
        public void Freeze_WithNegativeDuration_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _clock.Freeze(-0.1f));
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
