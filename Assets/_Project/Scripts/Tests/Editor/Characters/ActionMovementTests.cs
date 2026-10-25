using System.Collections.Generic;
using GodOfWar.Characters;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GodOfWar.Tests.Characters
{
    public class ActionMovementTests
    {
        // 구르기 궤적은 비선형이라, 고정 속도(거리/시간)로는 통과할 수 없어 궤적 차분을 검증할 수 있다.
        private const string RootMotionPath = "Assets/_Project/Data/RootMotion/DodgeRoll_F.asset";
        private const float Yaw = 90f;
        private const float Tolerance = 1e-4f;

        private BakedRootMotion _rootMotion;
        private ActionMovement _movement;

        [SetUp]
        public void SetUp()
        {
            _rootMotion = AssetDatabase.LoadAssetAtPath<BakedRootMotion>(RootMotionPath);
            Assert.That(_rootMotion, Is.Not.Null, $"Missing baked root motion at '{RootMotionPath}'.");
            _movement = new ActionMovement();
            _movement.Begin(_rootMotion, Yaw);
        }

        private static IEnumerable<TestCaseData> DeltaTimePatterns()
        {
            yield return new TestCaseData(new[] { 1f / 60f }).SetName("{m}(Fixed60Fps)");
            yield return new TestCaseData(new[] { 0.016f, 0.05f, 0.003f, 0.1f, 0.033f, 0.2f, 0.098f })
                .SetName("{m}(Irregular)");
            // 큰 프레임이 반복되어 마지막 프레임이 지속 시간을 넘는다.
            yield return new TestCaseData(new[] { 0.4f }).SetName("{m}(OvershootingLastFrame)");
        }

        [TestCaseSource(nameof(DeltaTimePatterns))]
        public void Tick_AcrossDeltaTimePatterns_AccumulatesRootMotionDisplacement(float[] pattern)
        {
            Vector3 displacement = Vector3.zero;
            for (int i = 0; !_movement.IsFinished; i++)
            {
                float deltaTime = pattern[i % pattern.Length];
                displacement += _movement.Tick(deltaTime) * deltaTime;
            }

            // 궤적 끝을 Yaw 90°(로컬 정면 → 월드 +X)로 돌린 위치에 도착해야 한다.
            Vector3 expected = Quaternion.Euler(0f, Yaw, 0f) * _rootMotion.Sample(_rootMotion.Duration);
            Assert.That(Vector3.Distance(displacement, expected), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Tick_BeforeDuration_LeavesMovementUnfinished()
        {
            _movement.Tick(_rootMotion.Duration - 0.01f);

            Assert.That(_movement.IsFinished, Is.False);
        }

        [Test]
        public void Tick_ToDuration_FinishesMovement()
        {
            _movement.Tick(_rootMotion.Duration);

            Assert.That(_movement.IsFinished, Is.True);
        }
    }
}
