namespace GodOfWar.Core.StateMachines
{
    /// <summary>
    /// 상태가 전환 대상 상태를 타입으로 찾는 질의 창구.
    /// 상태 인스턴스는 타입당 하나만 등록된다고 가정한다.
    /// </summary>
    public interface IStateLocator
    {
        /// <summary>등록된 <typeparamref name="T"/> 상태를 반환한다. 등록되지 않았으면 예외를 던진다.</summary>
        T Get<T>() where T : class, IState;
    }
}
