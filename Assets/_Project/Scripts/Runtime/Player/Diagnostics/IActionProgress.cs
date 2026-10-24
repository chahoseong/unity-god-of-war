namespace GodOfWar.Player.Diagnostics
{
    /// <summary>
    /// 길이가 정해진 동작을 하는 상태가 진행도를 공개하는 읽기 전용 계약.
    /// 디버그 표시만 읽으며, 이 계약을 구현한 상태만 동작 진행 바가 생긴다.
    /// </summary>
    public interface IActionProgress
    {
        /// <summary>진행 바에 표시할 동작 이름.</summary>
        string ActionName { get; }

        /// <summary>동작 시작부터 지난 시간(초).</summary>
        float Elapsed { get; }

        /// <summary>동작이 끝까지 진행될 때의 길이(초).</summary>
        float Duration { get; }
    }
}
