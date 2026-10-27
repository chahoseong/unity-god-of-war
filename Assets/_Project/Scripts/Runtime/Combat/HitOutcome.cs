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
        Evaded
    }
}
