using System;
using GodOfWar.Combat;
using UnityEngine;

namespace GodOfWar.Tests.Combat
{
    /// <summary>받은 타격 횟수를 세는 테스트용 피격 대상. 맞을 때 할 일을 <see cref="OnHit"/>로 덧붙일 수 있다.</summary>
    public sealed class SpyHittable : MonoBehaviour, IHittable
    {
        public int HitCount { get; private set; }

        /// <summary>타격을 센 뒤 호출한다. 다른 칼을 닫거나 오브젝트를 파괴하는 등 피격 처리를 흉내 낼 때 쓴다.</summary>
        public Action OnHit { get; set; }

        /// <summary>돌려줄 판정. 회피 무적처럼 흘리는 대상을 흉내 낼 때 바꾼다.</summary>
        public HitOutcome Outcome { get; set; } = HitOutcome.Hit;

        public HitResult ReceiveHit(in AttackInfo attack)
        {
            HitCount++;
            HitOutcome outcome = Outcome;
            OnHit?.Invoke();
            return new HitResult(outcome);
        }
    }
}
