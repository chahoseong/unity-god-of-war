namespace GodOfWar.Combat
{
    /// <summary>
    /// 피격자가 공격을 받은 순간 취하고 있는 방어 자세. 저장해 두는 상태가 아니라, 질의할 때마다
    /// 현재 상태와 경과 시간으로 정해지는 값이다. 가드가 생기면 값이 늘어난다.
    /// </summary>
    public enum DefenseStance
    {
        /// <summary>무방비. 맞으면 피격한다.</summary>
        Open,

        /// <summary>회피 판정이 성립하는 동안. 회피 상태라도 무적 구간 밖이면 <see cref="Open"/>이다.</summary>
        Evading
    }
}
