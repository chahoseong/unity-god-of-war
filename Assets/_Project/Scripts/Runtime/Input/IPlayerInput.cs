using System;
using UnityEngine;

namespace GodOfWar.Input
{
    /// <summary>
    /// 캐릭터 상태, 카메라 등이 의존하는 플레이어 입력.
    /// 장치 입력을 GameInputActions의 Player 맵 액션 단위로 그대로 중계하며 의미 해석은 하지 않는다.
    /// </summary>
    public interface IPlayerInput
    {
        /// <summary>이동 입력. 크기는 0~1.</summary>
        Vector2 Move { get; }

        /// <summary>시점 입력. 단위는 <see cref="LookType"/>에 따라 다르다.</summary>
        Vector2 Look { get; }

        /// <summary>마지막으로 들어온 <see cref="Look"/> 입력의 단위.</summary>
        LookInputType LookType { get; }

        bool IsHeld(PlayerAction action);

        /// <summary>버튼 액션이 눌리거나 떼어질 때 한 번씩 발생한다. 누르고 있는 동안 반복하지 않는다.</summary>
        event Action<PlayerActionEvent> ActionTriggered;
    }
}
