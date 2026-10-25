using System;
using System.Collections.Generic;
using GodOfWar.CameraSystem;
using GodOfWar.Characters;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using GodOfWar.Player.States;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace GodOfWar.Player
{
    /// <summary>
    /// 플레이어 상태 머신의 컴포지션 루트.
    /// 상태를 생성·등록하고 매 프레임 갱신하며, 상태들이 질의하는 Owner 역할을 한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class PlayerController : MonoBehaviour, IStateLocator
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

        [SerializeField] private LocomotionSettings _settings = new LocomotionSettings();
        [SerializeField] private DodgeSettings _dodgeSettings = new DodgeSettings();
        [SerializeField] private AttackSettings _attackSettings = new AttackSettings();
        [SerializeField] private GuardSettings _guardSettings = new GuardSettings();

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

        private readonly Dictionary<Type, IState> _states = new Dictionary<Type, IState>();
        private StateMachine _machine;
        private ActionRouter _router;
        private CharacterMotor _motor;
        private int[] _swordPoseLayers;
        private float[] _swordPoseWeights;
        private int _guardLayer = -1;

        public LocomotionSettings Settings => _settings;
        public DodgeSettings DodgeSettings => _dodgeSettings;
        public AttackSettings AttackSettings => _attackSettings;
        public GuardSettings GuardSettings => _guardSettings;
        /// <summary>상태가 자기 클립으로 크로스페이드할 때 쓰는 애니메이터.</summary>
        public Animator Animator => _animator;
        /// <summary>이동 입력의 기준 Yaw(도). 락온 중에는 카메라 보간과 무관하게 대상을 향한다.</summary>
        public float MovementYaw => _cameraLook.ReferenceYaw;
        /// <summary>상태가 생성될 때 자신의 진입 트리거를 선언하는 액션 라우터.</summary>
        public ActionRouter Router => _router;
        /// <summary>현재 상태. 첫 상태에 진입하기 전에는 null이다.</summary>
        public IState CurrentState => _machine?.CurrentState;
        /// <summary>상체 가드 레이어의 인덱스. 가중치는 이 컴포넌트가 재생 중인 상태에 맞춰 관리한다.</summary>
        public int GuardLayer => _guardLayer;

        public T Get<T>() where T : class, IState
        {
            if (_states.TryGetValue(typeof(T), out IState state))
            {
                return (T)state;
            }

            throw new InvalidOperationException($"State '{typeof(T).Name}' is not registered.");
        }

        /// <summary>상태가 생성 시점에 필요한 컴포넌트를 질의한다. 없으면 즉시 실패한다.</summary>
        public T GetRequiredComponent<T>()
        {
            // 인터페이스 타입은 Unity의 가짜 null을 == null로 거를 수 없어 TryGetComponent를 쓴다.
            if (TryGetComponent(out T component))
            {
                return component;
            }

            throw new InvalidOperationException(
                $"'{name}' requires a {typeof(T).Name} component.");
        }

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

            if (_dodgeSettings.QuickStep == null || _dodgeSettings.DodgeRoll == null)
            {
                throw new InvalidOperationException(
                    $"'{name}' has no dodge actions assigned. Assign QuickStep and DodgeRoll in Dodge Settings.");
            }

            if (_attackSettings.GetEntry(PlayerAction.LightAttack) == null
                || _attackSettings.GetEntry(PlayerAction.HeavyAttack) == null)
            {
                throw new InvalidOperationException(
                    $"'{name}' has no entry attacks assigned. Assign both entries in Attack Settings.");
            }

            _motor = GetComponent<CharacterMotor>();
            _machine = new StateMachine();
            // 상태가 생성자에서 진입 트리거를 선언하므로 상태보다 먼저 만든다.
            _router = new ActionRouter(
                _machine, GetRequiredComponent<IPlayerInput>(), _inputBufferDuration, NonRoutedActions);
            Register(new LocomotionState(this, _machine));
            Register(new SprintState(this, _machine));
            Register(new QuickStepState(this, _machine));
            Register(new DodgeRollState(this, _machine));
            Register(new AttackState(this, _machine));
            Register(new GuardState(this, _machine));
        }

        private void Start()
        {
            // 다른 컴포넌트의 Awake가 모두 끝난 뒤 첫 상태에 진입한다.
            // 애니메이터 레이어도 자식 Animator가 초기화된 뒤에 읽는다.
            FindSwordPoseLayers();
            _guardLayer = FindLayer(GuardLayerName);
            _machine.ChangeState(Get<LocomotionState>());
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
            float dampTime = _settings.AnimatorDampTime;
            _animator.SetFloat(MoveXHash, localVelocity.x, dampTime, Time.deltaTime);
            _animator.SetFloat(MoveZHash, localVelocity.z, dampTime, Time.deltaTime);

            bool isFullBody = IsFullBodyPlaying();
            UpdateSwordPoseLayers(isFullBody, Time.deltaTime);
            UpdateGuardLayer(isFullBody, Time.deltaTime);
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
            _states.Add(state.GetType(), state);
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
