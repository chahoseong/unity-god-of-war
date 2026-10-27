namespace GodOfWar.Combat
{
    /// <summary>
    /// 피격자가 내린 타격 판정. 가드·패링이 생기면 값이 늘어난다.
    /// </summary>
    public enum HitOutcome
    {
        /// <summary>막지 못하고 맞았다.</summary>
        Hit,

        /// <summary>회피 무적으로 흘렸다. 같은 스윙이 다시 맞힐 수 있도록 맞은 것으로 세지 않는다.</summary>
        Evaded,

        /// <summary>가드로 막았다. 가드가 유지된다.</summary>
        Blocked,

        /// <summary>가드로 막았지만 강한 타격에 자세가 무너졌다.</summary>
        GuardBroken,

        /// <summary>저스트 패링으로 튕겨 냈다.</summary>
        Parried
    }
}
