using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 공격자가 피격자에게 넘기는 타격 한 번의 정보.
    /// 공격 에셋(<see cref="AttackAction"/>)이 아니라, 판정이 대상에 닿은 순간마다 새로 만드는 값이다.
    /// </summary>
    public readonly struct AttackInfo
    {
        public AttackInfo(Transform attacker, HitStrength strength)
        {
            Attacker = attacker;
            Strength = strength;
        }

        /// <summary>공격한 캐릭터의 루트. 피격 방향은 이 위치를 기준으로 판정한다.</summary>
        public Transform Attacker { get; }

        public HitStrength Strength { get; }
    }
}
