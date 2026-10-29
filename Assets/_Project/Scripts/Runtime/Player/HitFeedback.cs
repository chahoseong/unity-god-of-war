using System;
using UnityEngine;

namespace GodOfWar.Player
{
    /// <summary>
    /// 타격 한 번에 내는 피드백. 플레이어와 상대를 얼마나 오래, 얼마나 느리게 할지(역경직) 담는다.
    /// 같은 충돌이므로 두 캐릭터에 똑같이 적용한다.
    /// </summary>
    [Serializable]
    public sealed class HitFeedback
    {
        [Tooltip("느려지는 시간(초, 전역 게임 시간).")]
        [SerializeField, Min(0f)] private float _duration;

        [Tooltip("느려진 동안의 재생 배율. 0이면 멈추고, 1이면 느려지지 않는다.")]
        [SerializeField, Range(0f, 1f)] private float _timeScale = 1f;

        public float Duration => _duration;
        public float TimeScale => _timeScale;
    }
}
