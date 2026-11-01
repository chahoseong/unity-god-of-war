using System;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 피격 반짝임의 모양. 어떤 색으로, 얼마나 밝게, 얼마나 오래, 어떤 모양으로 사라질지 담는다.
    /// 사라지는 모양은 모든 세기가 같이 쓰고, 세기마다 다른 것은 지속 시간과 밝기뿐이다.
    /// 같은 반짝임을 쓰는 캐릭터끼리 공유한다.
    /// </summary>
    [CreateAssetMenu(menuName = "GodOfWar/Combat/Hit Flash Settings", fileName = "HitFlashSettings")]
    public sealed class HitFlashSettings : ScriptableObject
    {
        [Tooltip("반짝임을 그릴 오버레이 머티리얼. 캐릭터마다 사본을 만들어 색만 바꾼다.")]
        [SerializeField] private Material _overlayMaterial;

        [Tooltip("반짝임 색. 밝기를 곱한 값이 블룸 임계값을 넘으면 빛이 번진다.")]
        [SerializeField, ColorUsage(false, true)] private Color _color = Color.white;

        [Tooltip("진행도(0~1)에 따른 밝기 비율. 시작 1에서 끝 0으로 사라지는 모양만 뜻하며, 0~1 밖의 값은 잘린다.")]
        [SerializeField] private AnimationCurve _falloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [Header("Light")]
        [Tooltip("약한 타격에 반짝이는 시간(초, 피격자 시계 기준). 0이면 반짝이지 않는다.")]
        [SerializeField, Min(0f)] private float _lightDuration = 0.1f;

        [Tooltip("약한 타격의 시작 밝기. 색에 곱한다.")]
        [SerializeField, Min(0f)] private float _lightIntensity = 1f;

        [Header("Heavy")]
        [Tooltip("강한 타격에 반짝이는 시간(초, 피격자 시계 기준). 0이면 반짝이지 않는다.")]
        [SerializeField, Min(0f)] private float _heavyDuration = 0.1f;

        [Tooltip("강한 타격의 시작 밝기. 색에 곱한다.")]
        [SerializeField, Min(0f)] private float _heavyIntensity = 1f;

        public Material OverlayMaterial => _overlayMaterial;
        public Color Color => _color;

        public float GetDuration(HitStrength strength)
        {
            return strength switch
            {
                HitStrength.Light => _lightDuration,
                HitStrength.Heavy => _heavyDuration,
                _ => throw new ArgumentOutOfRangeException(nameof(strength), strength, null)
            };
        }

        public float GetIntensity(HitStrength strength)
        {
            return strength switch
            {
                HitStrength.Light => _lightIntensity,
                HitStrength.Heavy => _heavyIntensity,
                _ => throw new ArgumentOutOfRangeException(nameof(strength), strength, null)
            };
        }

        /// <summary>진행도(0~1)에서의 밝기 비율(0~1).</summary>
        public float EvaluateFalloff(float progress)
        {
            return Mathf.Clamp01(_falloff.Evaluate(progress));
        }
    }
}
