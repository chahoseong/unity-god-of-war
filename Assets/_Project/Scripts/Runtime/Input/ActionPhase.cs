namespace GodOfWar.Input
{
    public enum ActionPhase
    {
        Pressed,
        Released,

        /// <summary>버튼이 눌려 있는 프레임마다 발생한다. 누른 프레임에도 <see cref="Pressed"/> 다음에 발생한다.</summary>
        Held
    }
}
