namespace GodOfWar.Core.StateMachines
{
    /// <summary>
    /// 상태 머신이 관리하는 단일 행동 상태.
    /// 상태가 필요로 하는 의존성은 생성자로 주입받는다.
    /// </summary>
    /// <remarks>
    /// 상태는 두 구간을 겪는다. 소유자(상태 머신을 가진 컴포넌트)가 켜져 있는 구간(<see cref="OnEnable"/>~<see cref="OnDisable"/>)과,
    /// 그 안에서 현재 상태인 구간(<see cref="Enter"/>~<see cref="Exit"/>)이다.
    /// </remarks>
    public interface IState
    {
        /// <summary>이 상태로 전환된 직후 한 번 호출된다.</summary>
        void Enter();

        /// <summary>이 상태가 현재 상태인 동안 매 갱신마다 호출된다.</summary>
        void Tick(float deltaTime);

        /// <summary>다른 상태(또는 자기 자신)로 전환되기 직전에 한 번 호출된다.</summary>
        void Exit();

        /// <summary>
        /// 소유자가 켜질 때 현재 상태인지와 관계없이 등록된 모든 상태에 호출된다.
        /// 현재 상태가 아닐 때도 들어야 하는 구독은 여기서 걸고 <see cref="OnDisable"/>에서 푼다.
        /// </summary>
        void OnEnable()
        {
        }

        /// <summary>
        /// 소유자가 꺼질 때 현재 상태인지와 관계없이 등록된 모든 상태에 호출된다. 소유자가 파괴될 때도 먼저 호출된다.
        /// </summary>
        void OnDisable()
        {
        }
    }
}
