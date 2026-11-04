using System;
using GodOfWar.Characters;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 넉다운 반응의 데이터. 쓰러지는 동작(Begin) → 누운 채 기다림(Loop) → 일어나는 동작(End) 순으로 재생한다.
    /// 쓰러지는 동안 정해 둔 만큼 뒤로 밀려난다. 동작은 뒤로 넘어지는 한 방향뿐이라, 재생하는 쪽이 먼저 공격자를 향해 돌아선다.
    /// </summary>
    [Serializable]
    public sealed class KnockdownReaction
    {
        [Tooltip("쓰러지는 동작을 재생할 애니메이터 상태 이름.")]
        [SerializeField] private string _beginStateName;

        [Tooltip("쓰러지는 동작의 클립. 길이가 쓰러지는 구간의 길이다.")]
        [SerializeField] private AnimationClip _beginClip;

        [Tooltip("쓰러지는 동안 밀려나는 이동. 제자리에서 쓰러지면 비워 둔다.")]
        [SerializeField] private BakedRootMotion _beginMovement;

        [Tooltip("누운 채 기다리는 동작을 재생할 애니메이터 상태 이름. 반복 클립이어야 한다.")]
        [SerializeField] private string _loopStateName;

        [Tooltip("누운 채 기다리는 시간(초, 캐릭터 시계 기준). 0이면 쓰러지자마자 일어난다.")]
        [SerializeField, Min(0f)] private float _lyingDuration = 0.5f;

        [Tooltip("일어나는 동작을 재생할 애니메이터 상태 이름.")]
        [SerializeField] private string _endStateName;

        [Tooltip("일어나는 동작의 클립. 길이가 기상 구간의 길이다.")]
        [SerializeField] private AnimationClip _endClip;

        [Tooltip("넉다운으로 들어가는 크로스페이드 시간(초). 반응은 역경직과 함께 시작해, 0이 아니면 느려진 동안 이전 자세가 오래 섞인다.")]
        [SerializeField, Min(0f)] private float _crossFadeInDuration;

        [Tooltip("쓰러짐 → 누움, 누움 → 기상으로 넘어가는 크로스페이드 시간(초).")]
        [SerializeField, Min(0f)] private float _phaseCrossFadeDuration = 0.1f;

        [Tooltip("기상을 마치고 대기로 돌아가는 크로스페이드 시간(초).")]
        [SerializeField, Min(0f)] private float _crossFadeOutDuration = 0.2f;

        public string BeginStateName => _beginStateName;
        public AnimationClip BeginClip => _beginClip;
        public BakedRootMotion BeginMovement => _beginMovement;
        public string LoopStateName => _loopStateName;
        public float LyingDuration => _lyingDuration;
        public string EndStateName => _endStateName;
        public AnimationClip EndClip => _endClip;
        public float CrossFadeInDuration => _crossFadeInDuration;
        public float PhaseCrossFadeDuration => _phaseCrossFadeDuration;
        public float CrossFadeOutDuration => _crossFadeOutDuration;

        /// <summary>쓰러지는 구간의 길이(초).</summary>
        public float BeginDuration => _beginClip.length;

        /// <summary>기상 구간의 길이(초).</summary>
        public float EndDuration => _endClip.length;
    }
}
