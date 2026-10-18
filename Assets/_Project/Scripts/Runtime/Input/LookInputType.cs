namespace GodOfWar.Input
{
    /// <summary>
    /// <see cref="IPlayerInput.Look"/> 값의 단위. 카메라는 이 값에 따라 적용 공식을 고른다.
    /// </summary>
    public enum LookInputType
    {
        /// <summary>
        /// 회전 속도의 비율(-1~1, 스틱 등). 초당 회전 속도와 deltaTime을 곱해 적용한다.
        /// </summary>
        Rate,

        /// <summary>
        /// 이번 프레임의 이동량(마우스 등). 이미 프레임 단위 값이므로 deltaTime을 곱하지 않는다.
        /// </summary>
        Delta
    }
}
