namespace GodOfWar.Combat
{
    /// <summary>
    /// 공격을 받을 수 있는 대상. 플레이어와 적이 똑같이 구현한다.
    /// 가드·패링처럼 결과를 가르는 상태는 피격자 안에 있으므로 판정도 피격자가 내린다.
    /// </summary>
    public interface IHittable
    {
        /// <summary>
        /// 타격을 받아 판정하고 결과를 돌려준다. 피격 반응 같은 상태 전이는 이 안에서 직접 요청한다.
        /// 한 번의 스윙에서 같은 대상에게는 한 번만 호출된다.
        /// </summary>
        HitResult ReceiveHit(in AttackInfo attack);
    }
}
