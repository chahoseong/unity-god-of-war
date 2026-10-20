using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 이동 입력에서 판정한 회피 방향.
    /// 입력에 가장 가까운 4방향 클립(<see cref="Cardinal"/>)을 고르고, 남는 각도(<see cref="ResidualYaw"/>)만큼
    /// 몸을 돌려 클립 방향과 몸 회전의 합이 입력 방향(<see cref="InputYaw"/>)이 되게 한다.
    /// 모든 각도는 이동 기준(카메라) 정면에 대한 상대 Yaw(도)다.
    /// </summary>
    public readonly struct DodgeDirection
    {
        private DodgeDirection(DodgeCardinal cardinal, float inputYaw, float residualYaw)
        {
            Cardinal = cardinal;
            InputYaw = inputYaw;
            ResidualYaw = residualYaw;
        }

        /// <summary>재생할 회피 클립의 방향.</summary>
        public DodgeCardinal Cardinal { get; }

        /// <summary>입력 방향. 실제로 이동할 방향이다. 범위는 -180~180.</summary>
        public float InputYaw { get; }

        /// <summary>클립 방향에서 입력 방향까지 몸을 돌릴 각도. 범위는 -45~45.</summary>
        public float ResidualYaw { get; }

        /// <summary>
        /// 입력 크기가 <paramref name="minInput"/> 이상이면 회피 방향을 판정한다.
        /// 정확한 대각 입력처럼 두 방향이 같은 거리면 전후(Forward/Backward)를 우선한다.
        /// </summary>
        public static bool TryFromInput(Vector2 move, float minInput, out DodgeDirection direction)
        {
            // 크기를 먼저 걸러 영벡터의 각도를 계산하지 않는다.
            float magnitude = move.magnitude;
            if (magnitude < minInput || magnitude <= 0f)
            {
                direction = default;
                return false;
            }

            DodgeCardinal cardinal;
            float cardinalYaw;
            if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
            {
                cardinal = move.x > 0f ? DodgeCardinal.Right : DodgeCardinal.Left;
                cardinalYaw = move.x > 0f ? 90f : -90f;
            }
            else
            {
                cardinal = move.y >= 0f ? DodgeCardinal.Forward : DodgeCardinal.Backward;
                cardinalYaw = move.y >= 0f ? 0f : 180f;
            }

            float inputYaw = Mathf.Atan2(move.x, move.y) * Mathf.Rad2Deg;
            // 후방(180°) 근처에서 -180/180 경계를 넘어도 최단 각도가 되도록 DeltaAngle로 구한다.
            float residualYaw = Mathf.DeltaAngle(cardinalYaw, inputYaw);

            direction = new DodgeDirection(cardinal, inputYaw, residualYaw);
            return true;
        }
    }
}
