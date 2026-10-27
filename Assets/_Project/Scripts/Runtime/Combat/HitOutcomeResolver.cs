using System;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 피격자의 방어 자세와 공격으로 타격 판정 결과를 정하는 전투 규칙. 규칙은 여기에만 두고,
    /// 상태는 <see cref="IDefensible"/>로 자세만 답한다.
    /// </summary>
    public static class HitOutcomeResolver
    {
        /// <param name="defenderPosition">피격자의 위치.</param>
        /// <param name="defenderForward">피격자의 몸 정면.</param>
        /// <param name="guardHalfAngle">막을 수 있는 범위의 반각(도). 가드·패링 중일 때만 쓴다.</param>
        public static HitOutcome Resolve(
            DefenseStance stance, in AttackInfo attack,
            Vector3 defenderPosition, Vector3 defenderForward, float guardHalfAngle)
        {
            switch (stance)
            {
                case DefenseStance.Open:
                    return HitOutcome.Hit;
                case DefenseStance.Evading:
                    // 회피는 공격의 세기·방향과 관계없이 흘린다.
                    return HitOutcome.Evaded;
                case DefenseStance.Guarding:
                    if (!IsWithinGuard(defenderPosition, defenderForward, attack.Attacker.position, guardHalfAngle))
                    {
                        return HitOutcome.Hit;
                    }

                    return attack.Strength switch
                    {
                        HitStrength.Light => HitOutcome.Blocked,
                        HitStrength.Heavy => HitOutcome.GuardBroken,
                        _ => throw new ArgumentOutOfRangeException(nameof(attack), attack.Strength, null)
                    };
                case DefenseStance.Parrying:
                    // 막을 수 있는 범위는 가드와 같고, 범위 안이면 강공격도 튕겨 낸다.
                    return IsWithinGuard(defenderPosition, defenderForward, attack.Attacker.position, guardHalfAngle)
                        ? HitOutcome.Parried
                        : HitOutcome.Hit;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stance), stance, null);
            }
        }

        /// <summary>
        /// 공격자가 몸 정면 기준 반각 안에 있는지. 높이 차이는 무시하고, 수평으로 겹치면 정면으로 본다
        /// (<see cref="HitDirectionResolver"/>와 같은 기준).
        /// </summary>
        private static bool IsWithinGuard(
            Vector3 defenderPosition, Vector3 defenderForward, Vector3 attackerPosition, float halfAngle)
        {
            Vector3 toAttacker = attackerPosition - defenderPosition;
            toAttacker.y = 0f;
            defenderForward.y = 0f;
            return Vector3.Angle(defenderForward, toAttacker) <= halfAngle;
        }
    }
}
