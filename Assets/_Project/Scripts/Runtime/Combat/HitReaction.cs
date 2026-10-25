using System;
using GodOfWar.Characters;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 피격 반응 하나의 데이터. 반응 클립을 재생하는 동안 정해 둔 만큼 밀려난다.
    /// </summary>
    [Serializable]
    public sealed class HitReaction
    {
        [Tooltip("반응을 재생할 애니메이터 상태 이름.")]
        [SerializeField] private string _stateName;

        [Tooltip("반응 상태의 클립. 길이가 반응 시간이다.")]
        [SerializeField] private AnimationClip _clip;

        [Tooltip("반응 동안 밀려나는 이동. 제자리 반응이면 비워 둔다.")]
        [SerializeField] private BakedRootMotion _movement;

        [Tooltip("반응으로 들어갈 때와 반응을 마치고 대기로 돌아갈 때의 크로스페이드 시간(초).")]
        [SerializeField, Min(0f)] private float _crossFadeDuration = 0.1f;

        public string StateName => _stateName;
        public AnimationClip Clip => _clip;
        public BakedRootMotion Movement => _movement;
        public float CrossFadeDuration => _crossFadeDuration;

        /// <summary>반응 시간(초).</summary>
        public float Duration => _clip.length;
    }
}
