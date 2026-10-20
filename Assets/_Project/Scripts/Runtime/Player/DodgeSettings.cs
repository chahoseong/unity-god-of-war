using System;
using GodOfWar.Player.States;
using UnityEngine;

namespace GodOfWar.Player
{
    /// <summary>
    /// 스텝 회피와 구르기 회피가 공유하는 튜닝 값.
    /// 프로필 기본값은 Synty 회피 클립(_RootMotion)의 루트 궤적에서 추출했다.
    /// </summary>
    [Serializable]
    public sealed class DodgeSettings
    {
        // 30fps 프레임별 정규화 변위. _RootMotion 클립의 RootT(무게중심) 궤적을 클립 이동 방향으로 투영한 값이며,
        // 원본 모션의 미세한 반동(1 초과·0 미만)을 그대로 담고 있다.
        private static readonly float[] StepForward =
            { 0f, 0.010f, 0.042f, 0.095f, 0.176f, 0.282f, 0.401f, 0.519f, 0.631f, 0.740f, 0.844f, 0.908f, 0.951f, 0.976f, 0.990f, 0.995f, 0.999f, 1f };
        private static readonly float[] StepBackward =
            { 0f, 0.010f, 0.039f, 0.082f, 0.141f, 0.220f, 0.324f, 0.461f, 0.597f, 0.728f, 0.806f, 0.874f, 0.932f, 0.975f, 0.985f, 0.994f, 0.998f, 1f };
        private static readonly float[] StepLeft =
            { 0f, -0.007f, -0.001f, 0.047f, 0.144f, 0.256f, 0.378f, 0.499f, 0.621f, 0.737f, 0.818f, 0.895f, 0.956f, 0.991f, 1.001f, 1.003f, 1.001f, 1f };
        private static readonly float[] StepRight =
            { 0f, 0.028f, 0.079f, 0.144f, 0.222f, 0.333f, 0.459f, 0.583f, 0.707f, 0.814f, 0.897f, 0.960f, 1.005f, 1.024f, 1.018f, 1.011f, 1.003f, 1f };

        private static readonly float[] RollForward =
        {
            0f, 0.007f, 0.025f, 0.053f, 0.088f, 0.137f, 0.201f, 0.271f, 0.340f, 0.399f, 0.456f, 0.501f,
            0.549f, 0.612f, 0.673f, 0.717f, 0.759f, 0.791f, 0.808f, 0.831f, 0.858f, 0.880f, 0.902f, 0.928f,
            0.948f, 0.965f, 0.978f, 0.985f, 0.990f, 0.995f, 0.999f, 1.002f, 1.002f, 1.001f, 1f, 1f
        };
        private static readonly float[] RollBackward =
        {
            0f, 0.013f, 0.035f, 0.067f, 0.100f, 0.125f, 0.152f, 0.185f, 0.231f, 0.280f, 0.341f, 0.400f,
            0.446f, 0.490f, 0.541f, 0.602f, 0.652f, 0.695f, 0.729f, 0.769f, 0.811f, 0.847f, 0.878f, 0.902f,
            0.925f, 0.942f, 0.955f, 0.967f, 0.977f, 0.986f, 0.992f, 0.997f, 0.999f, 1f, 1f, 1f
        };
        private static readonly float[] RollLeft =
        {
            0f, 0.005f, 0.018f, 0.044f, 0.076f, 0.120f, 0.174f, 0.226f, 0.283f, 0.355f, 0.431f, 0.497f,
            0.549f, 0.594f, 0.625f, 0.658f, 0.699f, 0.732f, 0.754f, 0.780f, 0.813f, 0.847f, 0.881f, 0.905f,
            0.929f, 0.943f, 0.954f, 0.962f, 0.970f, 0.978f, 0.986f, 0.991f, 0.995f, 0.998f, 0.999f, 1f
        };
        private static readonly float[] RollRight =
        {
            0f, 0.004f, 0.019f, 0.047f, 0.075f, 0.123f, 0.178f, 0.230f, 0.287f, 0.360f, 0.435f, 0.498f,
            0.542f, 0.577f, 0.605f, 0.642f, 0.688f, 0.725f, 0.750f, 0.779f, 0.811f, 0.846f, 0.877f, 0.900f,
            0.930f, 0.951f, 0.967f, 0.980f, 0.989f, 0.995f, 0.999f, 1.001f, 1.001f, 1.001f, 1f, 1f
        };

        [Tooltip("회피를 허용하는 최소 이동 입력 크기. 이보다 작으면 정지로 보고 회피하지 않는다.")]
        [SerializeField, Range(0f, 1f)] private float _minInput = 0.1f;

        [Tooltip("회피 클립 진입과 이동 클립 복귀에 쓰는 크로스페이드 시간(초).")]
        [SerializeField, Min(0f)] private float _crossFadeDuration = 0.1f;

        // 거리는 클립 원본(아바타 humanScale 적용), 시간은 클립 길이(30fps 17·35프레임)다.
        [SerializeField] private DodgeProfile _step = new DodgeProfile(
            1f, 17f / 30f,
            FromFrameSamples(StepForward), FromFrameSamples(StepBackward),
            FromFrameSamples(StepLeft), FromFrameSamples(StepRight));

        [SerializeField] private DodgeProfile _roll = new DodgeProfile(
            4.35f, 35f / 30f,
            FromFrameSamples(RollForward), FromFrameSamples(RollBackward),
            FromFrameSamples(RollLeft), FromFrameSamples(RollRight));

        public float MinInput => _minInput;
        public float CrossFadeDuration => _crossFadeDuration;
        public DodgeProfile Step => _step;
        public DodgeProfile Roll => _roll;

        /// <summary>
        /// 0~1 정규화 시간에 균등 배치한 샘플을 선형 탄젠트로 잇는다.
        /// 탄젠트를 0으로 두면 키마다 속도가 0으로 떨어져 매 프레임 멈칫거린다.
        /// </summary>
        private static AnimationCurve FromFrameSamples(float[] samples)
        {
            int last = samples.Length - 1;
            var keys = new Keyframe[samples.Length];
            for (int i = 0; i <= last; i++)
            {
                float inSlope = i > 0 ? (samples[i] - samples[i - 1]) * last : 0f;
                float outSlope = i < last ? (samples[i + 1] - samples[i]) * last : 0f;
                keys[i] = new Keyframe((float)i / last, samples[i], inSlope, outSlope);
            }

            return new AnimationCurve(keys);
        }
    }

    /// <summary>
    /// 회피 한 번의 이동 프로필. 거리와 시간은 방향과 무관하고, 변위 커브만 클립 방향별로 다르다.
    /// </summary>
    [Serializable]
    public sealed class DodgeProfile
    {
        private const string CurveTooltip =
            "정규화 시간(0~1) → 정규화 변위. (0, 0)에서 시작해 (1, 1)에서 끝나야 총 이동 거리가 Distance와 같다.";

        [Tooltip("회피 한 번의 총 이동 거리(m).")]
        [SerializeField, Min(0f)] private float _distance;

        [Tooltip("회피 지속 시간(초). 경과하면 이동 상태로 돌아간다.")]
        [SerializeField, Min(0.01f)] private float _duration;

        [Tooltip(CurveTooltip)] [SerializeField] private AnimationCurve _forwardCurve;
        [Tooltip(CurveTooltip)] [SerializeField] private AnimationCurve _backwardCurve;
        [Tooltip(CurveTooltip)] [SerializeField] private AnimationCurve _leftCurve;
        [Tooltip(CurveTooltip)] [SerializeField] private AnimationCurve _rightCurve;

        public DodgeProfile(
            float distance, float duration,
            AnimationCurve forwardCurve, AnimationCurve backwardCurve,
            AnimationCurve leftCurve, AnimationCurve rightCurve)
        {
            _distance = distance;
            _duration = duration;
            _forwardCurve = forwardCurve;
            _backwardCurve = backwardCurve;
            _leftCurve = leftCurve;
            _rightCurve = rightCurve;
        }

        public float Distance => _distance;
        public float Duration => _duration;

        public AnimationCurve GetDisplacementCurve(DodgeCardinal cardinal)
        {
            return cardinal switch
            {
                DodgeCardinal.Forward => _forwardCurve,
                DodgeCardinal.Backward => _backwardCurve,
                DodgeCardinal.Left => _leftCurve,
                DodgeCardinal.Right => _rightCurve,
                _ => throw new ArgumentOutOfRangeException(nameof(cardinal), cardinal, null)
            };
        }
    }
}
