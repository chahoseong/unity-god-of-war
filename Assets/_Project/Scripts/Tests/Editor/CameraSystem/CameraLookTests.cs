using GodOfWar.CameraSystem;
using GodOfWar.Input;
using NUnit.Framework;
using UnityEngine;

namespace GodOfWar.Tests.CameraSystem
{
    public class CameraLookTests
    {
        private const float MinPitch = -40f;
        private const float MaxPitch = 70f;
        private const float Tolerance = 1e-4f;

        private static readonly Vector2 MouseSensitivity = new Vector2(0.5f, 0.25f);
        private static readonly Vector2 RateSpeed = new Vector2(100f, 50f);

        private static CameraLookSettings CreateSettings(bool invertY = false)
        {
            return new CameraLookSettings(MouseSensitivity, RateSpeed, invertY, MinPitch, MaxPitch);
        }

        [Test]
        public void ApplyLook_WithDeltaInput_IgnoresDeltaTime()
        {
            CameraLookSettings settings = CreateSettings();
            var look = new Vector2(10f, 4f);

            Vector2 shortFrame = CameraLook.ApplyLook(Vector2.zero, look, LookInputType.Delta, settings, 0.01f);
            Vector2 longFrame = CameraLook.ApplyLook(Vector2.zero, look, LookInputType.Delta, settings, 0.1f);

            Assert.That(shortFrame.x, Is.EqualTo(longFrame.x).Within(Tolerance));
            Assert.That(shortFrame.y, Is.EqualTo(longFrame.y).Within(Tolerance));
        }

        [Test]
        public void ApplyLook_WithDeltaInput_ScalesBySensitivity()
        {
            var look = new Vector2(10f, 4f);

            Vector2 result = CameraLook.ApplyLook(Vector2.zero, look, LookInputType.Delta, CreateSettings(), 0.016f);

            Assert.That(result.x, Is.EqualTo(look.x * MouseSensitivity.x).Within(Tolerance));
        }

        [Test]
        public void ApplyLook_WithRateInput_ScalesByDeltaTime()
        {
            const float deltaTime = 0.1f;

            Vector2 result = CameraLook.ApplyLook(
                Vector2.zero, Vector2.right, LookInputType.Rate, CreateSettings(), deltaTime);

            Assert.That(result.x, Is.EqualTo(RateSpeed.x * deltaTime).Within(Tolerance));
        }

        [Test]
        public void ApplyLook_WithUpwardInput_DecreasesPitch()
        {
            // Unity Euler X는 양수가 내려다보는 방향이므로, 올려다보면 Pitch가 줄어야 한다.
            Vector2 result = CameraLook.ApplyLook(
                Vector2.zero, Vector2.up, LookInputType.Delta, CreateSettings(), 0.016f);

            Assert.That(result.y, Is.EqualTo(-MouseSensitivity.y).Within(Tolerance));
        }

        [Test]
        public void ApplyLook_WithInvertYAndUpwardInput_IncreasesPitch()
        {
            Vector2 result = CameraLook.ApplyLook(
                Vector2.zero, Vector2.up, LookInputType.Delta, CreateSettings(invertY: true), 0.016f);

            Assert.That(result.y, Is.EqualTo(MouseSensitivity.y).Within(Tolerance));
        }

        [Test]
        public void ApplyLook_WithDownwardInputPastMaxPitch_ClampsToMaxPitch()
        {
            // 아래쪽으로 크게 입력해 최대 Pitch(내려다봄)를 넘긴다.
            Vector2 result = CameraLook.ApplyLook(
                new Vector2(0f, MaxPitch - 1f), new Vector2(0f, -100f), LookInputType.Delta, CreateSettings(), 0.016f);

            Assert.That(result.y, Is.EqualTo(MaxPitch).Within(Tolerance));
        }

        [Test]
        public void ApplyLook_WithUpwardInputPastMinPitch_ClampsToMinPitch()
        {
            // 위쪽으로 크게 입력해 최소 Pitch(올려다봄)를 넘긴다.
            Vector2 result = CameraLook.ApplyLook(
                new Vector2(0f, MinPitch + 1f), new Vector2(0f, 100f), LookInputType.Delta, CreateSettings(), 0.016f);

            Assert.That(result.y, Is.EqualTo(MinPitch).Within(Tolerance));
        }
    }
}
