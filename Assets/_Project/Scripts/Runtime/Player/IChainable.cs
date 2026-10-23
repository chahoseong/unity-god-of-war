using GodOfWar.Input;

namespace GodOfWar.Player
{
    /// <summary>
    /// 같은 계열의 후속 행동(콤보 다음 타, 스텝 → 구르기 등)을 가진 상태의 내부 연계 계약 (ADR-004 4절).
    /// 액션 라우터는 라우팅 테이블을 보기 전에 현재 상태에 먼저 질의한다.
    /// </summary>
    public interface IChainable
    {
        /// <summary>
        /// <paramref name="e"/>로 연계를 이어 가면 true를 반환한다.
        /// 무시한 입력에는 false를 반환해 라우터가 이어서 판정하게 한다.
        /// </summary>
        bool TryChain(PlayerActionEvent e);
    }
}
