using System;
using GodOfWar.Combat;
using UnityEngine;

namespace GodOfWar.Player
{
    /// <summary>
    /// 스텝 회피와 구르기 회피가 공유하는 튜닝 값과 회피 액션 데이터 참조.
    /// </summary>
    [Serializable]
    public sealed class DodgeSettings
    {
        [Tooltip("회피를 허용하는 최소 이동 입력 크기. 이보다 작으면 정지로 보고 회피하지 않는다.")]
        [SerializeField, Range(0f, 1f)] private float _minInput = 0.1f;

        [Tooltip("회피 클립 진입과 이동 클립 복귀에 쓰는 크로스페이드 시간(초).")]
        [SerializeField, Min(0f)] private float _crossFadeDuration = 0.1f;

        [Tooltip("2단 회피의 1단계인 스텝 회피.")]
        [SerializeField] private DodgeAction _quickStep;

        [Tooltip("스텝 도중 재입력으로 이어지는 구르기 회피.")]
        [SerializeField] private DodgeAction _dodgeRoll;

        public float MinInput => _minInput;
        public float CrossFadeDuration => _crossFadeDuration;
        public DodgeAction QuickStep => _quickStep;
        public DodgeAction DodgeRoll => _dodgeRoll;
    }
}
