using System;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 타격 세기와 피격 방향마다 재생할 피격 반응. 약한 타격은 약경직, 강한 타격은 강경직으로 반응한다.
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

        public HitReaction Get(HitStrength strength, HitDirection direction)
        {
            return strength switch
            {
                HitStrength.Light => Pick(direction, _lightFront, _lightBack, _lightLeft, _lightRight),
                HitStrength.Heavy => Pick(direction, _heavyFront, _heavyBack, _heavyLeft, _heavyRight),
                _ => throw new ArgumentOutOfRangeException(nameof(strength), strength, null)
            };
        }

        private static HitReaction Pick(
            HitDirection direction, HitReaction front, HitReaction back, HitReaction left, HitReaction right)
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
