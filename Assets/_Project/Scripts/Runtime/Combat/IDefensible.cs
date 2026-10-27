namespace GodOfWar.Combat
{
    /// <summary>
    /// 상태가 피격 판정에 응답하는 방어 자세 계약. 판정 규칙은 <see cref="HitOutcomeResolver"/>가 갖고,
    /// 상태는 지금 자기가 어떤 자세인지만 답한다.
    /// 피격될 수 있는 캐릭터의 모든 상태가 구현하며, 막거나 피하지 않는 상태는 <see cref="DefenseStance.Open"/>을 응답한다.
    /// </summary>
    public interface IDefensible
    {
        /// <summary>지금 이 순간의 방어 자세.</summary>
        DefenseStance DefenseStance { get; }
    }
}
