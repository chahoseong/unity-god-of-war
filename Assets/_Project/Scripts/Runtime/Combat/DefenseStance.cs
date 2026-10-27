namespace GodOfWar.Combat
{
    /// <summary>
    /// 피격자가 공격을 받은 순간 취하고 있는 방어 자세. 저장해 두는 상태가 아니라, 질의할 때마다
    /// 현재 상태와 경과 시간으로 정해지는 값이다.
    /// </summary>
    public enum DefenseStance
    {
        /// <summary>무방비. 맞으면 피격한다.</summary>
        Open,

        /// <summary>회피 판정이 성립하는 동안. 회피 상태라도 무적 구간 밖이면 <see cref="Open"/>이다.</summary>
        Evading,

        /// <summary>가드 중. 막을 수 있는 범위 안의 공격만 막는다.</summary>
        Guarding,

        /// <summary>가드를 막 올린 순간(저스트 패링 윈도우). 막을 수 있는 범위 안의 공격을 세기와 관계없이 튕겨 낸다.</summary>
        Parrying
    }
}
