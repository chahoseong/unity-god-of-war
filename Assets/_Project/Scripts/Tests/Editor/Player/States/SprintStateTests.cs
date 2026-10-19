using GodOfWar.Player;
using GodOfWar.Player.States;
using NUnit.Framework;
using UnityEngine;

namespace GodOfWar.Tests.Player.States
{
    public class SprintStateTests
    {
        private const float MaxAngle = 50f;
        private const float MinInput = 0.5f;

        private LocomotionSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new LocomotionSettings(MaxAngle, MinInput);
        }

        [Test]
        public void CanSprint_WithForwardInput_ReturnsTrue()
        {
            Assert.That(SprintState.CanSprint(Vector2.up, _settings), Is.True);
        }

        [Test]
        public void CanSprint_WithDiagonalForwardInputForDefaultSettings_ReturnsTrue()
        {
            // 기본 콘이 정확히 45°인 대각 전방 입력(예: 정규화된 WASD W+D)을 여유 있게 포함해야
            // 키보드 대각 스프린트가 경계에서 흔들리지 않는다. 기본값을 낮추면 이 테스트가 막는다.
            var defaults = new LocomotionSettings();

            Assert.That(SprintState.CanSprint(new Vector2(1f, 1f).normalized, defaults), Is.True);
            Assert.That(SprintState.CanSprint(new Vector2(-1f, 1f).normalized, defaults), Is.True);
        }

        [TestCase(MaxAngle - 1f)]
        [TestCase(-(MaxAngle - 1f))]
        public void CanSprint_WithInputInsideCone_ReturnsTrue(float angleFromForward)
        {
            Assert.That(SprintState.CanSprint(FromForward(angleFromForward), _settings), Is.True);
        }

        [TestCase(MaxAngle + 1f)]
        [TestCase(-(MaxAngle + 1f))]
        public void CanSprint_WithInputOutsideCone_ReturnsFalse(float angleFromForward)
        {
            Assert.That(SprintState.CanSprint(FromForward(angleFromForward), _settings), Is.False);
        }

        [TestCase(1f)]
        [TestCase(-1f)]
        public void CanSprint_WithSidewaysInput_ReturnsFalse(float x)
        {
            Assert.That(SprintState.CanSprint(new Vector2(x, 0f), _settings), Is.False);
        }

        [Test]
        public void CanSprint_WithBackwardInput_ReturnsFalse()
        {
            Assert.That(SprintState.CanSprint(Vector2.down, _settings), Is.False);
        }

        [Test]
        public void CanSprint_WithInputBelowMinMagnitude_ReturnsFalse()
        {
            Assert.That(SprintState.CanSprint(Vector2.up * (MinInput - 0.01f), _settings), Is.False);
        }

        [Test]
        public void CanSprint_WithInputAtMinMagnitude_ReturnsTrue()
        {
            Assert.That(SprintState.CanSprint(Vector2.up * MinInput, _settings), Is.True);
        }

        [Test]
        public void CanSprint_WithZeroInput_ReturnsFalse()
        {
            var settings = new LocomotionSettings(MaxAngle, sprintMinInput: 0f);

            Assert.That(SprintState.CanSprint(Vector2.zero, settings), Is.False);
        }

        /// <summary>전방(0, 1)에서 오른쪽(+)으로 <paramref name="degrees"/>만큼 기운 단위 입력.</summary>
        private static Vector2 FromForward(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }
    }
}
