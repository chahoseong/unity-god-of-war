using GodOfWar.Input;
using GodOfWar.Player;
using NUnit.Framework;
using UnityEngine;

namespace GodOfWar.Tests.Player
{
    public class FlickDetectorTests
    {
        private const float StickFlickThreshold = 0.7f;
        private const float StickRearmThreshold = 0.3f;
        private const float MouseFlickThreshold = 50f;
        private const float MouseDecayRate = 10f;
        private const float MouseFlickCooldown = 0.25f;
        private const float FrameTime = 1f / 60f;

        private FlickDetector _detector;

        [SetUp]
        public void SetUp()
        {
            _detector = new FlickDetector(LockOnSettings.ForFlick(
                StickFlickThreshold, StickRearmThreshold, MouseFlickThreshold, MouseDecayRate, MouseFlickCooldown));
        }

        private int Stick(float x)
        {
            return _detector.Tick(new Vector2(x, 0f), LookInputType.Rate, FrameTime);
        }

        private int Mouse(float x, float deltaTime = FrameTime)
        {
            return _detector.Tick(new Vector2(x, 0f), LookInputType.Delta, deltaTime);
        }

        [Test]
        public void Tick_WithStickPastFlickThreshold_ReturnsDirection()
        {
            Assert.That(Stick(0.8f), Is.EqualTo(1));
        }

        // 밀고 있는 동안 대상이 줄줄이 넘어가면 안 된다.
        [Test]
        public void Tick_WithStickHeldAfterFlick_ReturnsZero()
        {
            Stick(0.8f);

            Assert.That(Stick(0.8f), Is.EqualTo(0));
        }

        // 완전히 놓지 않고 살짝 늦췄다 다시 미는 손떨림은 새 플릭이 아니다.
        [Test]
        public void Tick_WithStickEasedAboveRearmThreshold_ReturnsZero()
        {
            Stick(0.8f);
            Stick(0.45f);

            Assert.That(Stick(0.8f), Is.EqualTo(0));
        }

        [Test]
        public void Tick_WithStickReleasedBelowRearmThreshold_ReturnsDirectionOnNextFlick()
        {
            Stick(0.8f);
            Stick(0.2f);

            Assert.That(Stick(-0.8f), Is.EqualTo(-1));
        }

        // 우스틱 누름으로 락온하는 순간 스틱이 밀려 있어도 곧바로 전환되면 안 된다.
        [Test]
        public void Tick_AfterDisarmWithStickHeld_ReturnsZero()
        {
            _detector.Disarm();

            Assert.That(Stick(0.8f), Is.EqualTo(0));
        }

        [Test]
        public void Tick_WithMouseDeltaPastFlickThreshold_ReturnsDirection()
        {
            Assert.That(Mouse(-60f), Is.EqualTo(-1));
        }

        // 1초 동안의 이동량 합은 문턱을 넘지만, 누적이 감쇠해 전환되지 않는다.
        [Test]
        public void Tick_WithSlowMouseDrift_ReturnsZero()
        {
            int flicks = 0;
            for (int i = 0; i < 60; i++)
            {
                flicks += Mathf.Abs(Mouse(1f));
            }

            Assert.That(flicks, Is.EqualTo(0));
        }

        // 한 번 휘두른 마우스가 대상을 연달아 넘기면 안 된다.
        [Test]
        public void Tick_WithMouseFlickDuringCooldown_ReturnsZero()
        {
            Mouse(60f);

            Assert.That(Mouse(60f, deltaTime: 0.1f), Is.EqualTo(0));
        }
    }
}
