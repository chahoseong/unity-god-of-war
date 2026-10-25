using GodOfWar.Characters;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GodOfWar.Tests.Combat
{
    /// <summary>
    /// 액션 데이터가 참조하는 클립·변위가 애니메이터 컨트롤러·원본 클립과 맞는지 검사하는 공용 단언.
    /// </summary>
    public static class AnimationAssetAssertions
    {
        private const string SwordSuffix = "_Sword";

        /// <summary><paramref name="controllerPath"/>의 첫 레이어에서 <paramref name="stateName"/> 상태의 모션을 찾는다. 상태가 없으면 실패한다.</summary>
        public static Motion FindStateMotion(string controllerPath, string stateName)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            AnimatorState state = FindState(controller.layers[0].stateMachine, stateName);
            Assert.That(state, Is.Not.Null, $"'{controllerPath}' has no state named '{stateName}'.");
            return state.motion;
        }

        // Synty는 같은 동작의 루트 모션 판을 A_…_Sword → A_…_RootMotion_Sword로 이름 짓는다.
        public static void AssertBakedFromRootMotionVersion(BakedRootMotion rootMotion, AnimationClip clip)
        {
            Assert.That(rootMotion, Is.Not.Null, $"No root motion is assigned for '{clip.name}'.");
            Assert.That(rootMotion.SourceClip, Is.Not.Null,
                $"'{rootMotion.name}' has no source clip. The Synty assets may be missing.");

            string expected = clip.name.Substring(0, clip.name.Length - SwordSuffix.Length) + "_RootMotion" + SwordSuffix;
            Assert.That(rootMotion.SourceClip.name, Is.EqualTo(expected));
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
    }
}
