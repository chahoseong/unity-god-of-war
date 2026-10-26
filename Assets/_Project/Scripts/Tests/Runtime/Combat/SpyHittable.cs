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

        public HitResult ReceiveHit(in AttackInfo attack)
        {
            HitCount++;
            OnHit?.Invoke();
            return new HitResult(HitOutcome.Hit);
        }
    }
}
