using GodOfWar.Combat;
using NUnit.Framework;
using UnityEngine;

namespace GodOfWar.Tests.Combat
{
    public class HitDirectionResolverTests
    {
        // 원점·Yaw 0°에서는 위치 빼기 누락과 월드축 사용 버그가 통과하므로, 피격자를 옮기고 돌려 둔다.
        private static readonly Vector3 DefenderPosition = new Vector3(4f, 0f, -2f);
        private const float DefenderYaw = 90f;
        private const float Distance = 3f;

        private static Vector3 DefenderForward => Quaternion.Euler(0f, DefenderYaw, 0f) * Vector3.forward;

        [Test]
        public void Resolve_WithAttackerAhead_ReturnsFront()
        {
            Assert.That(Resolve(AttackerAt(0f)), Is.EqualTo(HitDirection.Front));
        }

        [Test]
        public void Resolve_WithAttackerBehind_ReturnsBack()
        {
            Assert.That(Resolve(AttackerAt(180f)), Is.EqualTo(HitDirection.Back));
        }

        [Test]
        public void Resolve_WithAttackerOnRight_ReturnsRight()
        {
            Assert.That(Resolve(AttackerAt(90f)), Is.EqualTo(HitDirection.Right));
        }

        [Test]
        public void Resolve_WithAttackerOnLeft_ReturnsLeft()
        {
            Assert.That(Resolve(AttackerAt(-90f)), Is.EqualTo(HitDirection.Left));
        }

        // 정면 구역이 90° 폭인지 확인한다. 내적 부호만으로 정면을 가르면 좌우로 판정된다.
        [Test]
        public void Resolve_WithAttackerThirtyDegreesRightOfAhead_ReturnsFront()
        {
            Assert.That(Resolve(AttackerAt(30f)), Is.EqualTo(HitDirection.Front));
        }

        private static HitDirection Resolve(Vector3 attackerPosition)
        {
            return HitDirectionResolver.Resolve(DefenderPosition, DefenderForward, attackerPosition);
        }

        /// <summary>피격자 정면에서 <paramref name="relativeYaw"/>(도, 오른쪽이 양수)만큼 돈 방향으로 떨어진 공격자 위치.</summary>
        private static Vector3 AttackerAt(float relativeYaw)
        {
            return DefenderPosition + Quaternion.Euler(0f, DefenderYaw + relativeYaw, 0f) * Vector3.forward * Distance;
        }
    }
}
