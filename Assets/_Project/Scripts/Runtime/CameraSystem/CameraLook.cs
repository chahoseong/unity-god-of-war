using System;
using GodOfWar.Input;
using UnityEngine;

namespace GodOfWar.CameraSystem
{
    /// <summary>
    /// 시점 입력으로 카메라 피벗의 Yaw·Pitch를 회전한다. Cinemachine 카메라는 이 피벗의 회전을 따라간다.
    /// 피벗은 캐릭터의 자식이지만 캐릭터 회전을 상속하지 않도록 매 프레임 월드 회전을 덮어쓴다.
    /// </summary>
    /// <remarks>
    /// PlayerController(Update)보다 먼저 실행되어 이동이 같은 프레임의 Yaw를 읽게 한다.
    /// LateUpdate는 CinemachineBrain(실행 순서 100)보다 먼저 실행된다.
    /// </remarks>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class CameraLook : MonoBehaviour
    {
        [SerializeField] private CameraLookSettings _settings = new CameraLookSettings();

        private IPlayerInput _input;

        /// <summary>수평 회전(도, 0~360). 이동의 기준 방향이다.</summary>
        public float Yaw { get; private set; }

        /// <summary>수직 회전(도). 양수 = 내려다봄.</summary>
        public float Pitch { get; private set; }

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
            Vector2 yawPitch = ApplyLook(
                new Vector2(Yaw, Pitch), _input.Look, _input.LookType, _settings, Time.deltaTime);
            Yaw = yawPitch.x;
            Pitch = yawPitch.y;
            ApplyRotation();
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
