using System;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 피격자가 받은 타격을 처리한다. 결과를 가르는 상태(가드·패링 등)는 피격자 안에 있으므로,
    /// 판정과 그에 따른 상태 전이(반응)를 모두 이 안에서 직접 한다.
    /// </summary>
    /// <returns>피격자가 내린 판정.</returns>
    public delegate HitResult HitHandler(in AttackInfo attack);

    /// <summary>
    /// 캐릭터가 타격을 받는 입구. 무기와 전달 시스템이 겨냥하는 대상이 이것이다.
    /// 판정은 소유자가 맡긴 <see cref="HitHandler"/>에 넘기고, 판정이 끝나면 <see cref="HitTaken"/>으로 알린다.
    /// 그래서 피격 연출처럼 판정을 관찰만 하는 쪽은 캐릭터 종류를 몰라도 이것 하나만 구독하면 된다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitTarget : MonoBehaviour, IHittable
    {
        private HitHandler _handler;

        /// <summary>
        /// 받은 타격을 판정과 상태 전이까지 마친 뒤 알린다. 회피로 흘린 타격도 알리므로 무엇을 거를지는 구독자가 정한다.
        /// 타격을 전하는 흐름 안에서 동기로 호출된다. 구독자 하나가 예외를 던져도 기록만 하고 다른 구독자와 판정은 그대로 이어진다.
        /// </summary>
        public event Action<HitEvent> HitTaken;

        /// <summary>이 대상이 받은 타격을 판정할 쪽을 정한다. 소유자가 한 번만 호출한다.</summary>
        /// <exception cref="InvalidOperationException">이미 판정할 쪽이 정해져 있다.</exception>
        public void Bind(HitHandler handler)
        {
            if (_handler != null)
            {
                throw new InvalidOperationException($"'{name}' already has a {nameof(HitHandler)}.");
            }

            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// 판정을 맡긴 쪽에 넘겨 판정을 받고, 알린 뒤 돌려준다. 판정이 실패하면 알리지 않고 예외를 그대로 올린다.
        /// </summary>
        /// <exception cref="InvalidOperationException">판정할 쪽이 정해지지 않았다.</exception>
        public HitResult ReceiveHit(in AttackInfo attack)
        {
            if (_handler == null)
            {
                throw new InvalidOperationException($"'{name}' has no {nameof(HitHandler)}. Its owner must call {nameof(Bind)}.");
            }

            HitResult result = _handler(attack);
            NotifyTaken(new HitEvent(this, attack, result));
            return result;
        }

        // 연출 하나의 오류로 다른 연출이나 공격자에게 돌아갈 판정까지 사라지지 않도록 구독자마다 따로 부른다.
        private void NotifyTaken(in HitEvent hitEvent)
        {
            if (HitTaken == null)
            {
                return;
            }

            foreach (Delegate subscriber in HitTaken.GetInvocationList())
            {
                try
                {
                    ((Action<HitEvent>)subscriber).Invoke(hitEvent);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }
        }
    }
}
