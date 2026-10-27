using GodOfWar.Player;
using NUnit.Framework;

namespace GodOfWar.Tests.Player
{
    public class ParryWindowTests
    {
        private const float Duration = 0.2f;
        private const float Cooldown = 0.5f;

        // 0이 아닌 시각에서 시작해, 누른 시각을 더하거나 빼는 계산이 빠지면 결과가 달라지게 한다.
        private const double PressTime = 12.3;

        private ParryWindow _window;

        [SetUp]
        public void SetUp()
        {
            _window = new ParryWindow(Duration, Cooldown);
        }

        [Test]
        public void IsOpen_WithinDurationAfterPress_ReturnsTrue()
        {
            _window.TryOpen(PressTime);

            Assert.That(_window.IsOpen(PressTime + Duration * 0.5), Is.True);
        }

        [Test]
        public void IsOpen_PastDurationAfterPress_ReturnsFalse()
        {
            _window.TryOpen(PressTime);

            Assert.That(_window.IsOpen(PressTime + Duration * 1.5), Is.False);
        }

        // 쿨다운은 윈도우가 끝난 시각부터 잰다. 누른 시각부터 재면 이 시각에는 이미 풀려 있다.
        [Test]
        public void TryOpen_WithinCooldownAfterExpiredWindow_ReturnsFalse()
        {
            _window.TryOpen(PressTime);
            _window.Close(PressTime + Duration + 0.1);

            Assert.That(_window.TryOpen(PressTime + Duration + Cooldown * 0.9), Is.False);
        }

        [Test]
        public void TryOpen_AfterCooldownFromExpiredWindow_ReturnsTrue()
        {
            _window.TryOpen(PressTime);
            _window.Close(PressTime + Duration + 0.1);

            Assert.That(_window.TryOpen(PressTime + Duration + Cooldown * 1.1), Is.True);
        }

        // 가드를 일찍 풀면 그 순간부터 잰다. 윈도우 끝부터 재면 연타가 실제보다 오래 막힌다.
        [Test]
        public void TryOpen_AfterCooldownFromEarlyClose_ReturnsTrue()
        {
            double closeTime = PressTime + Duration * 0.25;
            _window.TryOpen(PressTime);
            _window.Close(closeTime);

            Assert.That(_window.TryOpen(closeTime + Cooldown * 1.1), Is.True);
        }

        [Test]
        public void TryOpen_WithinCooldownAfterEarlyClose_ReturnsFalse()
        {
            double closeTime = PressTime + Duration * 0.25;
            _window.TryOpen(PressTime);
            _window.Close(closeTime);

            Assert.That(_window.TryOpen(closeTime + Cooldown * 0.9), Is.False);
        }

        // 성공한 윈도우는 헛친 것이 아니므로 바로 다음 공격도 노릴 수 있어야 한다.
        [Test]
        public void TryOpen_RightAfterSucceed_ReturnsTrue()
        {
            _window.TryOpen(PressTime);
            _window.Succeed();
            _window.Close(PressTime + Duration * 0.5);

            Assert.That(_window.TryOpen(PressTime + Duration * 0.6), Is.True);
        }

        [Test]
        public void IsOpen_AfterClose_ReturnsFalse()
        {
            _window.TryOpen(PressTime);
            _window.Close(PressTime + Duration * 0.25);

            Assert.That(_window.IsOpen(PressTime + Duration * 0.5), Is.False);
        }
    }
}
