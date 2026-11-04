using System.Collections.Generic;
using GodOfWar.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
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

        // 이 반응 세트를 쓰는 캐릭터의 애니메이터 컨트롤러.
        private static readonly (string label, string path)[] Controllers =
        {
            ("TrainingDummy", "Assets/_Project/Animations/Enemies/TrainingDummy.controller"),
            ("Player", "Assets/_Project/Animations/Player/Player.controller")
        };

        // 넉다운 반응은 쓰러지는 캐릭터의 애니메이터에만 있다.
        private const string KnockdownControllerPath = "Assets/_Project/Animations/Enemies/TrainingDummy.controller";

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

        private static IEnumerable<TestCaseData> ReactionsOnControllers()
        {
            foreach ((string label, string path) in Controllers)
            {
                foreach (HitStrength strength in new[] { HitStrength.Light, HitStrength.Heavy })
                {
                    foreach (HitDirection direction in Directions)
                    {
                        yield return new TestCaseData(path, strength, direction)
                            .SetName($"{{m}}({label}{strength}{direction})");
                    }
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
        [TestCaseSource(nameof(ReactionsOnControllers))]
        public void Clip_ForReaction_MatchesMotionOfStateWithSameName(
            string controllerPath, HitStrength strength, HitDirection direction)
        {
            HitReaction reaction = LoadSet().Get(strength, direction);

            Assert.That(FindStateMotion(controllerPath, reaction.StateName), Is.SameAs(reaction.Clip));
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

        // 쓰러지는·일어나는 구간의 길이를 클립 길이로 정하므로, 애니메이터가 재생하는 클립과 다르면 동작이 끊기거나 멈춰 선다.
        [Test]
        public void KnockdownClips_ForKnockdownReaction_MatchMotionsOfStatesWithSameNames()
        {
            KnockdownReaction knockdown = LoadSet().Knockdown;

            Assert.That(FindStateMotion(KnockdownControllerPath, knockdown.BeginStateName), Is.SameAs(knockdown.BeginClip));
            Assert.That(FindStateMotion(KnockdownControllerPath, knockdown.EndStateName), Is.SameAs(knockdown.EndClip));
        }

        // 누워 있는 시간은 클립 길이와 관계없이 정하므로, 반복하지 않는 클립이면 그 시간 동안 마지막 자세로 멈춰 있다.
        [Test]
        public void KnockdownLoopState_ForKnockdownReaction_PlaysLoopingClip()
        {
            KnockdownReaction knockdown = LoadSet().Knockdown;

            var motion = FindStateMotion(KnockdownControllerPath, knockdown.LoopStateName) as AnimationClip;
            Assert.That(motion, Is.Not.Null);
            Assert.That(motion.isLooping, Is.True);
        }

        // 쓰러짐이 끝날 때까지 이 시각에 닿지 못하면 지면 충돌을 알리지 않아, 흔들림이 조용히 사라진다.
        [Test]
        public void GroundImpactTime_ForKnockdownReaction_IsWithinBeginClip()
        {
            KnockdownReaction knockdown = LoadSet().Knockdown;

            Assert.That(knockdown.GroundImpactTime, Is.LessThan(knockdown.BeginDuration));
        }

        [Test]
        public void KnockdownMovement_ForKnockdownReaction_IsBakedFromRootMotionVersionOfBeginClip()
        {
            KnockdownReaction knockdown = LoadSet().Knockdown;

            AssertBakedFromRootMotionVersion(knockdown.BeginMovement, knockdown.BeginClip);
        }

        private static HitReactionSet LoadSet()
        {
            var set = AssetDatabase.LoadAssetAtPath<HitReactionSet>(SetPath);
            Assert.That(set, Is.Not.Null, $"Missing hit reaction set at '{SetPath}'.");
            return set;
        }
    }
}
