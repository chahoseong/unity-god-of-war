using System;
using GodOfWar.Combat;
using UnityEngine;

namespace GodOfWar.Player
{
    /// <summary>
    /// 플레이어가 치거나 맞은 타격의 판정·세기마다 낼 피드백. 칸은 지금 나올 수 있는 판정만 둔다.
    /// 판정 규칙상 나올 수 없는 조합을 조회하면 예외를 던져, 규칙이 바뀌었는데 칸이 없는 것을 드러낸다.
    /// </summary>
    [CreateAssetMenu(menuName = "GodOfWar/Player/Hit Feedback Settings", fileName = "HitFeedbackSettings")]
    public sealed class HitFeedbackSettings : ScriptableObject
    {
        [Header("Dealt")]
        [Tooltip("플레이어의 약한 타격이 적에게 맞았을 때.")]
        [SerializeField] private HitFeedback _dealtHitLight = new HitFeedback();

        [Tooltip("플레이어의 강한 타격(패링 카운터 포함)이 적에게 맞았을 때.")]
        [SerializeField] private HitFeedback _dealtHitHeavy = new HitFeedback();

        [Header("Taken")]
        [Tooltip("적의 약한 타격에 맞았을 때.")]
        [SerializeField] private HitFeedback _takenHitLight = new HitFeedback();

        [Tooltip("적의 강한 타격에 맞았을 때.")]
        [SerializeField] private HitFeedback _takenHitHeavy = new HitFeedback();

        [Tooltip("적의 약한 타격을 가드로 막았을 때.")]
        [SerializeField] private HitFeedback _blocked = new HitFeedback();

        [Tooltip("적의 강한 타격에 가드가 무너졌을 때.")]
        [SerializeField] private HitFeedback _guardBroken = new HitFeedback();

        [Tooltip("적의 약한 타격을 저스트 패링했을 때.")]
        [SerializeField] private HitFeedback _parriedLight = new HitFeedback();

        [Tooltip("적의 강한 타격을 저스트 패링했을 때.")]
        [SerializeField] private HitFeedback _parriedHeavy = new HitFeedback();

        /// <summary>
        /// 플레이어가 친 타격의 피드백을 찾는다. 흘린 타격이면 피드백이 없어 false를 반환한다.
        /// 적은 아직 막거나 튕겨 내지 않으므로 그 판정에는 칸이 없다.
        /// </summary>
        public bool TryGetDealt(HitOutcome outcome, HitStrength strength, out HitFeedback feedback)
        {
            switch (outcome)
            {
                case HitOutcome.Evaded:
                    feedback = null;
                    return false;
                case HitOutcome.Hit:
                    feedback = Pick(strength, _dealtHitLight, _dealtHitHeavy);
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(outcome), outcome, "No feedback slot for an enemy defending. Add one for this outcome.");
            }
        }

        /// <summary>
        /// 플레이어가 받은 타격의 피드백을 찾는다. 흘린 타격이면 피드백이 없어 false를 반환한다.
        /// 가드는 약한 타격만 막고 강한 타격에는 무너지므로, 그 반대 조합에는 칸이 없다.
        /// </summary>
        public bool TryGetTaken(HitOutcome outcome, HitStrength strength, out HitFeedback feedback)
        {
            switch (outcome)
            {
                case HitOutcome.Evaded:
                    feedback = null;
                    return false;
                case HitOutcome.Hit:
                    feedback = Pick(strength, _takenHitLight, _takenHitHeavy);
                    return true;
                case HitOutcome.Blocked:
                    feedback = Require(strength, HitStrength.Light, outcome, _blocked);
                    return true;
                case HitOutcome.GuardBroken:
                    feedback = Require(strength, HitStrength.Heavy, outcome, _guardBroken);
                    return true;
                case HitOutcome.Parried:
                    feedback = Pick(strength, _parriedLight, _parriedHeavy);
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null);
            }
        }

        private static HitFeedback Pick(HitStrength strength, HitFeedback light, HitFeedback heavy)
        {
            return strength switch
            {
                HitStrength.Light => light,
                HitStrength.Heavy => heavy,
                _ => throw new ArgumentOutOfRangeException(nameof(strength), strength, null)
            };
        }

        private static HitFeedback Require(
            HitStrength strength, HitStrength expected, HitOutcome outcome, HitFeedback feedback)
        {
            return strength == expected
                ? feedback
                : throw new ArgumentOutOfRangeException(
                    nameof(strength), strength, $"No feedback slot for a {strength} hit judged {outcome}.");
        }
    }
}
