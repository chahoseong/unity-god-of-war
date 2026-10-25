using System;
using UnityEngine;

namespace GodOfWar.CameraSystem
{
    /// <summary>
    /// 락온 중 <see cref="CameraLook"/>이 대상을 바라보는 구도와 보간.
    /// Pitch는 <see cref="CameraLookSettings"/>와 같은 부호를 쓴다 (양수 = 내려다봄).
    /// </summary>
    [Serializable]
    public sealed class LockOnCameraSettings
    {
        [Header("Framing (deg)")]
        [Tooltip("대상과 높이가 같을 때 내려다보는 각도. 대상이 화면 중앙보다 이만큼 위에 보여 플레이어와 대상 사이의 바닥이 화면에 담긴다.")]
        [SerializeField, Range(-30f, 30f)] private float _pitchBias = 10f;
        [Tooltip("락온 중 가장 올려다보는 각도. 자유 시점의 MinPitch 이상이어야 해제할 때 각도가 튀지 않는다.")]
        [SerializeField, Range(-89f, 0f)] private float _minPitch = -20f;
        [Tooltip("락온 중 가장 내려다보는 각도. 자유 시점의 MaxPitch 이하여야 해제할 때 각도가 튀지 않는다.")]
        [SerializeField, Range(0f, 89f)] private float _maxPitch = 35f;

        [Header("Smoothing")]
        [Tooltip("목표 각도를 따라가는 스프링의 보간 시간(초).")]
        [SerializeField, Min(0.01f)] private float _smoothTime = 0.12f;
        [Tooltip("최대 회전 속도(deg/s). 대상을 지나쳐 뒤로 돌 때 화면이 휙 돌지 않게 한다.")]
        [SerializeField, Min(1f)] private float _maxSpeed = 540f;

        [Header("Close Range")]
        [Tooltip("대상과의 수평 거리가 이보다 가까우면 목표 각도를 갱신하지 않는다. 스쳐 지나갈 때 방향이 뒤집히는 것을 막는다.")]
        [SerializeField, Min(0f)] private float _holdDistance = 1f;

        public LockOnCameraSettings()
        {
        }

        public LockOnCameraSettings(float pitchBias, float minPitch, float maxPitch, float holdDistance)
        {
            _pitchBias = pitchBias;
            _minPitch = minPitch;
            _maxPitch = maxPitch;
            _holdDistance = holdDistance;
        }

        public float PitchBias => _pitchBias;
        public float MinPitch => _minPitch;
        public float MaxPitch => _maxPitch;
        public float SmoothTime => _smoothTime;
        public float MaxSpeed => _maxSpeed;
        public float HoldDistance => _holdDistance;
    }
}
