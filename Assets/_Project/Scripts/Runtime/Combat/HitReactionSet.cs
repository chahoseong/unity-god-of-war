using System;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 타격 세기와 피격 방향마다 재생할 피격 반응. 약한 타격은 약경직, 강한 타격은 강경직으로 반응한다.
    /// 넉다운시키는 타격에 쓰러지는 캐릭터는 세기·방향과 관계없이 넉다운 반응 하나를 쓴다.
    /// 처치되는 캐릭터는 마지막 타격이 들어온 쪽의 처치 반응으로 쓰러진다.
    /// 같은 반응 클립을 쓰는 캐릭터끼리 공유한다.
    /// </summary>
    [CreateAssetMenu(menuName = "GodOfWar/Combat/Hit Reaction Set", fileName = "HitReactionSet")]
    public sealed class HitReactionSet : ScriptableObject
    {
        [Header("Light")]
        [SerializeField] private HitReaction _lightFront = new HitReaction();
        [SerializeField] private HitReaction _lightBack = new HitReaction();
        [SerializeField] private HitReaction _lightLeft = new HitReaction();
        [SerializeField] private HitReaction _lightRight = new HitReaction();

        [Header("Heavy")]
        [SerializeField] private HitReaction _heavyFront = new HitReaction();
        [SerializeField] private HitReaction _heavyBack = new HitReaction();
        [SerializeField] private HitReaction _heavyLeft = new HitReaction();
        [SerializeField] private HitReaction _heavyRight = new HitReaction();

        [Header("Knockdown")]
        [Tooltip("넉다운시키는 타격에 맞았을 때. 쓰러지지 않는 캐릭터는 쓰지 않는다.")]
        [SerializeField] private KnockdownReaction _knockdown = new KnockdownReaction();

        [Header("Death")]
        [Tooltip("앞에서 들어온 타격에 처치됐을 때. 처치되지 않는 캐릭터는 처치 칸을 쓰지 않는다.")]
        [SerializeField] private DeathReaction _deathFront = new DeathReaction();
        [SerializeField] private DeathReaction _deathBack = new DeathReaction();
        [SerializeField] private DeathReaction _deathLeft = new DeathReaction();
        [SerializeField] private DeathReaction _deathRight = new DeathReaction();

        public KnockdownReaction Knockdown => _knockdown;

        /// <summary><paramref name="direction"/>에서 들어온 타격에 처치됐을 때의 반응.</summary>
        public DeathReaction GetDeath(HitDirection direction)
        {
            return Pick(direction, _deathFront, _deathBack, _deathLeft, _deathRight);
        }

        public HitReaction Get(HitStrength strength, HitDirection direction)
        {
            return strength switch
            {
                HitStrength.Light => Pick(direction, _lightFront, _lightBack, _lightLeft, _lightRight),
                HitStrength.Heavy => Pick(direction, _heavyFront, _heavyBack, _heavyLeft, _heavyRight),
                _ => throw new ArgumentOutOfRangeException(nameof(strength), strength, null)
            };
        }

        private static T Pick<T>(HitDirection direction, T front, T back, T left, T right)
        {
            return direction switch
            {
                HitDirection.Front => front,
                HitDirection.Back => back,
                HitDirection.Left => left,
                HitDirection.Right => right,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }
    }
}
