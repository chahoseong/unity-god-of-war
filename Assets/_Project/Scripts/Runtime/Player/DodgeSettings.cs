using System;
using UnityEngine;

namespace GodOfWar.Player
{
    /// <summary>
    /// 스텝 회피와 구르기 회피가 공유하는 튜닝 값.
    /// </summary>
    [Serializable]
    public sealed class DodgeSettings
    {
        [Tooltip("회피를 허용하는 최소 이동 입력 크기. 이보다 작으면 정지로 보고 회피하지 않는다.")]
        [SerializeField, Range(0f, 1f)] private float _minInput = 0.1f;

        [Tooltip("회피 클립 진입과 이동 클립 복귀에 쓰는 크로스페이드 시간(초).")]
        [SerializeField, Min(0f)] private float _crossFadeDuration = 0.1f;

        // TODO(2단계): 거리·시간·커브를 _RootMotion 클립에서 추출한 값으로 교체한다. 시간은 클립 길이(30fps 가정)다.
        [SerializeField] private DodgeProfile _step = new DodgeProfile(2f, 0.57f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
        [SerializeField] private DodgeProfile _roll = new DodgeProfile(4f, 1.17f, AnimationCurve.Linear(0f, 0f, 1f, 1f));

        public float MinInput => _minInput;
        public float CrossFadeDuration => _crossFadeDuration;
        public DodgeProfile Step => _step;
        public DodgeProfile Roll => _roll;
    }

    /// <summary>
    /// 회피 한 번의 이동 프로필.
    /// </summary>
    [Serializable]
    public sealed class DodgeProfile
    {
        [Tooltip("회피 한 번의 총 이동 거리(m).")]
        [SerializeField, Min(0f)] private float _distance;

        [Tooltip("회피 지속 시간(초). 경과하면 이동 상태로 돌아간다.")]
        [SerializeField, Min(0.01f)] private float _duration;

        [Tooltip("정규화 시간(0~1) → 정규화 변위(0~1). (0, 0)에서 시작해 (1, 1)에서 끝나야 총 이동 거리가 Distance와 같다.")]
        [SerializeField] private AnimationCurve _displacementCurve;

        public DodgeProfile(float distance, float duration, AnimationCurve displacementCurve)
        {
            _distance = distance;
            _duration = duration;
            _displacementCurve = displacementCurve;
        }

        public float Distance => _distance;
        public float Duration => _duration;
        public AnimationCurve DisplacementCurve => _displacementCurve;
    }
}
