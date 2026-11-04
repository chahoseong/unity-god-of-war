using System;
using GodOfWar.CameraSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace GodOfWar.Player.Feedback
{
    /// <summary>
    /// 타격 한 번에 내는 피드백. 플레이어와 상대를 얼마나 오래, 얼마나 느리게 할지(역경직)와 카메라를 어떻게 흔들지 담는다.
    /// 같은 충돌이므로 역경직은 두 캐릭터에 똑같이 적용한다.
    /// </summary>
    [Serializable]
    public sealed class HitFeedback
    {
        [Header("Hitstop")]
        [Tooltip("느려지는 시간(초, 전역 게임 시간).")]
        [FormerlySerializedAs("_duration")]
        [SerializeField, Min(0f)] private float _hitstopDuration;

        [Tooltip("느려진 동안의 재생 배율. 0이면 멈추고, 1이면 느려지지 않는다.")]
        [FormerlySerializedAs("_timeScale")]
        [SerializeField, Range(0f, 1f)] private float _hitstopTimeScale = 1f;

        [Header("Camera Shake")]
        [Tooltip("흔들림의 모양.")]
        [SerializeField] private CameraShakeShape _shakeShape;

        [Tooltip("흔들리는 시간(초, 전역 게임 시간).")]
        [SerializeField, Min(0f)] private float _shakeDuration = 0.15f;

        [Tooltip("화면 아래쪽으로 밀리는 최대 거리(m). 회전 떨림도 이 값에 비례한다. 0이면 흔들지 않는다.")]
        [SerializeField, Min(0f)] private float _shakeForce;

        public float HitstopDuration => _hitstopDuration;
        public float HitstopTimeScale => _hitstopTimeScale;
        public CameraShakeShape ShakeShape => _shakeShape;
        public float ShakeDuration => _shakeDuration;
        public float ShakeForce => _shakeForce;
    }
}
