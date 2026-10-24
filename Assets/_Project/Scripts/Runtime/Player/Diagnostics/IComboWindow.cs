namespace GodOfWar.Player.Diagnostics
{
    /// <summary>
    /// 연계 구간을 가진 상태가 내부 타이밍을 공개하는 읽기 전용 계약.
    /// 디버그 표시만 읽으며, 액션 라우터의 판정에는 쓰지 않는다.
    /// </summary>
    public interface IComboWindow
    {
        /// <summary>지금 후속 입력을 다음 타로 받는 구간인지.</summary>
        bool IsComboWindowOpen { get; }

        /// <summary>이미 받은 다음 타가 실행을 기다리고 있는지.</summary>
        bool HasQueuedFollowUp { get; }
    }
}
