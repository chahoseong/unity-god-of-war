namespace GodOfWar.Player
{
    /// <summary>
    /// 상태가 액션 라우터에 응답하는 캔슬 허용 계약 (ADR-004 3절).
    /// 모든 플레이어 상태는 이 계약을 명시적으로 선언해야 한다.
    /// </summary>
    public interface ICancellable
    {
        /// <summary>지금 다른 액션이 이 상태에 끼어들어(캔슬) 전이할 수 있는지.</summary>
        bool IsCancelWindowOpen { get; }
    }
}
