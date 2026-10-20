using GodOfWar.Player.States;
using NUnit.Framework;
using UnityEngine;

namespace GodOfWar.Tests.Player.States
{
    public class DodgeDirectionTests
    {
        private const float MinInput = 0.1f;
        private const float Tolerance = 1e-3f;

        [Test]
        public void TryFromInput_WithZeroInput_ReturnsFalse()
        {
            // MinInput이 0이어도 영벡터는 기각돼야 한다 (각도 계산 가드).
            Assert.That(DodgeDirection.TryFromInput(Vector2.zero, 0f, out _), Is.False);
        }

        [Test]
        public void TryFromInput_WithMagnitudeBelowMinInput_ReturnsFalse()
        {
            Vector2 move = Vector2.right * (MinInput - 0.01f);

            Assert.That(DodgeDirection.TryFromInput(move, MinInput, out _), Is.False);
        }

        [Test]
        public void TryFromInput_WithMagnitudeAtMinInput_ReturnsTrue()
        {
            Vector2 move = Vector2.right * MinInput;

            Assert.That(DodgeDirection.TryFromInput(move, MinInput, out _), Is.True);
        }

        [TestCase(0f, 1f, DodgeCardinal.Forward)]
        [TestCase(1f, 0f, DodgeCardinal.Right)]
        [TestCase(0f, -1f, DodgeCardinal.Backward)]
        [TestCase(-1f, 0f, DodgeCardinal.Left)]
        public void TryFromInput_WithCardinalInput_ReturnsMatchingCardinal(
            float x, float y, DodgeCardinal expected)
        {
            DodgeDirection.TryFromInput(new Vector2(x, y), MinInput, out DodgeDirection direction);

            Assert.That(direction.Cardinal, Is.EqualTo(expected));
        }

        [TestCase(0f, 1f)]
        [TestCase(1f, 0f)]
        [TestCase(0f, -1f)]
        [TestCase(-1f, 0f)]
        public void TryFromInput_WithCardinalInput_ReturnsZeroResidualYaw(float x, float y)
        {
            DodgeDirection.TryFromInput(new Vector2(x, y), MinInput, out DodgeDirection direction);

            Assert.That(direction.ResidualYaw, Is.EqualTo(0f).Within(Tolerance));
        }

        // 키보드 W+D처럼 두 축이 정확히 같은 입력. 클립 선택이 경계에서 흔들리지 않아야 한다.
        [TestCase(1f, 1f, DodgeCardinal.Forward)]
        [TestCase(-1f, 1f, DodgeCardinal.Forward)]
        [TestCase(1f, -1f, DodgeCardinal.Backward)]
        [TestCase(-1f, -1f, DodgeCardinal.Backward)]
        public void TryFromInput_WithExactDiagonalInput_ReturnsForwardOrBackward(
            float x, float y, DodgeCardinal expected)
        {
            DodgeDirection.TryFromInput(new Vector2(x, y).normalized, MinInput, out DodgeDirection direction);

            Assert.That(direction.Cardinal, Is.EqualTo(expected));
        }

        // 입력 각도는 ±180° 경계 바로 안쪽이지만, 후방 클립(180°) 기준 잔여는 ±10°여야 한다.
        [TestCase(170f, -10f)]
        [TestCase(-170f, 10f)]
        public void TryFromInput_WithInputNearBackward_ReturnsWrappedResidualYaw(
            float inputYaw, float expectedResidual)
        {
            DodgeDirection.TryFromInput(FromForward(inputYaw), MinInput, out DodgeDirection direction);

            Assert.That(direction.ResidualYaw, Is.EqualTo(expectedResidual).Within(Tolerance));
        }

        [TestCase(30f, 0f)]
        [TestCase(-40f, 0f)]
        [TestCase(120f, 90f)]
        [TestCase(60f, 90f)]
        [TestCase(-100f, -90f)]
        [TestCase(-150f, 180f)]
        [TestCase(150f, 180f)]
        public void TryFromInput_WithOffAxisInput_CardinalPlusResidualEqualsInputYaw(
            float inputYaw, float expectedCardinalYaw)
        {
            DodgeDirection.TryFromInput(FromForward(inputYaw), MinInput, out DodgeDirection direction);

            float reconstructed = expectedCardinalYaw + direction.ResidualYaw;
            Assert.That(Mathf.DeltaAngle(reconstructed, inputYaw), Is.EqualTo(0f).Within(Tolerance));
        }

        /// <summary>전방(0, 1)에서 오른쪽(+)으로 <paramref name="degrees"/>만큼 기운 단위 입력.</summary>
        private static Vector2 FromForward(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }
    }
}
