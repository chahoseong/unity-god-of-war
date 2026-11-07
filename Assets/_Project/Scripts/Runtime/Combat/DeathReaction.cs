using System;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 처치 반응 하나의 데이터. 쓰러지는 클립을 끝까지 재생한 뒤 마지막 자세로 누워 있는다.
    /// 대기로 돌아가지 않으므로 나가는 크로스페이드나 이동은 없다. 몸은 클립 안에서만 움직이고 캐릭터는 제자리에 있다.
    /// </summary>
    [Serializable]
    public sealed class DeathReaction
    {
        [Tooltip("쓰러지는 동작을 재생할 애니메이터 상태 이름. 반복하지 않는 클립이어야 마지막 자세에 멈춘다.")]
        [SerializeField] private string _stateName;

        [Tooltip("쓰러지는 동작의 클립.")]
        [SerializeField] private AnimationClip _clip;

        [Tooltip("처치 반응으로 들어가는 크로스페이드 시간(초). 반응은 역경직과 함께 시작해, 0이 아니면 느려진 동안 이전 자세가 오래 섞인다.")]
        [SerializeField, Min(0f)] private float _crossFadeInDuration;

        public string StateName => _stateName;
        public AnimationClip Clip => _clip;
        public float CrossFadeInDuration => _crossFadeInDuration;
    }
}
