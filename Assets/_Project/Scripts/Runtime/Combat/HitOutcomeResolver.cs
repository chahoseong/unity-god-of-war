using System;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 피격자의 방어 자세로 타격 판정 결과를 정하는 전투 규칙. 규칙은 여기에만 두고,
    /// 상태는 <see cref="IDefensible"/>로 자세만 답한다. 회피·가드가 생기면 규칙이 늘어난다.
    /// </summary>
    public static class HitOutcomeResolver
    {
        public static HitOutcome Resolve(DefenseStance stance)
        {
            return stance switch
            {
                DefenseStance.Open => HitOutcome.Hit,
                _ => throw new ArgumentOutOfRangeException(nameof(stance), stance, null)
            };
        }
    }
}
