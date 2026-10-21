using GodOfWar.EditorTools.Characters;
using NUnit.Framework;
using UnityEngine;

namespace GodOfWar.Tests.EditorTools.Characters
{
    public class BakedRootMotionExtractorTests
    {
        private const float FrameRate = 30f;
        private const float Tolerance = 1e-5f;

        // 순 이동은 +Z로 2, 중간에 옆(X)으로 흔들린다. 경로 길이가 아니라 순 이동에 투영해야 한다.
        // 실제 클립의 RootT처럼 원점이 아닌 곳에서 시작해, 시작점 기준 변위를 빠뜨리면 커브 시작·끝이 어긋난다.
        private static readonly Vector2[] WobblingForward =
        {
            new Vector2(0.5f, 0.3f), new Vector2(1f, 0.9f), new Vector2(0.2f, 1.7f), new Vector2(0.5f, 2.3f)
        };

        [Test]
        public void Extract_WithNetDisplacement_StartsCurveAtZero()
        {
            var result = BakedRootMotionExtractor.Extract(WobblingForward, FrameRate, 1f);

            Assert.That(result.Curve.Evaluate(0f), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Extract_WithNetDisplacement_EndsCurveAtOne()
        {
            var result = BakedRootMotionExtractor.Extract(WobblingForward, FrameRate, 1f);

            Assert.That(result.Curve.Evaluate(1f), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Extract_WithWobblingPath_ReturnsNetDisplacementAsDistance()
        {
            var result = BakedRootMotionExtractor.Extract(WobblingForward, FrameRate, 1f);

            Assert.That(result.Distance, Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void Extract_WithHumanScale_ScalesDistanceByHumanScale()
        {
            // 직선 경로라 투영이 결과에 끼어들지 않는다.
            Vector2[] straight = { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 2f) };

            var result = BakedRootMotionExtractor.Extract(straight, FrameRate, 0.5f);

            Assert.That(result.Distance, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Extract_WithFourSamples_ReturnsDurationOfThreeFrames()
        {
            var result = BakedRootMotionExtractor.Extract(WobblingForward, FrameRate, 1f);

            Assert.That(result.Duration, Is.EqualTo(3f / FrameRate).Within(Tolerance));
        }

        [Test]
        public void Extract_WithSamplePastEnd_PreservesOvershootInCurve()
        {
            Vector2[] overshoot = { new Vector2(0f, 0f), new Vector2(0f, 1.2f), new Vector2(0f, 1f) };

            var result = BakedRootMotionExtractor.Extract(overshoot, FrameRate, 1f);

            Assert.That(result.Curve.Evaluate(0.5f), Is.EqualTo(1.2f).Within(Tolerance));
        }

        [Test]
        public void Extract_WithoutNetDisplacement_ReturnsZeroDistance()
        {
            Vector2[] returning = { new Vector2(0f, 0f), new Vector2(0.4f, 0.3f), new Vector2(0f, 0f) };

            var result = BakedRootMotionExtractor.Extract(returning, FrameRate, 1f);

            Assert.That(result.Distance, Is.EqualTo(0f));
        }
    }
}
