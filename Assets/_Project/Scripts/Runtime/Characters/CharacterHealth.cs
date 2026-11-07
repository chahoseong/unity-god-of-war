using System;
using UnityEngine;

namespace GodOfWar.Characters
{
    /// <summary>
    /// 캐릭터의 체력. 남은 양을 관리하고 바뀔 때마다 알릴 뿐, 언제 깎을지와 바닥나면 무엇을 할지는 소유자가 정한다.
    /// 처음에는 최대 체력으로 시작한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHealth : MonoBehaviour
    {
        [Tooltip("최대 체력. 처음에는 이만큼 채운 채로 시작한다.")]
        [SerializeField, Min(1)] private int _maxHealth = 100;

        public int Max => _maxHealth;

        /// <summary>남은 체력. 0 아래로 내려가지 않는다.</summary>
        public int Current { get; private set; }

        /// <summary>체력이 바닥났는지.</summary>
        public bool IsDepleted => Current == 0;

        /// <summary>남은 체력이 바뀌었다. 깎아도 값이 그대로면(이미 바닥났거나 피해량이 0) 알리지 않는다.</summary>
        public event Action Changed;

        private void Awake()
        {
            Current = _maxHealth;
        }

        /// <summary><paramref name="amount"/>만큼 깎는다. 남은 체력보다 많으면 0에서 멈춘다.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount"/>가 음수다.</exception>
        public void TakeDamage(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Damage must not be negative.");
            }

            int next = Mathf.Max(Current - amount, 0);
            if (next == Current)
            {
                return;
            }

            Current = next;
            Changed?.Invoke();
        }
    }
}
