using System;
using GodOfWar.CameraSystem;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using GodOfWar.Player.States;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace GodOfWar.Player
{
    /// <summary>
    /// 플레이어 상태 머신의 컴포지션 루트.
    /// 상태를 생성·등록하고 매 프레임 갱신하며, 상태들이 속한 Context다.
    /// 상태에게는 <see cref="IPlayerStateContext"/>로만 구성 요소와 전이 수단을 열고, 공개 API는 관찰용만 둔다.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.PlayerController)]
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class PlayerController : MonoBehaviour, IPlayerStateContext
    {
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
        private static readonly int FullBodyTagHash = Animator.StringToHash("FullBody");

        private static readonly int GuardBeginTagHash = Animator.StringToHash("GuardBegin");
        private static readonly int GuardTagHash = Animator.StringToHash("Guard");
        private static readonly int GuardEndTagHash = Animator.StringToHash("GuardEnd");

        // 이동 중 검을 든 팔·손 자세를 덮어쓰는 레이어. 전신 클립(FullBody 태그) 재생 중에는 끈다.
        private static readonly string[] SwordPoseLayerNames = { "SwordArm", "SwordHand" };

        // 이동하는 다리 위에 가드 자세를 덮어쓰는 상체 레이어. 전신 클립 재생 중에는 끈다.
        private const string GuardLayerName = "Guard";

        // 상태 전이와 무관해 라우터가 다루지 않는 버튼. LockOn은 TargetLock이 직접 구독한다.
        private static readonly PlayerAction[] NonRoutedActions = { PlayerAction.LockOn };

        [SerializeField] private LocomotionSettings _locomotionSettings;
        [SerializeField] private DodgeSettings _dodgeSettings;
        [SerializeField] private AttackSettings _attackSettings;
        [SerializeField] private GuardSettings _guardSettings;

        [Tooltip("이동 입력의 기준 방향을 제공하는 카메라 피벗. 이 피벗의 Yaw를 기준으로 이동한다.")]
        [SerializeField] private CameraLook _cameraLook;

        [SerializeField] private Animator _animator;

        [Tooltip("가드 중 걷기 클립의 골반 흔들림이 상체로 번지지 않게 척추를 고정하는 리그. 가중치는 Guard 레이어에 맞춰 관리한다.")]
        [SerializeField] private Rig _guardRig;

        [Tooltip("검 든 자세 레이어가 꺼지고 켜지는 데 걸리는 시간(초).")]
        [SerializeField, Min(0.01f)] private float _swordPoseBlendDuration = 0.1f;

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
        private IPlayerInput _input;
        private Hitbox _hitbox;
        private int[] _swordPoseLayers;
        private float[] _swordPoseWeights;
        private int _guardLayer = -1;

        /// <summary>현재 상태. 첫 상태에 진입하기 전에는 null이다.</summary>
        public IState CurrentState => _machine?.CurrentState;

        /// <summary>보관 중인 선입력이 있는지.</summary>
        public bool HasBufferedInput => _router.HasBufferedInput;

        /// <summary>보관 중인 선입력의 버튼. <see cref="HasBufferedInput"/>이 거짓이면 의미가 없다.</summary>
        public PlayerAction BufferedAction => _router.BufferedAction;

        CharacterMotor IPlayerStateContext.Motor => _motor;
        IPlayerInput IPlayerStateContext.Input => _input;
        Animator IPlayerStateContext.Animator => _animator;
        Hitbox IPlayerStateContext.Hitbox => _hitbox;
        float IPlayerStateContext.MovementYaw => _cameraLook.ReferenceYaw;
        StateMachine IPlayerStateContext.Machine => _machine;
        IStateLocator IPlayerStateContext.States => _registry;
        ActionRouter IPlayerStateContext.Router => _router;
        int IPlayerStateContext.GuardLayer => _guardLayer;

        private void Awake()
        {
            if (_cameraLook == null)
            {
                throw new InvalidOperationException(
                    $"'{name}' has no {nameof(CameraLook)} assigned. Assign the camera pivot.");
            }

            if (_animator == null)
            {
                throw new InvalidOperationException($"'{name}' has no Animator assigned.");
            }

            if (_guardRig == null)
            {
                throw new InvalidOperationException($"'{name}' has no guard Rig assigned.");
            }

            // 설정 내용은 그 값을 쓰는 상태가 생성 시점에 검사한다.
            RequireAssigned(_locomotionSettings);
            RequireAssigned(_dodgeSettings);
            RequireAssigned(_attackSettings);
            RequireAssigned(_guardSettings);

            // 상태가 생성자에서 창구의 값을 읽으므로 상태보다 먼저 준비한다.
            _motor = GetComponent<CharacterMotor>();
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
            Register(new GuardState(this, _guardSettings, _locomotionSettings));
        }

        private void Start()
        {
            // 다른 컴포넌트의 Awake가 모두 끝난 뒤 첫 상태에 진입한다.
            // 애니메이터 레이어도 자식 Animator가 초기화된 뒤에 읽는다.
            FindSwordPoseLayers();
            _guardLayer = FindLayer(GuardLayerName);
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

            Vector3 localVelocity = transform.InverseTransformDirection(_motor.HorizontalVelocity);
            float dampTime = _locomotionSettings.AnimatorDampTime;
            _animator.SetFloat(MoveXHash, localVelocity.x, dampTime, Time.deltaTime);
            _animator.SetFloat(MoveZHash, localVelocity.z, dampTime, Time.deltaTime);

            bool isFullBody = IsFullBodyPlaying();
            UpdateSwordPoseLayers(isFullBody, Time.deltaTime);
            UpdateGuardLayer(isFullBody, Time.deltaTime);
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

        /// <summary>검 든 자세 레이어를 찾고, 애니메이터에 설정된 기본 가중치를 켜졌을 때의 가중치로 기억한다.</summary>
        private void FindSwordPoseLayers()
        {
            _swordPoseLayers = new int[SwordPoseLayerNames.Length];
            _swordPoseWeights = new float[SwordPoseLayerNames.Length];
            for (int i = 0; i < SwordPoseLayerNames.Length; i++)
            {
                int layer = FindLayer(SwordPoseLayerNames[i]);
                _swordPoseLayers[i] = layer;
                _swordPoseWeights[i] = _animator.GetLayerWeight(layer);
            }
        }

        private int FindLayer(string layerName)
        {
            int layer = _animator.GetLayerIndex(layerName);
            if (layer < 0)
            {
                throw new InvalidOperationException($"'{_animator.name}' has no '{layerName}' animator layer.");
            }

            return layer;
        }

        private bool IsFullBodyPlaying()
        {
            return GetIncomingStateInfo(0).tagHash == FullBodyTagHash;
        }

        // 크로스페이드 중에는 들어가는 상태를 기준으로 삼아 레이어를 미리 끄거나 켠다.
        private AnimatorStateInfo GetIncomingStateInfo(int layer)
        {
            return _animator.IsInTransition(layer)
                ? _animator.GetNextAnimatorStateInfo(layer)
                : _animator.GetCurrentAnimatorStateInfo(layer);
        }

        private void UpdateSwordPoseLayers(bool isFullBody, float deltaTime)
        {
            for (int i = 0; i < _swordPoseLayers.Length; i++)
            {
                int layer = _swordPoseLayers[i];
                float target = isFullBody ? 0f : _swordPoseWeights[i];
                float step = _swordPoseWeights[i] / _swordPoseBlendDuration * deltaTime;
                _animator.SetLayerWeight(layer, Mathf.MoveTowards(_animator.GetLayerWeight(layer), target, step));
            }
        }

        private void UpdateGuardLayer(bool isFullBody, float deltaTime)
        {
            // 가드를 내리는 클립은 끝까지 보여 주고, 끝난 뒤에는 마지막 포즈를 유지한 채 가중치만 내린다.
            // 모션 없는 상태로 넘기면 Write Defaults 때문에 상체가 기본 포즈 쪽으로 끌려간다.
            AnimatorStateInfo state = GetIncomingStateInfo(_guardLayer);
            float progress = Mathf.Clamp01(state.normalizedTime);
            bool isRaised = state.tagHash == GuardBeginTagHash
                || state.tagHash == GuardTagHash
                || (state.tagHash == GuardEndTagHash && state.normalizedTime < 1f);
            float target = !isFullBody && isRaised ? 1f : 0f;

            float step = deltaTime / _swordPoseBlendDuration;
            float layerWeight = Mathf.MoveTowards(_animator.GetLayerWeight(_guardLayer), target, step);
            _animator.SetLayerWeight(_guardLayer, layerWeight);

            // 척추 고정 기준은 Loop 자세다. 몸을 트는 Begin·End 동안은 진행도만큼만 고정해 클립의 회전을 살린다.
            float stabilize = state.tagHash == GuardBeginTagHash ? progress
                : state.tagHash == GuardTagHash ? 1f
                : state.tagHash == GuardEndTagHash ? 1f - progress
                : 0f;
            _guardRig.weight = stabilize * layerWeight;
        }

        // 캔슬 윈도우 선언을 빠뜨린 상태는 여기서 컴파일 에러가 난다 (ADR-004 3절).
        private void Register<T>(T state) where T : class, IState, ICancellable
        {
            _registry.Add(state);
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
