using System.Collections.Generic;
using System.IO;
using GodOfWar.Combat;
using NUnit.Framework;
using UnityEditor;
using static GodOfWar.Tests.Combat.AnimationAssetAssertions;

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

            Assert.That(FindStateMotion(ControllerPath, action.StateName), Is.SameAs(action.Clip));
        }

        [TestCaseSource(nameof(Actions))]
        public void ReturnClip_ForAction_MatchesMotionOfStateWithSameName(string path)
        {
            var action = AssetDatabase.LoadAssetAtPath<AttackAction>(path);

            Assert.That(FindStateMotion(ControllerPath, action.ReturnStateName), Is.SameAs(action.ReturnClip));
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

        // 조준이 끝나기 전에 열리면 몸을 돌리는 동안 옆 대상을 벤다. 구간이 뒤집히면 판정이 조용히 사라진다.
        [TestCaseSource(nameof(Actions))]
        public void HitWindowStart_ForAction_IsBetweenTrackingEndAndHitWindowEnd(string path)
        {
            var action = AssetDatabase.LoadAssetAtPath<AttackAction>(path);

            Assert.That(action.HitWindowStart, Is.InRange(action.TrackingEnd, action.HitWindowEnd));
        }

        // 후딜레이 캔슬이 열린 뒤에도 판정이 남으면 회피로 캔슬하는 동안 칼이 대상을 벤다.
        // 시작과 끝이 같으면 판정이 한 번도 열리지 않으므로 시작보다 커야 한다.
        [TestCaseSource(nameof(Actions))]
        public void HitWindowEnd_ForAction_IsAfterHitWindowStartAndAtOrBeforeRecoveryStart(string path)
        {
            var action = AssetDatabase.LoadAssetAtPath<AttackAction>(path);

            Assert.That(action.HitWindowEnd,
                Is.GreaterThan(action.HitWindowStart).And.AtMost(action.RecoveryStart));
        }
    }
}
