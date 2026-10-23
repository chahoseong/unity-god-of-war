using System.Collections.Generic;
using System.IO;
using GodOfWar.Characters;
using GodOfWar.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GodOfWar.Tests.Combat
{
    /// <summary>
    /// 공격 액션 에셋이 두 곳에 나눠 적힌 사실이 서로 맞는지 검사한다.
    /// 어긋나도 공격은 그럴듯하게 재생되어 플레이로는 잘 드러나지 않는다.
    /// 원본 클립은 Synty 에셋이므로 Synty가 없는 환경에서는 실패한다.
    /// </summary>
    public class AttackActionAssetTests
    {
        private const string ControllerPath = "Assets/_Project/Animations/Player/Player.controller";
        private const string SwordSuffix = "_Sword";

        private static IEnumerable<TestCaseData> Actions()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(AttackAction)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                yield return new TestCaseData(path).SetName($"{{m}}({Path.GetFileNameWithoutExtension(path)})");
            }
        }

        // 액션의 클립은 상태 진행 시간의 기준이라, 애니메이터가 실제로 재생하는 클립과 달라지면 전환 시점이 어긋난다.
        [TestCaseSource(nameof(Actions))]
        public void Clip_ForAction_MatchesMotionOfStateWithSameName(string path)
        {
            var action = AssetDatabase.LoadAssetAtPath<AttackAction>(path);

            Assert.That(FindStateMotion(action.StateName), Is.SameAs(action.Clip));
        }

        [TestCaseSource(nameof(Actions))]
        public void ReturnClip_ForAction_MatchesMotionOfStateWithSameName(string path)
        {
            var action = AssetDatabase.LoadAssetAtPath<AttackAction>(path);

            Assert.That(FindStateMotion(action.ReturnStateName), Is.SameAs(action.ReturnClip));
        }

        // 다른 타의 변위를 참조하면 전진 거리가 미묘하게 틀어진다.
        [TestCaseSource(nameof(Actions))]
        public void Movement_ForAction_IsBakedFromRootMotionVersionOfClip(string path)
        {
            var action = AssetDatabase.LoadAssetAtPath<AttackAction>(path);

            AssertBakedFromRootMotionVersion(action.Movement, action.Clip);
        }

        [TestCaseSource(nameof(Actions))]
        public void ReturnMovement_ForAction_IsBakedFromRootMotionVersionOfReturnClip(string path)
        {
            var action = AssetDatabase.LoadAssetAtPath<AttackAction>(path);

            AssertBakedFromRootMotionVersion(action.ReturnMovement, action.ReturnClip);
        }

        // 비워 두면 첫 프레임부터 캔슬이 열리고, 클립보다 길면 캔슬이 조용히 사라진다. 몇 타에만 생기면 플레이로 잘 드러나지 않는다.
        [TestCaseSource(nameof(Actions))]
        public void RecoveryStart_ForAction_IsBetweenTrackingEndAndClipEnd(string path)
        {
            var action = AssetDatabase.LoadAssetAtPath<AttackAction>(path);

            Assert.That(action.RecoveryStart, Is.InRange(action.TrackingEnd, action.Duration));
        }

        private static Motion FindStateMotion(string stateName)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            AnimatorState state = FindState(controller.layers[0].stateMachine, stateName);
            Assert.That(state, Is.Not.Null, $"'{ControllerPath}' has no state named '{stateName}'.");
            return state.motion;
        }

        private static AnimatorState FindState(AnimatorStateMachine machine, string stateName)
        {
            foreach (ChildAnimatorState child in machine.states)
            {
                if (child.state.name == stateName)
                {
                    return child.state;
                }
            }

            foreach (ChildAnimatorStateMachine child in machine.stateMachines)
            {
                AnimatorState state = FindState(child.stateMachine, stateName);
                if (state != null)
                {
                    return state;
                }
            }

            return null;
        }

        // Synty는 같은 동작의 루트 모션 판을 A_…_Sword → A_…_RootMotion_Sword로 이름 짓는다.
        private static void AssertBakedFromRootMotionVersion(BakedRootMotion rootMotion, AnimationClip clip)
        {
            Assert.That(rootMotion, Is.Not.Null, $"No root motion is assigned for '{clip.name}'.");
            Assert.That(rootMotion.SourceClip, Is.Not.Null,
                $"'{rootMotion.name}' has no source clip. The Synty assets may be missing.");

            string expected = clip.name.Substring(0, clip.name.Length - SwordSuffix.Length) + "_RootMotion" + SwordSuffix;
            Assert.That(rootMotion.SourceClip.name, Is.EqualTo(expected));
        }
    }
}
