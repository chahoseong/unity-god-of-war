using System.Collections.Generic;
using GodOfWar.Characters;
using GodOfWar.Combat;
using NUnit.Framework;
using UnityEditor;

namespace GodOfWar.Tests.Combat
{
    /// <summary>
    /// 회피 액션 에셋의 방향 슬롯이 같은 방향의 클립에서 구운 변위를 가리키는지 검사한다.
    /// 슬롯이 어긋나도 이동 방향은 입력으로 정해져 플레이로는 잘 드러나지 않는다.
    /// 원본 클립은 Synty 에셋이므로 Synty가 없는 환경에서는 실패한다.
    /// </summary>
    public class DodgeActionAssetTests
    {
        private static IEnumerable<TestCaseData> Slots()
        {
            var tags = new Dictionary<DodgeCardinal, string>
            {
                { DodgeCardinal.Forward, "_F_" },
                { DodgeCardinal.Backward, "_B_" },
                { DodgeCardinal.Left, "_L_" },
                { DodgeCardinal.Right, "_R_" }
            };

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(DodgeAction)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (KeyValuePair<DodgeCardinal, string> tag in tags)
                {
                    yield return new TestCaseData(path, tag.Key, tag.Value)
                        .SetName($"{{m}}({System.IO.Path.GetFileNameWithoutExtension(path)}, {tag.Key})");
                }
            }
        }

        [TestCaseSource(nameof(Slots))]
        public void GetRootMotion_ForCardinal_ReturnsRootMotionFromClipOfSameCardinal(
            string path, DodgeCardinal cardinal, string clipTag)
        {
            var action = AssetDatabase.LoadAssetAtPath<DodgeAction>(path);

            BakedRootMotion rootMotion = action.GetRootMotion(cardinal);

            Assert.That(rootMotion, Is.Not.Null, $"'{action.name}' has no root motion in the {cardinal} slot.");
            Assert.That(rootMotion.SourceClip, Is.Not.Null,
                $"'{rootMotion.name}' has no source clip. The Synty assets may be missing.");
            Assert.That(rootMotion.SourceClip.name, Does.Contain(clipTag));
        }

        private static IEnumerable<TestCaseData> Actions()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(DodgeAction)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                yield return new TestCaseData(path).SetName($"{{m}}({System.IO.Path.GetFileNameWithoutExtension(path)})");
            }
        }

        // 시작이 끝보다 늦으면 회피가 한 번도 무적이 되지 않는데, 플레이로는 타이밍을 못 맞춘 것처럼 보인다.
        // 가장 짧은 방향의 클립이 끝나기 전에 무적도 끝나야 어느 방향이든 같은 구간이 적용된다.
        [TestCaseSource(nameof(Actions))]
        public void InvulnerableEnd_ForAction_IsAfterInvulnerableStartAndAtOrBeforeShortestClipEnd(string path)
        {
            var action = AssetDatabase.LoadAssetAtPath<DodgeAction>(path);
            float shortest = float.MaxValue;
            foreach (DodgeCardinal cardinal in new[]
                         { DodgeCardinal.Forward, DodgeCardinal.Backward, DodgeCardinal.Left, DodgeCardinal.Right })
            {
                shortest = System.Math.Min(shortest, action.GetRootMotion(cardinal).Duration);
            }

            Assert.That(action.InvulnerableEnd,
                Is.GreaterThan(action.InvulnerableStart).And.LessThanOrEqualTo(shortest));
        }
    }
}
