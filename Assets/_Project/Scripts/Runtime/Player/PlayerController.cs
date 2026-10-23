using System;
using System.Collections.Generic;
using GodOfWar.CameraSystem;
using GodOfWar.Characters;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using GodOfWar.Player.States;
using UnityEngine;

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

        // 이동 중 검을 든 팔·손 자세를 덮어쓰는 레이어. 전신 클립(FullBody 태그) 재생 중에는 끈다.
        private static readonly string[] SwordPoseLayerNames = { "SwordArm", "SwordHand" };

        [SerializeField] private LocomotionSettings _settings = new LocomotionSettings();
        [SerializeField] private DodgeSettings _dodgeSettings = new DodgeSettings();
        [SerializeField] private AttackSettings _attackSettings = new AttackSettings();

        [Tooltip("이동 입력의 기준 방향을 제공하는 카메라 피벗. 이 피벗의 Yaw를 기준으로 이동한다.")]
        [SerializeField] private CameraLook _cameraLook;

        [SerializeField] private Animator _animator;

        [Tooltip("검 든 자세 레이어가 꺼지고 켜지는 데 걸리는 시간(초).")]
        [SerializeField, Min(0.01f)] private float _swordPoseBlendDuration = 0.1f;

        [Header("Debug")]
        [Tooltip("켜면 시작할 때 액션 라우터가 수집한 라우팅 테이블을 콘솔에 출력한다. 에디터와 개발 빌드에서만 동작한다.")]
        [SerializeField] private bool _logActionRoutes;

        private readonly Dictionary<Type, IState> _states = new Dictionary<Type, IState>();
        private StateMachine _machine;
        private ActionRouter _router;
        private CharacterMotor _motor;
        private int[] _swordPoseLayers;
        private float[] _swordPoseWeights;

        public LocomotionSettings Settings => _settings;
        public DodgeSettings DodgeSettings => _dodgeSettings;
        public AttackSettings AttackSettings => _attackSettings;
        /// <summary>상태가 자기 클립으로 크로스페이드할 때 쓰는 애니메이터.</summary>
        public Animator Animator => _animator;
        /// <summary>이동 입력의 기준 Yaw(도).</summary>
        public float MovementYaw => _cameraLook.Yaw;
        /// <summary>상태가 생성될 때 자신의 진입 트리거를 선언하는 액션 라우터.</summary>
        public ActionRouter Router => _router;

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
            _router = new ActionRouter(_machine, GetRequiredComponent<IPlayerInput>());
            Register(new LocomotionState(this, _machine));
            Register(new SprintState(this, _machine));
            Register(new QuickStepState(this, _machine));
            Register(new DodgeRollState(this, _machine));
            Register(new AttackState(this, _machine));
        }

        private void Start()
        {
            // 다른 컴포넌트의 Awake가 모두 끝난 뒤 첫 상태에 진입한다.
            // 애니메이터 레이어도 자식 Animator가 초기화된 뒤에 읽는다.
            FindSwordPoseLayers();
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

            Vector3 localVelocity = transform.InverseTransformDirection(_motor.HorizontalVelocity);
            float dampTime = _settings.AnimatorDampTime;
            _animator.SetFloat(MoveXHash, localVelocity.x, dampTime, Time.deltaTime);
            _animator.SetFloat(MoveZHash, localVelocity.z, dampTime, Time.deltaTime);

            UpdateSwordPoseLayers(Time.deltaTime);
        }

        /// <summary>검 든 자세 레이어를 찾고, 애니메이터에 설정된 기본 가중치를 켜졌을 때의 가중치로 기억한다.</summary>
        private void FindSwordPoseLayers()
        {
            _swordPoseLayers = new int[SwordPoseLayerNames.Length];
            _swordPoseWeights = new float[SwordPoseLayerNames.Length];
            for (int i = 0; i < SwordPoseLayerNames.Length; i++)
            {
                int layer = _animator.GetLayerIndex(SwordPoseLayerNames[i]);
                if (layer < 0)
                {
                    throw new InvalidOperationException(
                        $"'{_animator.name}' has no '{SwordPoseLayerNames[i]}' animator layer.");
                }

                _swordPoseLayers[i] = layer;
                _swordPoseWeights[i] = _animator.GetLayerWeight(layer);
            }
        }

        private void UpdateSwordPoseLayers(float deltaTime)
        {
            // 크로스페이드 중에는 들어가는 상태를 기준으로 미리 끄거나 켠다.
            AnimatorStateInfo state = _animator.IsInTransition(0)
                ? _animator.GetNextAnimatorStateInfo(0)
                : _animator.GetCurrentAnimatorStateInfo(0);
            bool isFullBody = state.tagHash == FullBodyTagHash;

            for (int i = 0; i < _swordPoseLayers.Length; i++)
            {
                int layer = _swordPoseLayers[i];
                float target = isFullBody ? 0f : _swordPoseWeights[i];
                float step = _swordPoseWeights[i] / _swordPoseBlendDuration * deltaTime;
                _animator.SetLayerWeight(layer, Mathf.MoveTowards(_animator.GetLayerWeight(layer), target, step));
            }
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
