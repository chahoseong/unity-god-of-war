using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GodOfWar.Input
{
    /// <summary>
    /// GameInputActions의 Player 맵을 구독해 <see cref="IPlayerInput"/>으로 중계한다.
    /// 컴포넌트의 활성 상태가 Player 맵의 활성 상태를 결정한다.
    /// 소비자가 같은 프레임의 입력을 보도록 다른 게임플레이 컴포넌트보다 먼저 갱신한다.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.PlayerInputReader)]
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInput
    {
        private const string PlayerMapName = "Player";
        private const string MoveActionName = "Move";
        private const string LookActionName = "Look";

        [Tooltip("비워 두면 Project-wide Actions(InputSystem.actions)를 사용한다.")]
        [SerializeField] private InputActionAsset _actions;

        private InputActionMap _playerMap;
        private InputAction _move;
        private InputAction _look;

        // PlayerAction 값을 인덱스로 쓴다. 구독 해제가 같은 인스턴스를 가리키도록 핸들러는 Awake에서 한 번만 만든다.
        private InputAction[] _buttons;
        private Action<InputAction.CallbackContext>[] _pressedHandlers;
        private Action<InputAction.CallbackContext>[] _releasedHandlers;

        public event Action<PlayerActionEvent> ActionTriggered;

        public Vector2 Move => _move.ReadValue<Vector2>();
        public Vector2 Look => _look.ReadValue<Vector2>();
        public LookInputType LookType { get; private set; } = LookInputType.Rate;

        public bool IsHeld(PlayerAction action) => _buttons[(int)action].IsPressed();

        private void Awake()
        {
            InputActionAsset asset = _actions != null ? _actions : InputSystem.actions;
            if (asset == null)
            {
                throw new InvalidOperationException(
                    "No input actions asset. Assign one or set Project-wide Actions.");
            }

            _playerMap = asset.FindActionMap(PlayerMapName, throwIfNotFound: true);
            _move = _playerMap.FindAction(MoveActionName, throwIfNotFound: true);
            _look = _playerMap.FindAction(LookActionName, throwIfNotFound: true);

            var actions = (PlayerAction[])Enum.GetValues(typeof(PlayerAction));
            _buttons = new InputAction[actions.Length];
            _pressedHandlers = new Action<InputAction.CallbackContext>[actions.Length];
            _releasedHandlers = new Action<InputAction.CallbackContext>[actions.Length];
            foreach (PlayerAction action in actions)
            {
                int index = (int)action;
                _buttons[index] = _playerMap.FindAction(action.ToString(), throwIfNotFound: true);
                _pressedHandlers[index] = context => Raise(action, ActionPhase.Pressed, context.time);
                _releasedHandlers[index] = context => Raise(action, ActionPhase.Released, context.time);
            }
        }

        private void OnEnable()
        {
            // Awake가 예외로 중단된 경우 원인 예외만 남기고 후속 NullReferenceException을 내지 않는다.
            if (_playerMap == null)
            {
                return;
            }

            for (int i = 0; i < _buttons.Length; i++)
            {
                _buttons[i].performed += _pressedHandlers[i];
                _buttons[i].canceled += _releasedHandlers[i];
            }

            _look.performed += OnLookPerformed;
            _playerMap.Enable();
        }

        private void OnDisable()
        {
            if (_playerMap == null)
            {
                return;
            }

            // 맵을 먼저 꺼야 눌려 있던 버튼의 canceled가 Released로 발행되어 소비자의 홀드 상태가 남지 않는다.
            _playerMap.Disable();
            _look.performed -= OnLookPerformed;

            for (int i = 0; i < _buttons.Length; i++)
            {
                _buttons[i].performed -= _pressedHandlers[i];
                _buttons[i].canceled -= _releasedHandlers[i];
            }
        }

        private void Update()
        {
            if (_playerMap == null)
            {
                return;
            }

            // Input System은 눌려 있는 동안 콜백을 반복하지 않으므로 유지 단계는 직접 발행한다.
            double time = Time.realtimeSinceStartupAsDouble;
            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i].IsPressed())
                {
                    Raise((PlayerAction)i, ActionPhase.Held, time);
                }
            }
        }

        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            LookType = context.control.device is Pointer ? LookInputType.Delta : LookInputType.Rate;
        }

        private void Raise(PlayerAction action, ActionPhase phase, double time)
        {
            ActionTriggered?.Invoke(new PlayerActionEvent(action, phase, time));
        }
    }
}
