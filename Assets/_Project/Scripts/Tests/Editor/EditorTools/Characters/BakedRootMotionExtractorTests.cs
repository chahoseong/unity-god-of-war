using GodOfWar.EditorTools.Characters;
using NUnit.Framework;
using UnityEngine;

namespace GodOfWar.Tests.EditorTools.Characters
{
    public class BakedRootMotionExtractorTests
    {
        private const float FrameRate = 30f;
        private const float Tolerance = 1e-5f;

        // +Z로 2 이동하면서 중간에 옆(X)으로 흔들린다.
        // 실제 클립의 RootT처럼 원점이 아닌 곳에서 시작해, 시작점 기준 변위를 빠뜨리면 커브가 0에서 시작하지 않는다.
        private static readonly Vector2[] WobblingForward =
        {
            new Vector2(0.5f, 0.3f), new Vector2(1f, 0.9f), new Vector2(0.2f, 1.7f), new Vector2(0.5f, 2.3f)
        };

        [Test]
        public void Extract_WithOffsetStart_StartsCurvesAtOrigin()
        {
            var result = BakedRootMotionExtractor.Extract(WobblingForward, FrameRate, 1f);

            var start = new Vector2(result.X.Evaluate(0f), result.Z.Evaluate(0f));
            Assert.That(start.magnitude, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Extract_WithWobblingPath_KeepsSidewaysDisplacementInX()
        {
            var result = BakedRootMotionExtractor.Extract(WobblingForward, FrameRate, 1f);

            // 두 번째 샘플은 시작점보다 옆으로 0.5 떨어져 있다.
            Assert.That(result.X.Evaluate(1f / FrameRate), Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void Extract_WithHumanScale_ScalesDisplacementByHumanScale()
        {
            Vector2[] straight = { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 2f) };

            var result = BakedRootMotionExtractor.Extract(straight, FrameRate, 0.5f);

            Assert.That(result.Z.Evaluate(2f / FrameRate), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Extract_WithFourSamples_ReturnsDurationOfThreeFrames()
        {
            var result = BakedRootMotionExtractor.Extract(WobblingForward, FrameRate, 1f);

            Assert.That(result.Duration, Is.EqualTo(3f / FrameRate).Within(Tolerance));
        }
    }
}
