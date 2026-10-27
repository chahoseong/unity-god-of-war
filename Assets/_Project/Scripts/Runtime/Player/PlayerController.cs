using System;
using GodOfWar.CameraSystem;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using GodOfWar.Player.States;
using UnityEngine;

namespace GodOfWar.Player
{
    /// <summary>
    /// 플레이어 상태 머신의 컴포지션 루트.
    /// 상태를 생성·등록하고 매 프레임 갱신하며, 상태들이 속한 Context다.
    /// 상태에게는 <see cref="IPlayerStateContext"/>로만 구성 요소와 전이 수단을 열고, 공개 API는 관찰용만 둔다.
    /// 바깥에서는 상태를 모른 채 타격을 전하므로, 타격을 받아 현재 상태의 방어 자세로 판정하고 결과에 맞게 전이하는 것도 이 Context의 일이다.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.PlayerController)]
    [RequireComponent(typeof(CharacterMotor), typeof(PlayerAnimator))]
    public sealed class PlayerController : MonoBehaviour, IPlayerStateContext, IHittable
    {
        // 상태 전이와 무관해 라우터가 다루지 않는 버튼. LockOn은 TargetLock이 직접 구독한다.
        private static readonly PlayerAction[] NonRoutedActions = { PlayerAction.LockOn };

        [SerializeField] private LocomotionSettings _locomotionSettings;
        [SerializeField] private DodgeSettings _dodgeSettings;
        [SerializeField] private AttackSettings _attackSettings;
        [SerializeField] private GuardSettings _guardSettings;

        [Tooltip("타격 세기·방향별로 재생할 피격 반응.")]
        [SerializeField] private HitReactionSet _hitReactions;

        [Tooltip("이동 입력의 기준 방향을 제공하는 카메라 피벗. 이 피벗의 Yaw를 기준으로 이동한다.")]
        [SerializeField] private CameraLook _cameraLook;

        [Tooltip("캔슬할 수 없는 동안 누른 버튼을 기억했다가 받을 수 있게 되면 실행하는 시간(초). 0이면 선입력을 쓰지 않는다. "
                 + "시작할 때 읽으므로 플레이 중 바꾼 값은 다음 실행부터 적용된다.")]
        [SerializeField, Min(0f)] private float _inputBufferDuration = 0.2f;

        [Header("Debug")]
        [Tooltip("켜면 시작할 때 액션 라우터가 수집한 라우팅 테이블을 콘솔에 출력한다. 에디터와 개발 빌드에서만 동작한다.")]
        [SerializeField] private bool _logActionRoutes;

        private readonly StateRegistry _registry = new StateRegistry();
        private StateMachine _machine;
        private ActionRouter _router;
        private CharacterMotor _motor;
        private PlayerAnimator _animator;
        private IPlayerInput _input;
        private Hitbox _hitbox;
        private GuardState _guard;
        private HitReactState _hitReact;
        private GuardBreakState _guardBreak;
        private ParryState _parry;

        /// <summary>현재 상태. 첫 상태에 진입하기 전에는 null이다.</summary>
        public IState CurrentState => _machine?.CurrentState;

        /// <summary>보관 중인 선입력이 있는지.</summary>
        public bool HasBufferedInput => _router.HasBufferedInput;

        /// <summary>보관 중인 선입력의 버튼. <see cref="HasBufferedInput"/>이 거짓이면 의미가 없다.</summary>
        public PlayerAction BufferedAction => _router.BufferedAction;

        CharacterMotor IPlayerStateContext.Motor => _motor;
        IPlayerInput IPlayerStateContext.Input => _input;
        PlayerAnimator IPlayerStateContext.Animator => _animator;
        Hitbox IPlayerStateContext.Hitbox => _hitbox;
        float IPlayerStateContext.MovementYaw => _cameraLook.ReferenceYaw;
        StateMachine IPlayerStateContext.Machine => _machine;
        IStateLocator IPlayerStateContext.States => _registry;
        ActionRouter IPlayerStateContext.Router => _router;

        private void Awake()
        {
            if (_cameraLook == null)
            {
                throw new InvalidOperationException(
                    $"'{name}' has no {nameof(CameraLook)} assigned. Assign the camera pivot.");
            }

            // 설정 내용은 그 값을 쓰는 상태가 생성 시점에 검사한다.
            RequireAssigned(_locomotionSettings);
            RequireAssigned(_dodgeSettings);
            RequireAssigned(_attackSettings);
            RequireAssigned(_guardSettings);
            RequireAssigned(_hitReactions);

            // 상태가 생성자에서 창구의 값을 읽으므로 상태보다 먼저 준비한다.
            _motor = GetComponent<CharacterMotor>();
            // RequireComponent는 이미 만들어진 프리팹에 컴포넌트를 채워 주지 않으므로 직접 확인한다.
            if (!TryGetComponent(out _animator))
            {
                throw new InvalidOperationException($"'{name}' requires a {nameof(PlayerAnimator)} component.");
            }

            _input = FindRequiredInput();
            _hitbox = FindRequiredHitbox();
            _machine = new StateMachine();
            // 상태가 생성자에서 진입 트리거를 선언하므로 상태보다 먼저 만든다.
            _router = new ActionRouter(_machine, _input, _inputBufferDuration, NonRoutedActions);
            Register(new LocomotionState(this, _locomotionSettings));
            Register(new SprintState(this, _locomotionSettings));
            Register(new QuickStepState(this, _dodgeSettings));
            Register(new DodgeRollState(this, _dodgeSettings));
            Register(new AttackState(this, _attackSettings));
            _guard = Register(new GuardState(this, _guardSettings, _locomotionSettings));
            _hitReact = Register(new HitReactState(this, _hitReactions));
            _guardBreak = Register(new GuardBreakState(this, _guardSettings));
            _parry = Register(new ParryState(this, _guardSettings));
        }

        private void Start()
        {
            // 다른 컴포넌트의 Awake가 모두 끝난 뒤 첫 상태에 진입한다.
            _machine.ChangeState(_registry.Get<LocomotionState>());
            LogActionRoutes();
        }

        private void OnDestroy()
        {
            // Awake 검증에서 실패하면 라우터가 만들어지지 않는다.
            _router?.Dispose();
        }

        private void Update()
        {
            _machine.Tick(Time.deltaTime);
            // 이번 프레임에 열린 캔슬 윈도우나 끝난 행동에 선입력이 같은 프레임에 나가도록 상태 갱신 뒤에 판정한다.
            _router.Tick(Time.deltaTime);
        }

        public HitResult ReceiveHit(in AttackInfo attack)
        {
            IState current = _machine.CurrentState;
            DefenseStance stance = current is IDefensible defensible
                ? defensible.DefenseStance
                : throw new InvalidOperationException(
                    $"'{current?.GetType().Name}' does not implement {nameof(IDefensible)}. Register it through {nameof(PlayerController)}.");

            HitOutcome outcome = HitOutcomeResolver.Resolve(
                stance, attack, transform.position, transform.forward, _guardSettings.GuardHalfAngle);
            switch (outcome)
            {
                case HitOutcome.Hit:
                    _hitReact.Prepare(attack);
                    ForceTransition(_hitReact);
                    break;
                case HitOutcome.Evaded:
                    // 회피가 그대로 이어지므로 상태도 선입력도 건드리지 않는다.
                    break;
                case HitOutcome.Blocked:
                    // 가드 중이면 상태를 유지한 채 막는 반응만 하므로 선입력을 건드리지 않는다.
                    // 패링 동작 중이면 가드 자세로 돌아가 막는다. 상태가 바뀌므로 강제 전이로 선입력을 버린다.
                    if (current != _guard)
                    {
                        _guard.PrepareReturnFromParry();
                        ForceTransition(_guard);
                    }

                    _guard.Block(attack);
                    break;
                case HitOutcome.GuardBroken:
                    ForceTransition(_guardBreak);
                    break;
                case HitOutcome.Parried:
                    // 패링 상태로 나가며 윈도우를 헛친 것으로 닫지 않도록 성공을 먼저 알린다.
                    _guard.ConfirmParry();
                    ForceTransition(_parry);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null);
            }

            return new HitResult(outcome);
        }

        /// <summary>
        /// 피격처럼 입력과 무관한 원인으로 상태를 바꾸는 유일한 경로.
        /// 강제 전이 전에 누른 버튼이 전이 뒤에 뒤늦게 나가지 않도록 선입력을 먼저 버린다 (ADR-004 5절).
        /// </summary>
        private void ForceTransition(IState next)
        {
            _router.Flush();
            _machine.ChangeState(next);
        }

        private IPlayerInput FindRequiredInput()
        {
            // 인터페이스 타입은 Unity의 가짜 null을 == null로 거를 수 없어 TryGetComponent를 쓴다.
            if (TryGetComponent(out IPlayerInput input))
            {
                return input;
            }

            throw new InvalidOperationException($"'{name}' requires a {nameof(IPlayerInput)} component.");
        }

        // 칼은 손 본 아래에 있어 루트가 아니라 자식에서 찾는다.
        private Hitbox FindRequiredHitbox()
        {
            Hitbox[] hitboxes = GetComponentsInChildren<Hitbox>(true);
            if (hitboxes.Length != 1)
            {
                throw new InvalidOperationException(
                    $"'{name}' requires exactly one {nameof(Hitbox)} in its children, but found {hitboxes.Length}.");
            }

            return hitboxes[0];
        }

        private void RequireAssigned<T>(T settings) where T : ScriptableObject
        {
            if (settings == null)
            {
                throw new InvalidOperationException($"'{name}' has no {typeof(T).Name} assigned.");
            }
        }

        // 캔슬 윈도우(ADR-004 3절)나 방어 자세 선언을 빠뜨린 상태는 여기서 컴파일 에러가 난다.
        private T Register<T>(T state) where T : class, IState, ICancellable, IDefensible
        {
            _registry.Add(state);
            return state;
        }

        private void LogActionRoutes()
        {
            // 에디터와 개발 빌드에서만 출력한다. 에디터에서는 항상 isDebugBuild가 참이다.
            if (_logActionRoutes && Debug.isDebugBuild)
            {
                Debug.Log(_router.Describe(), this);
            }
        }
    }
}
