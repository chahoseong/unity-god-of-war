namespace GodOfWar.Combat
{
    /// <summary>
    /// 피격자가 공격자에게 돌려주는 타격 결과.
    /// 판정 외에 공격자에게 알릴 정보가 생기면 이 구조체에 덧붙인다.
    /// </summary>
    public readonly struct HitResult
    {
        public HitResult(HitOutcome outcome)
        {
            Outcome = outcome;
        }

        public HitOutcome Outcome { get; }
    }
}
