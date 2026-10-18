namespace GodOfWar.Input
{
    /// <summary>
    /// 플레이어 버튼 액션의 누름/뗌 한 건.
    /// </summary>
    public readonly struct PlayerActionEvent
    {
        public PlayerActionEvent(PlayerAction action, ActionPhase phase, double time)
        {
            Action = action;
            Phase = phase;
            Time = time;
        }

        public PlayerAction Action { get; }
        public ActionPhase Phase { get; }

        /// <summary>
        /// 입력 이벤트가 발생한 시각 (<c>Time.realtimeSinceStartupAsDouble</c> 기준).
        /// 프레임 시작 시각이 아니라 실제 입력 시각이므로 패링 판정 같은 타이밍 비교에 쓴다.
        /// </summary>
        public double Time { get; }

        public override string ToString() => $"{Action} {Phase} @ {Time:F3}";
    }
}
