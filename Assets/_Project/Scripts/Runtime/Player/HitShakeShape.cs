namespace GodOfWar.Player
{
    /// <summary>
    /// 타격 흔들림의 모양. Cinemachine Impulse의 기본 모양 중 곡선 없이 쓸 수 있는 것만 둔다.
    /// </summary>
    public enum HitShakeShape
    {
        /// <summary>맞는 순간 가장 크게 튕긴 뒤 잦아든다. 날카로운 타격.</summary>
        Recoil,

        /// <summary>부드럽게 한 번 눌렸다 돌아온다. 가벼운 접촉.</summary>
        Bump,

        /// <summary>반대쪽부터 크게 출렁인다. 자세가 무너지는 충격.</summary>
        Explosion,

        /// <summary>길게 떨린다.</summary>
        Rumble
    }
}
