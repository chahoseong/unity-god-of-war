namespace GodOfWar.Input
{
    /// <summary>
    /// 누름/뗌 이벤트를 발행하는 플레이어 버튼 액션.
    /// 각 값의 이름은 GameInputActions 에셋 Player 맵의 액션 이름과 같아야 한다.
    /// </summary>
    public enum PlayerAction
    {
        Sprint,
        LockOn,
        Dodge,
        LightAttack,
        HeavyAttack,
        Guard
    }
}
