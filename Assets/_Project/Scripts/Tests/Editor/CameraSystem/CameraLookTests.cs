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

        private const float LockPitchBias = 10f;
        private const float LockMinPitch = -20f;
        private const float LockMaxPitch = 35f;
        private const float LockHoldDistance = 1f;

        private static CameraLookSettings CreateSettings(bool invertY = false)
        {
            return new CameraLookSettings(MouseSensitivity, RateSpeed, invertY, MinPitch, MaxPitch);
        }

        private static LockOnCameraSettings CreateLockOnSettings()
        {
            return new LockOnCameraSettings(LockPitchBias, LockMinPitch, LockMaxPitch, LockHoldDistance);
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

        [Test]
        public void ComputeLockAngles_WithTargetToTheRight_ReturnsYawTowardTarget()
        {
            Vector2 result = CameraLook.ComputeLockAngles(
                Vector3.zero, new Vector3(5f, 0f, 0f), Vector2.zero, CreateLockOnSettings());

            Assert.That(result.x, Is.EqualTo(90f).Within(Tolerance));
        }

        // 높이 차가 없으면 보정값만큼 내려다봐 대상이 화면 중앙보다 위에 온다.
        [Test]
        public void ComputeLockAngles_WithTargetAtPivotHeight_ReturnsPitchBias()
        {
            Vector2 result = CameraLook.ComputeLockAngles(
                Vector3.zero, new Vector3(0f, 0f, 6f), Vector2.zero, CreateLockOnSettings());

            Assert.That(result.y, Is.EqualTo(LockPitchBias).Within(Tolerance));
        }

        [Test]
        public void ComputeLockAngles_WithTargetAbovePivot_ReturnsPitchBiasMinusElevation()
        {
            // 5m 앞, 1m 위. 고저각은 약 11.3°로 락온 Pitch 범위 안에 든다.
            Vector2 result = CameraLook.ComputeLockAngles(
                Vector3.zero, new Vector3(0f, 1f, 5f), Vector2.zero, CreateLockOnSettings());

            float elevation = Mathf.Atan2(1f, 5f) * Mathf.Rad2Deg;
            Assert.That(result.y, Is.EqualTo(LockPitchBias - elevation).Within(Tolerance));
        }

        // 가까운 거대 보스를 올려다보다 카메라가 바닥에 닿지 않게 한다.
        [Test]
        public void ComputeLockAngles_WithTargetFarAbovePivot_ClampsToMinPitch()
        {
            // 3m 앞, 2.4m 위. 제한이 없으면 약 -28.7°다.
            Vector2 result = CameraLook.ComputeLockAngles(
                Vector3.zero, new Vector3(0f, 2.4f, 3f), Vector2.zero, CreateLockOnSettings());

            Assert.That(result.y, Is.EqualTo(LockMinPitch).Within(Tolerance));
        }

        [Test]
        public void ComputeLockAngles_WithTargetFarBelowPivot_ClampsToMaxPitch()
        {
            // 3m 앞, 3m 아래. 제한이 없으면 55°다.
            Vector2 result = CameraLook.ComputeLockAngles(
                Vector3.zero, new Vector3(0f, -3f, 3f), Vector2.zero, CreateLockOnSettings());

            Assert.That(result.y, Is.EqualTo(LockMaxPitch).Within(Tolerance));
        }

        // 대상을 스쳐 지나갈 때 방향이 뒤집혀 화면이 도는 것을 막는다.
        [Test]
        public void ComputeLockAngles_WithTargetWithinHoldDistance_ReturnsPrevious()
        {
            var previous = new Vector2(30f, 5f);

            Vector2 result = CameraLook.ComputeLockAngles(
                Vector3.zero, new Vector3(LockHoldDistance * 0.5f, 0f, 0f), previous, CreateLockOnSettings());

            Assert.That(result, Is.EqualTo(previous));
        }

        [Test]
        public void ComputeLockAngles_WithTargetAtHoldDistance_ReturnsAnglesTowardTarget()
        {
            Vector2 result = CameraLook.ComputeLockAngles(
                Vector3.zero, new Vector3(LockHoldDistance, 0f, 0f), new Vector2(30f, 5f), CreateLockOnSettings());

            Assert.That(Vector2.Distance(result, new Vector2(90f, LockPitchBias)), Is.LessThan(Tolerance));
        }
    }
}
