using System;
using GodOfWar.Characters;
using UnityEngine;
using UnityEngine.Serialization;

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

        [Tooltip("반응으로 들어가는 크로스페이드 시간(초). 반응은 역경직과 함께 시작해, 0이 아니면 느려진 동안 이전 자세가 오래 섞인다.")]
        [SerializeField, Min(0f)] private float _crossFadeInDuration;

        [Tooltip("반응을 마치고 대기로 돌아가는 크로스페이드 시간(초).")]
        [FormerlySerializedAs("_crossFadeDuration")]
        [SerializeField, Min(0f)] private float _crossFadeOutDuration = 0.1f;

        public string StateName => _stateName;
        public AnimationClip Clip => _clip;
        public BakedRootMotion Movement => _movement;
        public float CrossFadeInDuration => _crossFadeInDuration;
        public float CrossFadeOutDuration => _crossFadeOutDuration;

        /// <summary>반응 시간(초).</summary>
        public float Duration => _clip.length;
    }
}
