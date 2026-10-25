using GodOfWar.Combat;
using UnityEngine;

namespace GodOfWar.Tests.Combat
{
    /// <summary>받은 타격 횟수만 세는 테스트용 피격 대상.</summary>
    public sealed class SpyHittable : MonoBehaviour, IHittable
    {
        public int HitCount { get; private set; }

        public HitResult ReceiveHit(in AttackInfo attack)
        {
            HitCount++;
            return new HitResult(HitOutcome.Hit);
        }
    }
}
