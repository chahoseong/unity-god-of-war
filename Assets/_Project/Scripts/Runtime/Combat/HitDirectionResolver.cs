using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 공격자가 피격자의 어느 쪽에 있는지로 피격 방향을 판정한다.
    /// 수평면을 피격자 정면 기준 90°씩 네 구역으로 나눈다.
    /// </summary>
    public static class HitDirectionResolver
    {
        // 정면·후방 구역의 반각(도). 나머지가 좌우 구역이다.
        private const float SectorHalfAngle = 45f;

        /// <summary>
        /// <paramref name="defenderForward"/>를 바라보는 피격자에게 <paramref name="attackerPosition"/>의 공격자가 어느 쪽에 있는지 판정한다.
        /// 높이 차이는 무시한다. 두 위치가 수평으로 겹치면 정면으로 본다.
        /// </summary>
        public static HitDirection Resolve(Vector3 defenderPosition, Vector3 defenderForward, Vector3 attackerPosition)
        {
            Vector3 toAttacker = attackerPosition - defenderPosition;
            toAttacker.y = 0f;
            defenderForward.y = 0f;

            // Y축 기준 시계 방향(위에서 볼 때 오른쪽)이 양수다.
            float angle = Vector3.SignedAngle(defenderForward, toAttacker, Vector3.up);
            float absAngle = Mathf.Abs(angle);

            if (absAngle <= SectorHalfAngle)
            {
                return HitDirection.Front;
            }

            if (absAngle >= 180f - SectorHalfAngle)
            {
                return HitDirection.Back;
            }

            return angle > 0f ? HitDirection.Right : HitDirection.Left;
        }
    }
}
