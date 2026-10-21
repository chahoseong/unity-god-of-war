using System;
using GodOfWar.Combat;
using GodOfWar.Input;
using UnityEngine;

namespace GodOfWar.Player
{
    /// <summary>
    /// 약공격·강공격 콤보가 공유하는 튜닝 값과 버튼별 1타 참조.
    /// </summary>
    [Serializable]
    public sealed class AttackSettings
    {
        [Tooltip("약공격 버튼으로 시작하는 콤보의 1타.")]
        [SerializeField] private AttackAction _lightEntry;

        [Tooltip("강공격 버튼으로 시작하는 콤보의 1타.")]
        [SerializeField] private AttackAction _heavyEntry;

        [Tooltip("공격 방향을 입력으로 정하는 최소 이동 입력 크기. 이보다 작으면 카메라 정면으로 공격한다.")]
        [SerializeField, Range(0f, 1f)] private float _minInput = 0.1f;

        [Tooltip("선딜레이 동안 목표 방향으로 도는 속도(도/초).")]
        [SerializeField, Min(0f)] private float _turnSpeed = 720f;

        public float MinInput => _minInput;
        public float TurnSpeed => _turnSpeed;

        /// <summary><paramref name="input"/> 버튼으로 시작하는 콤보의 1타. 할당되지 않았으면 null이다.</summary>
        public AttackAction GetEntry(PlayerAction input)
        {
            return input switch
            {
                PlayerAction.LightAttack => _lightEntry,
                PlayerAction.HeavyAttack => _heavyEntry,
                _ => throw new ArgumentOutOfRangeException(nameof(input), input, "Not an attack input.")
            };
        }
    }
}
