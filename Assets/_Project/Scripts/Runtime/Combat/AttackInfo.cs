using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 공격자가 피격자에게 넘기는 타격 한 번의 사실. 공격의 성질(공격자·세기·넉다운·피해량)과 몸에 닿은 지점처럼 공격하는 쪽이 아는 것을 담는다.
    /// 공격 에셋(<see cref="AttackAction"/>)이 아니라, 판정이 대상에 닿은 순간마다 새로 만드는 값이다.
    /// </summary>
    public readonly struct AttackInfo
    {
        public AttackInfo(Transform attacker, HitStrength strength, bool knockdown, int damage, ImpactPoint impactPoint)
        {
            Attacker = attacker;
            Strength = strength;
            Knockdown = knockdown;
            Damage = damage;
            ImpactPoint = impactPoint;
        }

        /// <summary>공격한 캐릭터의 루트. 피격 방향은 이 위치를 기준으로 판정한다.</summary>
        public Transform Attacker { get; }

        public HitStrength Strength { get; }

        /// <summary>공격이 대상을 넉다운시키려 하는지. 세기와는 따로 정한다. 실제로 쓰러질지는 피격자가 정한다.</summary>
        public bool Knockdown { get; }

        /// <summary>공격이 전하는 피해량. 체력을 실제로 얼마나 깎을지는 피격자가 정한다.</summary>
        public int Damage { get; }

        /// <summary>타격이 피격자의 몸에 닿은 지점. 타격 이펙트를 이 위치와 방향에 띄운다.</summary>
        public ImpactPoint ImpactPoint { get; }
    }
}
