using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 검 궤적 한 줄기의 외형. 어떤 색으로, 얼마나 오래 남아, 어떤 모양으로 흐려질지, 칼끝에서 손잡이 쪽으로 얼마나 연해질지 담는다.
    /// 공격 데이터가 참조하며, 같은 느낌을 낼 공격끼리 공유한다.
    /// </summary>
    [CreateAssetMenu(menuName = "GodOfWar/Combat/Weapon Trail Profile", fileName = "WeaponTrailProfile")]
    public sealed class WeaponTrailProfile : ScriptableObject
    {
        [Tooltip("궤적 색. 흐려지는 비율을 곱한 값이 블룸 임계값을 넘으면 빛이 번진다.")]
        [SerializeField, ColorUsage(false, true)] private Color _color = Color.white;

        [Tooltip("칼날이 지나간 자리가 남아 있는 시간(초, 휘두른 캐릭터의 시계 기준).")]
        [SerializeField, Min(0f)] private float _lifetime = 0.2f;

        [Tooltip("남은 시간의 진행도(0 = 방금, 1 = 사라짐)에 따른 밝기 비율. 1에서 0으로 흐려지는 모양만 뜻하며, 0~1 밖의 값은 잘린다.")]
        [SerializeField] private AnimationCurve _fade = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [Tooltip("칼끝(1)에서 손잡이 쪽(0)으로 갈수록 연해지는 정도. 폭 위치를 이 값만큼 거듭제곱한 비율을 밝기에 곱한다. "
                 + "0이면 폭 전체가 균일하고, 1이면 선형, 클수록 밝기가 칼끝에 모인다.")]
        [SerializeField, Min(0f)] private float _widthFalloff = 2f;

        public Color Color => _color;
        public float Lifetime => _lifetime;
        public float WidthFalloff => _widthFalloff;

        /// <summary>진행도(0~1)에서의 밝기 비율(0~1).</summary>
        public float EvaluateFade(float progress)
        {
            return Mathf.Clamp01(_fade.Evaluate(progress));
        }
    }
}
