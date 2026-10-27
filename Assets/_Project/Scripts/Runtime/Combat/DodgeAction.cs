using System;
using GodOfWar.Characters;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 회피 한 종류(스텝 또는 구르기)의 액션 데이터. 클립 방향마다 구운 루트 변위를 참조한다.
    /// 시각은 모두 회피 시작부터 잰 초다.
    /// </summary>
    [CreateAssetMenu(menuName = "GodOfWar/Combat/Dodge Action", fileName = "DodgeAction")]
    public sealed class DodgeAction : ScriptableObject
    {
        [SerializeField] private BakedRootMotion _forward;
        [SerializeField] private BakedRootMotion _backward;
        [SerializeField] private BakedRootMotion _left;
        [SerializeField] private BakedRootMotion _right;

        [Tooltip("공격을 흘리는 무적 구간이 시작되는 시각.")]
        [SerializeField, Min(0f)] private float _invulnerableStart;

        [Tooltip("무적 구간이 끝나는 시각. 이 시각부터는 맞는다. 클립 길이를 넘기지 않는다.")]
        [SerializeField, Min(0f)] private float _invulnerableEnd;

        public float InvulnerableStart => _invulnerableStart;
        public float InvulnerableEnd => _invulnerableEnd;

        /// <summary><paramref name="elapsed"/>가 무적 구간 안인지 판정한다. 시작 시각은 포함하고 끝 시각은 포함하지 않는다.</summary>
        public bool IsInvulnerable(float elapsed)
        {
            return elapsed >= _invulnerableStart && elapsed < _invulnerableEnd;
        }

        public BakedRootMotion GetRootMotion(DodgeCardinal cardinal)
        {
            return cardinal switch
            {
                DodgeCardinal.Forward => _forward,
                DodgeCardinal.Backward => _backward,
                DodgeCardinal.Left => _left,
                DodgeCardinal.Right => _right,
                _ => throw new ArgumentOutOfRangeException(nameof(cardinal), cardinal, null)
            };
        }
    }
}
