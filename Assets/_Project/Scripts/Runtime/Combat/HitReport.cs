namespace GodOfWar.Combat
{
    /// <summary>
    /// 판정이 끝난 타격 한 번의 통지. 누가 누구를 어떻게 쳤고 피격자가 어떻게 판정했는지 함께 담는다.
    /// 판정(<see cref="HitResult"/>)은 피격자가 내리고, 나머지는 타격을 전한 쪽이 이미 아는 사실이라 전한 쪽이 채운다.
    /// </summary>
    public readonly struct HitReport
    {
        public HitReport(IHittable target, in AttackInfo attack, HitResult result)
        {
            Target = target;
            Attack = attack;
            Result = result;
        }

        /// <summary>타격을 받은 대상.</summary>
        public IHittable Target { get; }

        public AttackInfo Attack { get; }

        /// <summary>피격자가 내린 판정.</summary>
        public HitResult Result { get; }
    }
}
