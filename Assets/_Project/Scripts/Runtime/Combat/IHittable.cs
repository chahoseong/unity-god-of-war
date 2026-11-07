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
        /// 한 번의 스윙에서 같은 대상에게는 한 번만 호출된다. 단, <see cref="HitOutcome.Evaded"/>를 돌려주면
        /// 칼날이 걸쳐 있는 동안 다음 프레임에도 다시 호출된다.
        /// 모든 칼의 검출이 끝난 뒤 같은 프레임의 LateUpdate 직후에 호출되며, 그 사이 칼이 닫혀도 호출된다.
        /// </summary>
        HitResult ReceiveHit(in AttackInfo attack);

        /// <summary>
        /// 지금 타격을 받을 수 있는지. 거짓이면 타격을 전하지 않고 버리므로 판정도 알림도 없다.
        /// 처치된 캐릭터처럼 칼이 닿아도 맞지 않아야 하는 동안 거짓이다. 타격은 전하기 직전에 이 값을 본다.
        /// </summary>
        bool CanBeHit { get; }
    }
}
