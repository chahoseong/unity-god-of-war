using System;
using System.Collections.Generic;
using GodOfWar.CameraSystem;
using GodOfWar.Characters;
using GodOfWar.Core.StateMachines;
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

        [SerializeField] private LocomotionSettings _settings = new LocomotionSettings();
        [SerializeField] private DodgeSettings _dodgeSettings = new DodgeSettings();

        [Tooltip("이동 입력의 기준 방향을 제공하는 카메라 피벗. 이 피벗의 Yaw를 기준으로 이동한다.")]
        [SerializeField] private CameraLook _cameraLook;

        [SerializeField] private Animator _animator;

        private readonly Dictionary<Type, IState> _states = new Dictionary<Type, IState>();
        private StateMachine _machine;
        private CharacterMotor _motor;

        public LocomotionSettings Settings => _settings;
        public DodgeSettings DodgeSettings => _dodgeSettings;
        /// <summary>상태가 자기 클립으로 크로스페이드할 때 쓰는 애니메이터.</summary>
        public Animator Animator => _animator;
        /// <summary>이동 입력의 기준 Yaw(도).</summary>
        public float MovementYaw => _cameraLook.Yaw;

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

            _motor = GetComponent<CharacterMotor>();
            _machine = new StateMachine();
            Register(new LocomotionState(this, _machine));
            Register(new SprintState(this, _machine));
            Register(new QuickStepState(this, _machine));
            Register(new DodgeRollState(this, _machine));
        }

        private void Start()
        {
            // 다른 컴포넌트의 Awake가 모두 끝난 뒤 첫 상태에 진입한다.
            _machine.ChangeState(Get<LocomotionState>());
        }

        private void Update()
        {
            _machine.Tick(Time.deltaTime);

            Vector3 localVelocity = transform.InverseTransformDirection(_motor.HorizontalVelocity);
            float dampTime = _settings.AnimatorDampTime;
            _animator.SetFloat(MoveXHash, localVelocity.x, dampTime, Time.deltaTime);
            _animator.SetFloat(MoveZHash, localVelocity.z, dampTime, Time.deltaTime);
        }

        private void Register(IState state)
        {
            _states.Add(state.GetType(), state);
        }
    }
}
