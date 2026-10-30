using System;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using GodOfWar.Player.Routing;
using GodOfWar.Player.Settings;
using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 가드 상태. Guard 버튼이 눌려 있는 동안 유지되고, 떼면 <see cref="LocomotionState"/>로 돌아간다.
    /// 다리는 이동 클립으로 걷기 속도의 스트레이프를 하고, 상체는 Guard 레이어에서 가드 자세를 덮어쓴다.
    /// 항상 이동 기준(카메라) 정면을 바라본다.
    /// 눌려 있는 Guard 입력(Held)으로 진입하므로, 다른 행동 중에 누르고 있으면 받을 수 있게 된 순간 가드한다.
    /// 가드 중에는 다른 액션이 끼어들 수 없고, 그동안 누른 버튼은 선입력으로 남아 가드를 풀면 실행된다.
    /// 공격을 막으면 <see cref="BlockState"/>로 반응하고, 그동안 버튼을 계속 누르고 있었으면 가드를 다시 올리지 않고 돌아온다.
    /// Guard 버튼을 새로 누르고 들어오면 누른 순간부터 잠깐 저스트 패링 윈도우가 열린다(<see cref="ParryWindow"/>).
    /// 누르고 있기만 해서 들어오거나 막기에서 돌아오면 윈도우가 열리지 않는다.
    /// 저스트 패링에 쓴 누름은 끝난 것으로 보아, 그 버튼을 떼기 전까지는 누르고 있어도(Held) 가드에 들어가지 않는다.
    /// </summary>
    public sealed class GuardState : IState, ICancellable, IChainable, IDefensible
    {
        private readonly IPlayerStateContext _context;
        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly IPlayerInput _input;
        private readonly CharacterMotor _motor;
        private readonly CharacterClock _clock;
        private readonly PlayerAnimator _animator;
        private readonly LocomotionSettings _locomotionSettings;
        private readonly GuardSettings _settings;
        private readonly ParryWindow _parryWindow;

        private LocomotionState _locomotion;

        // 다음 Enter에 쓸 진입 방식. 새로 누른 가드면 누른 시각(캐릭터 시계)을, 막기에서 돌아오면 그 사실을 넘긴다.
        private bool _hasPendingPress;
        private double _pendingPressTime;
        private bool _isReturningFromBlock;

        // 저스트 패링에 쓴 누름이 아직 이어지는지. 가드 상태가 아닐 때도 버튼을 떼는 순간 풀려야 하므로 입력을 직접 듣는다.
        private bool _isHoldSpentOnParry;

        public GuardState(IPlayerStateContext context, GuardSettings settings, LocomotionSettings locomotionSettings)
        {
            _context = context;
            _machine = context.Machine;
            _locator = context.States;
            _input = context.Input;
            _motor = context.Motor;
            _clock = context.Clock;
            _animator = context.Animator;
            _locomotionSettings = locomotionSettings;
            _settings = settings;
            _parryWindow = new ParryWindow(_settings.ParryWindow, _settings.ParryCooldown);

            // 새로 누른 가드는 누른 시각으로 패링 윈도우를 연다. 선입력으로 미뤄졌다가 들어와도 원래 누른 시각이 넘어온다.
            context.Router.Declare(new ActionTrigger(
                PlayerAction.Guard, this, () => true, prepare: e => PreparePress(e.Time)));
            // 누르고 있기만 해도 받을 수 있게 된 순간 가드한다. 윈도우는 열지 않는다. 패링에 쓴 누름이면 떼고 다시 눌러야 한다.
            context.Router.Declare(new ActionTrigger(
                PlayerAction.Guard, this, () => !_isHoldSpentOnParry, phase: ActionPhase.Held));
        }

        public bool IsCancelWindowOpen => false;

        // 가드를 올린 순간부터 막고, 패링 윈도우 동안은 튕겨 낸다.
        public DefenseStance DefenseStance => _parryWindow.IsOpen(_clock.Now)
            ? DefenseStance.Parrying
            : DefenseStance.Guarding;

        /// <summary>막기 반응에서 돌아와 다음 진입에서 가드를 다시 올리지 않고 가드 자세로 이어 간다. 전이 직전에 호출한다.</summary>
        public void PrepareReturnFromBlock()
        {
            _isReturningFromBlock = true;
        }

        /// <summary>열려 있던 패링 윈도우로 패링에 성공했다. 패링 상태로 전이하기 직전에 호출해, 헛친 것으로 세지 않게 한다.</summary>
        public void ConfirmParry()
        {
            _parryWindow.Succeed();
            _isHoldSpentOnParry = true;
        }

        public void OnEnable()
        {
            _input.ActionTriggered += OnActionTriggered;
        }

        public void OnDisable()
        {
            _input.ActionTriggered -= OnActionTriggered;
            // 꺼진 동안의 뗌은 들을 수 없으므로 누름의 연속은 여기서 끊긴 것으로 본다.
            _isHoldSpentOnParry = false;
        }

        private void OnActionTriggered(PlayerActionEvent e)
        {
            if (e.Action == PlayerAction.Guard && e.Phase == ActionPhase.Released)
            {
                _isHoldSpentOnParry = false;
            }
        }

        public void Enter()
        {
            // 생성 시점에는 다른 상태가 아직 등록되지 않았을 수 있어 첫 진입에서 찾는다.
            _locomotion ??= _locator.Get<LocomotionState>();

            if (_hasPendingPress)
            {
                // 쿨다운 중에 누른 것이면 일반 가드로만 들어온다.
                _parryWindow.TryOpen(_pendingPressTime);
            }

            if (_isReturningFromBlock)
            {
                _animator.FinishBlockReturn(_settings.BlockCrossFadeOutDuration);
            }
            else
            {
                _animator.RaiseGuard(_settings.CrossFadeDuration);
            }

            _hasPendingPress = false;
            _isReturningFromBlock = false;
        }

        public void Tick(float deltaTime)
        {
            // 패링 없이 지나간 윈도우는 헛친 것으로 닫아 쿨다운을 건다.
            double now = _clock.Now;
            if (!_parryWindow.IsOpen(now))
            {
                _parryWindow.Close(now);
            }

            if (!_input.IsHeld(PlayerAction.Guard))
            {
                _machine.ChangeState(_locomotion);
                return;
            }

            float referenceYaw = _context.MovementYaw;
            Vector2 move = _input.Move;
            Vector3 target = Quaternion.Euler(0f, referenceYaw, 0f)
                * new Vector3(move.x, 0f, move.y) * _settings.WalkSpeed;
            Vector3 current = _motor.HorizontalVelocity;
            float rate = target.sqrMagnitude > current.sqrMagnitude
                ? _locomotionSettings.Acceleration
                : _locomotionSettings.Deceleration;

            _motor.Move(Vector3.MoveTowards(current, target, rate * deltaTime), deltaTime);
            _motor.RotateTowards(referenceYaw, _locomotionSettings.TurnSpeed * deltaTime);
        }

        public void Exit()
        {
            // 윈도우가 끝나기 전에 가드를 풀거나 맞아서 나가도 헛친 것이다. 그러지 않으면 짧게 눌렀다 떼는 연타가 쿨다운을 피한다.
            _parryWindow.Close(_clock.Now);
            // 해제든 캔슬이든 상체가 가드 자세에 남지 않게 내린다.
            _animator.LowerGuard(_settings.CrossFadeDuration);
        }

        /// <summary>
        /// 새로 누른 가드의 누른 시각을 다음 진입에 넘긴다. 입력 시각은 실시간이므로, 누른 뒤 지난 실시간을 이번 프레임의 진행 비율
        /// (전역 배속 × 역경직)로 캐릭터 시계의 시간으로 바꿔 지금 시각에서 거스른다. 그래야 윈도우가 이후 시간 배율을 따르고,
        /// 역경직으로 느려진 동안 누른 가드는 그만큼 짧게 거슬러, 캐릭터 시계로 누른 순간에 맞춰진다.
        /// 선입력으로 여러 프레임 미뤄진 누름은 이번 프레임의 비율로 어림한다.
        /// </summary>
        private void PreparePress(double pressRealtime)
        {
            double delay = Math.Max(0d, Time.realtimeSinceStartupAsDouble - pressRealtime);
            float realDeltaTime = Time.unscaledDeltaTime;
            double rate = realDeltaTime > 0f ? _clock.DeltaTime / realDeltaTime : 0d;
            _pendingPressTime = _clock.Now - delay * rate;
            _hasPendingPress = true;
        }

        public bool TryChain(PlayerActionEvent e)
        {
            // 가드 중에 오는 Guard 입력은 이미 처리 중이다. 라우터로 넘기면 매 프레임 가드에 다시 진입한다.
            return e.Action == PlayerAction.Guard
                && (e.Phase == ActionPhase.Pressed || e.Phase == ActionPhase.Held);
        }
    }
}
