using GodOfWar.Characters;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 콤보를 이루는 공격 한 타의 액션 데이터.
    /// 공격 클립이 끝나면 복귀 클립으로 이어지고, 연계 구간 안의 입력은 <see cref="Next"/> 또는 다른 버튼의 1타로 이어진다.
    /// 시각은 모두 액션 시작부터 잰 초다.
    /// </summary>
    [CreateAssetMenu(menuName = "GodOfWar/Combat/Attack Action", fileName = "AttackAction")]
    public sealed class AttackAction : ScriptableObject
    {
        [Tooltip("공격 동작을 재생할 애니메이터 상태 이름.")]
        [SerializeField] private string _stateName;

        [Tooltip("후속 입력이 없을 때 이어서 재생할 복귀 상태 이름.")]
        [SerializeField] private string _returnStateName;

        [Tooltip("공격 상태의 클립. 길이가 공격 구간의 길이다.")]
        [SerializeField] private AnimationClip _clip;

        [Tooltip("복귀 상태의 클립. 길이가 복귀 구간의 길이다.")]
        [SerializeField] private AnimationClip _returnClip;

        [Tooltip("공격·복귀·이동 상태로 넘어갈 때의 크로스페이드 시간(초).")]
        [SerializeField, Min(0f)] private float _crossFadeDuration = 0.1f;

        [Tooltip("이 시각까지 목표 방향으로 몸을 돌린다. 보통 선딜레이(WindUp)가 끝나는 시각이다.")]
        [SerializeField, Min(0f)] private float _trackingEnd;

        [Tooltip("후속 입력을 받기 시작하는 시각.")]
        [SerializeField, Min(0f)] private float _comboWindowStart;

        [Tooltip("후속 입력을 받는 마지막 시각. 공격 클립 길이를 넘겨 복귀 구간까지 둘 수 있다.")]
        [SerializeField, Min(0f)] private float _comboWindowEnd;

        [Tooltip("공격 구간 동안의 전진 이동.")]
        [SerializeField] private BakedRootMotion _movement;

        [Tooltip("같은 버튼을 다시 눌렀을 때 이어지는 다음 타. 비우면 같은 버튼은 1타부터 다시 시작한다.")]
        [SerializeField] private AttackAction _next;

        public string StateName => _stateName;
        public string ReturnStateName => _returnStateName;
        public AnimationClip Clip => _clip;
        public AnimationClip ReturnClip => _returnClip;
        public float CrossFadeDuration => _crossFadeDuration;
        public float TrackingEnd => _trackingEnd;
        public float ComboWindowStart => _comboWindowStart;
        public float ComboWindowEnd => _comboWindowEnd;
        public BakedRootMotion Movement => _movement;
        public AttackAction Next => _next;

        /// <summary>공격 구간의 길이(초).</summary>
        public float Duration => _clip.length;

        /// <summary>복귀 구간의 길이(초).</summary>
        public float ReturnDuration => _returnClip.length;

        /// <summary><paramref name="elapsed"/>가 연계 구간 안인지 판정한다. 구간의 양 끝을 포함한다.</summary>
        public bool IsInComboWindow(float elapsed)
        {
            return elapsed >= _comboWindowStart && elapsed <= _comboWindowEnd;
        }
    }
}
