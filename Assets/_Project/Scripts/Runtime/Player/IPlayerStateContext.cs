using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;

namespace GodOfWar.Player
{
    /// <summary>
    /// 플레이어가 자기 상태들에게 열어 주는 창구(상태 패턴의 Context).
    /// 캐릭터의 구성 요소와 상태용 인프라만 담는다. 상태마다 다른 데이터(설정 등)는 생성자로 받는다.
    /// 외부 객체에는 보이지 않도록 <see cref="PlayerController"/>가 명시적으로 구현한다.
    /// </summary>
    public interface IPlayerStateContext
    {
        /// <summary>상태가 이동을 맡기는 모터. 몸의 위치와 방향도 이 모터의 Transform이 대표한다.</summary>
        CharacterMotor Motor { get; }

        IPlayerInput Input { get; }

        /// <summary>상태가 자기 클립을 재생하는 애니메이터 창구. 레이어 구성은 이 창구만 안다.</summary>
        PlayerAnimator Animator { get; }

        /// <summary>무기 판정. 공격 계열 상태가 타격 구간에 맞춰 연다.</summary>
        Hitbox Hitbox { get; }

        /// <summary>이동 입력의 기준 Yaw(도). 락온 중에는 카메라 보간과 무관하게 대상을 향한다.</summary>
        float MovementYaw { get; }

        /// <summary>상태가 전이를 요청하는 상태 머신.</summary>
        StateMachine Machine { get; }

        /// <summary>상태가 전이 대상 상태를 찾는 창구.</summary>
        IStateLocator States { get; }

        /// <summary>상태가 생성될 때 자신의 진입 트리거를 선언하는 액션 라우터.</summary>
        ActionRouter Router { get; }
    }
}
