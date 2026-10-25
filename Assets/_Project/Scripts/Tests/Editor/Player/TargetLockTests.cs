using GodOfWar.Player;
using NUnit.Framework;
using UnityEngine;

namespace GodOfWar.Tests.Player
{
    public class TargetLockTests
    {
        private const float AcquireDistance = 15f;
        private const float ViewHalfAngle = 60f;
        private const float KeepDistance = 20f;

        private static LockOnSettings CreateSettings(float distanceWeight = 1f)
        {
            return new LockOnSettings(AcquireDistance, ViewHalfAngle, distanceWeight, KeepDistance);
        }

        // viewYaw 0(+Z 정면) 기준으로 각도·수평 거리만큼 떨어진 지점.
        private static Vector3 PointAt(float yaw, float distance, float height = 0f)
        {
            return Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * distance + Vector3.up * height;
        }

        [Test]
        public void TryScore_WithTargetBeyondAcquireDistance_ReturnsFalse()
        {
            bool result = TargetLock.TryScore(Vector3.zero, 0f, PointAt(0f, 16f), CreateSettings(), out _);

            Assert.That(result, Is.False);
        }

        [Test]
        public void TryScore_WithTargetAtAcquireDistance_ReturnsTrue()
        {
            bool result = TargetLock.TryScore(Vector3.zero, 0f, PointAt(0f, AcquireDistance), CreateSettings(), out _);

            Assert.That(result, Is.True);
        }

        // 높은 곳의 대상이 높이 차 때문에 획득 거리를 벗어나면 안 된다. 직선거리는 약 17.2m다.
        [Test]
        public void TryScore_WithTargetBeyondAcquireDistanceOnlyByHeight_ReturnsTrue()
        {
            bool result = TargetLock.TryScore(
                Vector3.zero, 0f, PointAt(0f, 14f, height: 10f), CreateSettings(), out _);

            Assert.That(result, Is.True);
        }

        [Test]
        public void TryScore_WithTargetOutsideViewAngle_ReturnsFalse()
        {
            bool result = TargetLock.TryScore(Vector3.zero, 0f, PointAt(70f, 5f), CreateSettings(), out _);

            Assert.That(result, Is.False);
        }

        // 그룹 전투에서는 화면 중앙의 먼 적보다 가까이 붙은 적이 먼저 잡혀야 한다.
        [Test]
        public void TryScore_NearOffCenterVsFarCentered_ScoresNearLower()
        {
            LockOnSettings settings = CreateSettings(distanceWeight: 1f);

            TargetLock.TryScore(Vector3.zero, 0f, PointAt(27f, 3f), settings, out float near);
            TargetLock.TryScore(Vector3.zero, 0f, PointAt(2f, 13f), settings, out float far);

            Assert.That(near, Is.LessThan(far));
        }

        // 새로 잡을 수 없는 거리(획득 거리 밖)라도 이미 잡은 대상은 유지해 경계에서 켜졌다 꺼지기를 반복하지 않는다.
        [Test]
        public void IsWithinKeepDistance_WithTargetBetweenAcquireAndKeepDistance_ReturnsTrue()
        {
            bool result = TargetLock.IsWithinKeepDistance(Vector3.zero, PointAt(0f, 17f), CreateSettings());

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsWithinKeepDistance_WithTargetAtKeepDistance_ReturnsTrue()
        {
            bool result = TargetLock.IsWithinKeepDistance(Vector3.zero, PointAt(0f, KeepDistance), CreateSettings());

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsWithinKeepDistance_WithTargetBeyondKeepDistance_ReturnsFalse()
        {
            bool result = TargetLock.IsWithinKeepDistance(Vector3.zero, PointAt(0f, 21f), CreateSettings());

            Assert.That(result, Is.False);
        }
    }
}
