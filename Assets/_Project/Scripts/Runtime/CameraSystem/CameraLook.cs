using System;
using GodOfWar.Input;
using UnityEngine;

namespace GodOfWar.CameraSystem
{
    /// <summary>
    /// 시점 입력으로 카메라 피벗의 Yaw·Pitch를 회전한다. Cinemachine 카메라는 이 피벗의 회전을 따라간다.
    /// 피벗은 캐릭터의 자식이지만 캐릭터 회전을 상속하지 않도록 매 프레임 월드 회전을 덮어쓴다.
    /// 바라볼 대상이 지정되면 시점 입력을 무시하고 대상 쪽으로 보간해 회전한다.
    /// </summary>
    /// <remarks>
    /// PlayerController(Update)보다 먼저 실행되어 이동이 같은 프레임의 Yaw를 읽게 한다.
    /// LateUpdate는 CinemachineBrain(실행 순서 100)보다 먼저 실행된다.
    /// </remarks>
    [DefaultExecutionOrder(ExecutionOrder.CameraLook)]
    [DisallowMultipleComponent]
    public sealed class CameraLook : MonoBehaviour
    {
        [SerializeField] private CameraLookSettings _settings = new CameraLookSettings();
        [SerializeField] private LockOnCameraSettings _lockOnSettings = new LockOnCameraSettings();

        private IPlayerInput _input;
        private Transform _lookTarget;

        // 보간하기 전의 목표 (Yaw, Pitch)와 스프링의 각속도.
        private Vector2 _lockAngles;
        private Vector2 _lockVelocity;

        /// <summary>화면에 보이는 수평 회전(도, 0~360).</summary>
        public float Yaw { get; private set; }

        /// <summary>수직 회전(도). 양수 = 내려다봄.</summary>
        public float Pitch { get; private set; }

        /// <summary>
        /// 이동의 기준 Yaw(도). 대상을 바라보는 중에는 보간하기 전의 목표 Yaw, 아니면 <see cref="Yaw"/>와 같다.
        /// 카메라의 보간 지연이 이동 방향을 늦추면 대상 주위를 돌 때 바깥으로 밀려나므로 둘을 나눈다.
        /// </summary>
        public float ReferenceYaw => _lookTarget != null ? _lockAngles.x : Yaw;

        /// <summary>
        /// 피벗에서 조준점을 바라보는 (Yaw, Pitch)를 반환한다.
        /// Pitch는 <see cref="LockOnCameraSettings.PitchBias"/>에서 조준점의 고저각을 뺀 뒤 락온 범위로 제한한다.
        /// 수평 거리가 <see cref="LockOnCameraSettings.HoldDistance"/>보다 가까우면 방향이 불안정하므로 <paramref name="previous"/>를 그대로 반환한다.
        /// </summary>
        public static Vector2 ComputeLockAngles(
            Vector3 pivot, Vector3 aimPoint, Vector2 previous, LockOnCameraSettings settings)
        {
            Vector3 offset = aimPoint - pivot;
            float horizontal = new Vector2(offset.x, offset.z).magnitude;
            if (horizontal < settings.HoldDistance)
            {
                return previous;
            }

            float yaw = Mathf.Repeat(Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg, 360f);
            float elevation = Mathf.Atan2(offset.y, horizontal) * Mathf.Rad2Deg;
            float pitch = Mathf.Clamp(settings.PitchBias - elevation, settings.MinPitch, settings.MaxPitch);
            return new Vector2(yaw, pitch);
        }

        /// <summary>시점 입력 대신 <paramref name="target"/>을 바라보기 시작한다. 현재 각도에서 보간을 시작한다.</summary>
        public void SetLookTarget(Transform target)
        {
            _lookTarget = target != null ? target : throw new ArgumentNullException(nameof(target));
            _lockVelocity = Vector2.zero;
            // 같은 프레임에 이동이 기준 Yaw를 읽을 수 있도록 목표를 바로 계산한다.
            _lockAngles = ComputeLockAngles(transform.position, target.position, new Vector2(Yaw, Pitch), _lockOnSettings);
        }

        /// <summary>대상을 놓고, 마지막 각도에서 시점 입력을 다시 받는다.</summary>
        public void ClearLookTarget()
        {
            _lookTarget = null;
        }

        /// <summary>
        /// 시점 입력을 적용한 (Yaw, Pitch)를 반환한다.
        /// <see cref="LookInputType.Delta"/>는 이미 프레임 단위 값이라 <paramref name="deltaTime"/>을 곱하지 않는다.
        /// 위쪽 입력(<c>look.y &gt; 0</c>)은 올려다보는 방향이다.
        /// </summary>
        public static Vector2 ApplyLook(
            Vector2 yawPitch, Vector2 look, LookInputType type, CameraLookSettings settings, float deltaTime)
        {
            Vector2 delta = type == LookInputType.Delta
                ? Vector2.Scale(look, settings.MouseSensitivity)
                : Vector2.Scale(look, settings.RateSpeed) * deltaTime;

            float pitchDelta = settings.InvertY ? delta.y : -delta.y;

            float yaw = Mathf.Repeat(yawPitch.x + delta.x, 360f);
            float pitch = Mathf.Clamp(yawPitch.y + pitchDelta, settings.MinPitch, settings.MaxPitch);
            return new Vector2(yaw, pitch);
        }

        private void Awake()
        {
            _input = GetComponentInParent<IPlayerInput>();
            if (_input == null)
            {
                throw new InvalidOperationException(
                    $"'{name}' requires an {nameof(IPlayerInput)} on itself or a parent.");
            }

            Yaw = transform.eulerAngles.y;
            Pitch = 0f;
            ApplyRotation();
        }

        private void Update()
        {
            // 대상이 소멸하면 Unity 비교에서 null이 되어 시점 입력으로 돌아간다.
            if (_lookTarget != null)
            {
                FollowLookTarget(Time.deltaTime);
            }
            else
            {
                Vector2 yawPitch = ApplyLook(
                    new Vector2(Yaw, Pitch), _input.Look, _input.LookType, _settings, Time.deltaTime);
                Yaw = yawPitch.x;
                Pitch = yawPitch.y;
            }

            ApplyRotation();
        }

        private void FollowLookTarget(float deltaTime)
        {
            _lockAngles = ComputeLockAngles(transform.position, _lookTarget.position, _lockAngles, _lockOnSettings);

            float smoothTime = _lockOnSettings.SmoothTime;
            float maxSpeed = _lockOnSettings.MaxSpeed;
            float yaw = Mathf.SmoothDampAngle(Yaw, _lockAngles.x, ref _lockVelocity.x, smoothTime, maxSpeed, deltaTime);
            Yaw = Mathf.Repeat(yaw, 360f);
            Pitch = Mathf.SmoothDampAngle(Pitch, _lockAngles.y, ref _lockVelocity.y, smoothTime, maxSpeed, deltaTime);
        }

        private void LateUpdate()
        {
            // 부모(캐릭터)가 Update에서 회전하며 바뀐 월드 회전을 되돌린다.
            ApplyRotation();
        }

        private void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(Pitch, Yaw, 0f);
        }
    }
}
