using System.Collections.Generic;
using GodOfWar.Combat;
using NUnit.Framework;
using UnityEditor;
using static GodOfWar.Tests.Combat.AnimationAssetAssertions;

namespace GodOfWar.Tests.Combat
{
    /// <summary>
    /// 검 피격 반응 에셋의 연결을 검사한다. 칸을 잘못 연결해도 그럴듯한 반응이 재생되어 플레이로는 잘 드러나지 않는다.
    /// 원본 클립은 Synty 에셋이므로 Synty가 없는 환경에서는 실패한다.
    /// </summary>
    public class HitReactionSetAssetTests
    {
        private const string SetPath = "Assets/_Project/Data/Combat/SwordHitReactions.asset";
        private const string ControllerPath = "Assets/_Project/Animations/Enemies/TrainingDummy.controller";

        private static readonly HitDirection[] Directions =
            { HitDirection.Front, HitDirection.Back, HitDirection.Left, HitDirection.Right };

        private static IEnumerable<TestCaseData> Reactions()
        {
            foreach (HitStrength strength in new[] { HitStrength.Light, HitStrength.Heavy })
            {
                foreach (HitDirection direction in Directions)
                {
                    yield return new TestCaseData(strength, direction).SetName($"{{m}}({strength}{direction})");
                }
            }
        }

        private static IEnumerable<TestCaseData> HeavyReactions()
        {
            foreach (HitDirection direction in Directions)
            {
                yield return new TestCaseData(HitStrength.Heavy, direction).SetName($"{{m}}({direction})");
            }
        }

        // 반응 클립 길이로 대기 복귀 시점을 정하므로, 애니메이터가 재생하는 클립과 다르면 반응이 끊기거나 멈춰 선다.
        [TestCaseSource(nameof(Reactions))]
        public void Clip_ForReaction_MatchesMotionOfStateWithSameName(HitStrength strength, HitDirection direction)
        {
            HitReaction reaction = LoadSet().Get(strength, direction);

            Assert.That(FindStateMotion(ControllerPath, reaction.StateName), Is.SameAs(reaction.Clip));
        }

        // 다른 방향의 변위를 연결하면 반응 동작과 밀려나는 방향이 어긋난다.
        [TestCaseSource(nameof(HeavyReactions))]
        public void Movement_ForStaggerReaction_IsBakedFromRootMotionVersionOfClip(
            HitStrength strength, HitDirection direction)
        {
            HitReaction reaction = LoadSet().Get(strength, direction);

            AssertBakedFromRootMotionVersion(reaction.Movement, reaction.Clip);
        }

        // Synty 피격 클립의 방향 글자는 맞은 쪽이 아니라 반응해서 움직이는 쪽이다
        // (예: A_Hit_F_Stagger는 앞으로 0.75m 밀린다). 앞에서 맞으면 뒤로 밀리는 B 클립이어야 한다.
        // 칸 연결과 Get의 방향 매핑이 함께 뒤바뀌는 실수를 잡는다.
        [TestCaseSource(nameof(Reactions))]
        public void Clip_ForHitDirection_HasOppositeDirectionLetter(HitStrength strength, HitDirection direction)
        {
            HitReaction reaction = LoadSet().Get(strength, direction);

            string expectedLetter = direction switch
            {
                HitDirection.Front => "B",
                HitDirection.Back => "F",
                HitDirection.Left => "R",
                HitDirection.Right => "L",
                _ => throw new System.ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
            Assert.That(reaction.Clip.name, Does.StartWith($"A_Hit_{expectedLetter}_"));
        }

        private static HitReactionSet LoadSet()
        {
            var set = AssetDatabase.LoadAssetAtPath<HitReactionSet>(SetPath);
            Assert.That(set, Is.Not.Null, $"Missing hit reaction set at '{SetPath}'.");
            return set;
        }
    }
}
