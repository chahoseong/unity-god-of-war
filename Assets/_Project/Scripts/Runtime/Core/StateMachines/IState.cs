namespace GodOfWar.Core.StateMachines
{
    /// <summary>
    /// 상태 머신이 관리하는 단일 행동 상태.
    /// 상태가 필요로 하는 의존성은 생성자로 주입받는다.
    /// </summary>
    public interface IState
    {
        /// <summary>이 상태로 전환된 직후 한 번 호출된다.</summary>
        void Enter();

        /// <summary>이 상태가 활성화된 동안 매 갱신마다 호출된다.</summary>
        void Tick(float deltaTime);

        /// <summary>다른 상태(또는 자기 자신)로 전환되기 직전에 한 번 호출된다.</summary>
        void Exit();
    }
}
