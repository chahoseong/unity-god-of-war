using GodOfWar.Combat;
using GodOfWar.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using static GodOfWar.Tests.Combat.AnimationAssetAssertions;

namespace GodOfWar.Tests.Player
{
    /// <summary>
    /// 가드 설정 에셋의 가드 브레이크·패링 반응 연결을 검사한다. 엉뚱한 클립이나 변위를 연결해도 재생은 되어 플레이로는 잘 드러나지 않는다.
    /// 원본 클립은 Synty 에셋이므로 Synty가 없는 환경에서는 실패한다.
    /// </summary>
    public class GuardSettingsAssetTests
    {
        private const string SettingsPath = "Assets/_Project/Data/Player/Settings/Guard.asset";
        private const string ControllerPath = "Assets/_Project/Animations/Player/Player.controller";

        // 반응 클립 길이로 이동 복귀 시점을 정하므로, 애니메이터가 재생하는 클립과 다르면 반응이 끊기거나 멈춰 선다.
        [Test]
        public void GuardBreakClip_ForPlayer_MatchesMotionOfStateWithSameName()
        {
            HitReaction reaction = LoadSettings().GuardBreak;

            Assert.That(FindStateMotion(ControllerPath, reaction.StateName), Is.SameAs(reaction.Clip));
        }

        [Test]
        public void GuardBreakMovement_ForPlayer_IsBakedFromRootMotionVersionOfClip()
        {
            HitReaction reaction = LoadSettings().GuardBreak;

            AssertBakedFromRootMotionVersion(reaction.Movement, reaction.Clip);
        }

        // 패링 클립 길이로 가드·이동 복귀 시점을 정하므로, 애니메이터가 재생하는 클립과 다르면 동작이 끊기거나 멈춰 선다.
        [Test]
        public void ParryClip_ForPlayer_MatchesMotionOfStateWithSameName()
        {
            HitReaction reaction = LoadSettings().Parry;

            Assert.That(FindStateMotion(ControllerPath, reaction.StateName), Is.SameAs(reaction.Clip));
        }

        // 막는 동작 클립 길이 동안 이동을 막으므로, 상체 레이어가 재생하는 클립과 다르면 발이 묶이는 시간과 동작이 어긋난다.
        [Test]
        public void BlockClip_ForPlayer_MatchesMotionOfGuardBlockStateOnGuardLayer()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            AnimatorControllerLayer guardLayer = System.Array.Find(controller.layers, layer => layer.name == "Guard");
            Assert.That(guardLayer, Is.Not.Null, $"'{ControllerPath}' has no Guard layer.");
            ChildAnimatorState block = System.Array.Find(
                guardLayer.stateMachine.states, child => child.state.name == "Guard_Block");
            Assert.That(block.state, Is.Not.Null, "The Guard layer has no Guard_Block state.");

            Assert.That(block.state.motion, Is.SameAs(LoadSettings().BlockClip));
        }

        private static GuardSettings LoadSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GuardSettings>(SettingsPath);
            Assert.That(settings, Is.Not.Null, $"Missing guard settings at '{SettingsPath}'.");
            return settings;
        }
    }
}
