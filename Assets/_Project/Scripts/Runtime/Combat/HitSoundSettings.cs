using System;
using GodOfWar.Effects;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 피격자가 받은 타격의 판정·세기·넉다운마다 낼 소리. 맞은 쪽(몸, 갑옷, 무기로 막음 등)에 따라 다르므로 피격자 종류마다 하나씩 둔다.
    /// 클립이 없는 칸은 그 판정에 소리가 없다는 뜻이다. 넉다운 칸은 적중 칸을 대신하지 않고 그 위에 덧붙이는 소리다.
    /// 흘린 타격(<see cref="HitOutcome.Evaded"/>)은 칼이 몸에 닿지 않았으므로 칸이 없다. 그때 들리는 것은 공격하는 쪽의 휘두르는 소리다.
    /// </summary>
    [CreateAssetMenu(menuName = "GodOfWar/Combat/Hit Sound Settings", fileName = "HitSoundSettings")]
    public sealed class HitSoundSettings : ScriptableObject
    {
        [Header("Hit")]
        [Tooltip("막지 못하고 맞은 약한 타격.")]
        [SerializeField] private SoundCue _hitLight = new SoundCue();

        [Tooltip("막지 못하고 맞은 강한 타격. 패링 카운터도 여기에 속한다.")]
        [SerializeField] private SoundCue _hitHeavy = new SoundCue();

        [Tooltip("막지 못하고 맞은 넉다운시키는 타격(콤보 마지막 타 등)에 적중 소리와 함께 덧붙이는 소리. 세기와 관계없이 이 칸을 쓴다.")]
        [SerializeField] private SoundCue _knockdown = new SoundCue();

        [Header("Defense")]
        [Tooltip("가드로 막은 타격.")]
        [SerializeField] private SoundCue _blocked = new SoundCue();

        [Tooltip("가드가 무너진 타격.")]
        [SerializeField] private SoundCue _guardBroken = new SoundCue();

        [Tooltip("저스트 패링으로 받아친 타격.")]
        [SerializeField] private SoundCue _parried = new SoundCue();

        [Header("Playback")]
        [Tooltip("0이면 거리와 방향이 없는 2D, 1이면 타격이 닿은 지점에서 나는 3D 소리.")]
        [SerializeField, Range(0f, 1f)] private float _spatialBlend;

        public float SpatialBlend => _spatialBlend;

        /// <summary>
        /// <paramref name="outcome"/>·<paramref name="strength"/>에 낼 소리. 넉다운 타격도 적중 소리는 이 칸에서 낸다.
        /// 흘린 타격이면 칸이 없어 false다. 칸이 있어도 클립이 비어 있으면 <see cref="SoundCue"/>가 소리를 내지 않는다.
        /// </summary>
        public bool TryGet(HitOutcome outcome, HitStrength strength, out SoundCue cue)
        {
            cue = outcome switch
            {
                HitOutcome.Hit => Pick(strength, _hitLight, _hitHeavy),
                HitOutcome.Evaded => null,
                HitOutcome.Blocked => _blocked,
                HitOutcome.GuardBroken => _guardBroken,
                HitOutcome.Parried => _parried,
                _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
            };
            return cue != null;
        }

        /// <summary>
        /// 적중 소리 위에 덧붙일 넉다운 소리. 넉다운은 막지 못하고 맞았을 때만 의미가 있어,
        /// <paramref name="outcome"/>이 <see cref="HitOutcome.Hit"/>이고 <paramref name="knockdown"/>일 때만 true다.
        /// </summary>
        public bool TryGetKnockdown(HitOutcome outcome, bool knockdown, out SoundCue cue)
        {
            cue = outcome == HitOutcome.Hit && knockdown ? _knockdown : null;
            return cue != null;
        }

        private static SoundCue Pick(HitStrength strength, SoundCue light, SoundCue heavy)
        {
            return strength switch
            {
                HitStrength.Light => light,
                HitStrength.Heavy => heavy,
                _ => throw new ArgumentOutOfRangeException(nameof(strength), strength, null)
            };
        }
    }
}
