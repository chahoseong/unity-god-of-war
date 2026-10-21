using System;
using GodOfWar.Characters;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 회피 한 종류(스텝 또는 구르기)의 액션 데이터. 클립 방향마다 구운 루트 변위를 참조한다.
    /// </summary>
    [CreateAssetMenu(menuName = "GodOfWar/Combat/Dodge Action", fileName = "DodgeAction")]
    public sealed class DodgeAction : ScriptableObject
    {
        [SerializeField] private BakedRootMotion _forward;
        [SerializeField] private BakedRootMotion _backward;
        [SerializeField] private BakedRootMotion _left;
        [SerializeField] private BakedRootMotion _right;

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
