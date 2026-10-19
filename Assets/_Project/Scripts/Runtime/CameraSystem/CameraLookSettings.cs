using System;
using UnityEngine;

namespace GodOfWar.CameraSystem
{
    /// <summary>
    /// <see cref="CameraLook"/>의 시점 감도와 Pitch 제한.
    /// Pitch는 Unity Euler X와 같은 부호를 쓴다 (양수 = 내려다봄).
    /// </summary>
    [Serializable]
    public sealed class CameraLookSettings
    {
        [Header("Sensitivity")]
        [Tooltip("Delta 입력(마우스) 1당 회전 각도(도). x = Yaw, y = Pitch.")]
        [SerializeField] private Vector2 _mouseSensitivity = new Vector2(0.1f, 0.1f);
        [Tooltip("Rate 입력(스틱) 크기 1일 때의 회전 속도(deg/s). x = Yaw, y = Pitch.")]
        [SerializeField] private Vector2 _rateSpeed = new Vector2(200f, 140f);
        [SerializeField] private bool _invertY;

        [Header("Pitch (deg)")]
        [Tooltip("올려다볼 수 있는 한계. 음수다.")]
        [SerializeField, Range(-89f, 0f)] private float _minPitch = -40f;
        [Tooltip("내려다볼 수 있는 한계.")]
        [SerializeField, Range(0f, 89f)] private float _maxPitch = 70f;

        public CameraLookSettings()
        {
        }

        public CameraLookSettings(
            Vector2 mouseSensitivity, Vector2 rateSpeed, bool invertY, float minPitch, float maxPitch)
        {
            _mouseSensitivity = mouseSensitivity;
            _rateSpeed = rateSpeed;
            _invertY = invertY;
            _minPitch = minPitch;
            _maxPitch = maxPitch;
        }

        public Vector2 MouseSensitivity => _mouseSensitivity;
        public Vector2 RateSpeed => _rateSpeed;
        public bool InvertY => _invertY;
        public float MinPitch => _minPitch;
        public float MaxPitch => _maxPitch;
    }
}
