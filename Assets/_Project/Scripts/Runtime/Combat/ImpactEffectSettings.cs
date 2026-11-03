using System;
using System.Collections.Generic;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 피격자가 받은 타격의 판정·세기마다 접점에 띄울 이펙트. 맞은 쪽의 재질(몸, 갑옷 등)에 따라 다르므로 피격자 종류마다 하나씩 둔다.
    /// 빈 칸은 그 판정에 이펙트가 없다는 뜻이다. 흘린 타격(<see cref="HitOutcome.Evaded"/>)에는 칸이 없다.
    /// 프리팹의 반복 재생·자동 재생·정지 동작은 풀이 덮어쓰므로 신경 쓰지 않아도 된다. 프리팹의 +Z가 몸 바깥(법선)을 향한다.
    /// </summary>
    [CreateAssetMenu(menuName = "GodOfWar/Combat/Impact Effect Settings", fileName = "ImpactEffectSettings")]
    public sealed class ImpactEffectSettings : ScriptableObject
    {
        [Header("Hit")]
        [Tooltip("막지 못하고 맞은 약한 타격.")]
        [SerializeField] private ParticleSystem _hitLight;

        [Tooltip("막지 못하고 맞은 강한 타격. 패링 카운터도 여기에 속한다.")]
        [SerializeField] private ParticleSystem _hitHeavy;

        [Header("Defense")]
        [Tooltip("가드로 막은 타격.")]
        [SerializeField] private ParticleSystem _blocked;

        [Tooltip("가드가 무너진 타격.")]
        [SerializeField] private ParticleSystem _guardBroken;

        [Tooltip("저스트 패링으로 받아친 타격.")]
        [SerializeField] private ParticleSystem _parried;

        /// <summary>
        /// <paramref name="outcome"/>·<paramref name="strength"/>에 띄울 이펙트 프리팹. 흘린 타격이거나 칸이 비어 있으면 false다.
        /// </summary>
        public bool TryGet(HitOutcome outcome, HitStrength strength, out ParticleSystem prefab)
        {
            prefab = outcome switch
            {
                HitOutcome.Hit => strength switch
                {
                    HitStrength.Light => _hitLight,
                    HitStrength.Heavy => _hitHeavy,
                    _ => throw new ArgumentOutOfRangeException(nameof(strength), strength, null)
                },
                HitOutcome.Evaded => null,
                HitOutcome.Blocked => _blocked,
                HitOutcome.GuardBroken => _guardBroken,
                HitOutcome.Parried => _parried,
                _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
            };
            return prefab != null;
        }

        /// <summary>채워진 칸의 프리팹을 모두 <paramref name="prefabs"/>에 더한다. 같은 프리팹이 여러 칸에 있으면 한 번만 더한다.</summary>
        public void CollectPrefabs(ICollection<ParticleSystem> prefabs)
        {
            foreach (ParticleSystem prefab in new[] { _hitLight, _hitHeavy, _blocked, _guardBroken, _parried })
            {
                if (prefab != null && !prefabs.Contains(prefab))
                {
                    prefabs.Add(prefab);
                }
            }
        }
    }
}
