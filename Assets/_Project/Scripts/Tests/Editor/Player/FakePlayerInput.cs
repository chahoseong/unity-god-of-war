using System;
using GodOfWar.Input;
using UnityEngine;

namespace GodOfWar.Tests.Player
{
    /// <summary>버튼 이벤트를 테스트에서 직접 발생시키는 입력 테스트 더블.</summary>
    internal sealed class FakePlayerInput : IPlayerInput
    {
        public Vector2 Move { get; set; }
        public Vector2 Look { get; set; }
        public LookInputType LookType { get; set; }

        public event Action<PlayerActionEvent> ActionTriggered;

        public bool IsHeld(PlayerAction action) => false;

        public void Raise(PlayerAction action, ActionPhase phase = ActionPhase.Pressed)
        {
            ActionTriggered?.Invoke(new PlayerActionEvent(action, phase, 0d));
        }
    }
}
