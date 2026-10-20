using System.Collections.Generic;
using GodOfWar.Player;
using GodOfWar.Player.States;
using NUnit.Framework;
using UnityEngine;

namespace GodOfWar.Tests.Player.States
{
    public class DodgeMotionTests
    {
        private const float Distance = 2f;
        private const float Duration = 0.5f;
        private const float MoveYaw = 90f;
        private const float Tolerance = 1e-4f;

        private DodgeProfile _profile;
        private DodgeMotion _motion;

        [SetUp]
        public void SetUp()
        {
            // 선형 커브는 고정 속도(거리/시간)로도 통과하므로, 커브 차분을 검증하려고 비선형 커브를 쓴다.
            _profile = new DodgeProfile(Distance, Duration, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
            _motion = new DodgeMotion();
            _motion.Begin(_profile, MoveYaw);
        }

        private static IEnumerable<TestCaseData> DeltaTimeSequences()
        {
            yield return new TestCaseData((object)Repeat(1f / 60f, 30)).SetName("{m}(Fixed60Fps)");
            yield return new TestCaseData((object)new[] { 0.016f, 0.05f, 0.003f, 0.1f, 0.033f, 0.2f, 0.098f })
                .SetName("{m}(Irregular)");
            // 마지막 프레임이 지속 시간을 넘는다 (0.2 × 3 = 0.6 > 0.5).
            yield return new TestCaseData((object)Repeat(0.2f, 3)).SetName("{m}(OvershootingLastFrame)");
        }

        [TestCaseSource(nameof(DeltaTimeSequences))]
        public void Tick_WithVaryingDeltaTimes_AccumulatesProfileDistance(float[] deltaTimes)
        {
            Vector3 displacement = Vector3.zero;
            foreach (float deltaTime in deltaTimes)
            {
                displacement += _motion.Tick(deltaTime) * deltaTime;
            }

            // MoveYaw 90°는 월드 +X 방향이다.
            Vector3 expected = Vector3.right * Distance;
            Assert.That(Vector3.Distance(displacement, expected), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Tick_BeforeDuration_LeavesMotionUnfinished()
        {
            _motion.Tick(Duration - 0.01f);

            Assert.That(_motion.IsFinished, Is.False);
        }

        [Test]
        public void Tick_ToDuration_FinishesMotion()
        {
            _motion.Tick(Duration);

            Assert.That(_motion.IsFinished, Is.True);
        }

        private static float[] Repeat(float value, int count)
        {
            var values = new float[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = value;
            }

            return values;
        }
    }
}
