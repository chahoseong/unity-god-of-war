using GodOfWar.Combat;
using NUnit.Framework;
using UnityEngine;

namespace GodOfWar.Tests.Combat
{
    // 무기는 손에 쥐듯 축 오브젝트의 자식으로 두고, 축을 Y축으로 돌려 휘두른다.
    // 원점에서 떨어뜨려 오프셋 계산이 월드 원점에 기대지 않는지도 함께 확인한다.
    public class SwingTrackerTests
    {
        private const float BaseDistance = 0.1f;
        private const float TipDistance = 1f;
        private const float Tolerance = 1e-4f;

        private Transform _pivot;
        private Transform _base;
        private Transform _tip;
        private SwingTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _pivot = new GameObject("Pivot").transform;
            _pivot.position = new Vector3(10f, 0f, 5f);
            _base = CreatePoint("Base", BaseDistance);
            _tip = CreatePoint("Tip", TipDistance);
            _tracker = new SwingTracker(_pivot, _base, _tip);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_pivot.gameObject);
        }

        // 궤적은 시작하자마자 첫 단면을 이 값으로 붙인다.
        [Test]
        public void GetSegment_AfterBegin_ReturnsCurrentEnds()
        {
            _tracker.Begin();

            _tracker.GetSegment(1f, out Vector3 start, out Vector3 end);

            AssertNear(start, _base.position);
            AssertNear(end, _tip.position);
        }

        // 양 끝을 직선으로 보간했다면 중간 시점의 선분이 짧아지고 축에 가까워진다.
        [Test]
        public void GetSegment_WhilePivotRotates_FollowsArc()
        {
            _tracker.Begin();
            _pivot.Rotate(0f, 120f, 0f);
            _tracker.Advance(0.05f, 64);

            _tracker.GetSegment(0.5f, out Vector3 start, out Vector3 end);

            Assert.That(Vector3.Distance(start, end), Is.EqualTo(TipDistance - BaseDistance).Within(Tolerance));
            Assert.That(Vector3.Distance(_pivot.position, end), Is.EqualTo(TipDistance).Within(Tolerance));
        }

        [Test]
        public void GetSegment_AtEndOfAdvance_ReturnsCurrentEnds()
        {
            _tracker.Begin();
            _pivot.Rotate(0f, 90f, 0f);
            _pivot.position += Vector3.forward;
            _tracker.Advance(0.05f, 64);

            _tracker.GetSegment(1f, out Vector3 start, out Vector3 end);

            AssertNear(start, _base.position);
            AssertNear(end, _tip.position);
        }

        // 90° 돌면 칼끝은 √2(≈1.414) m, 시작점은 그 1/10을 움직인다. 칼끝 기준 0.1 m 간격이면 15단계다.
        [Test]
        public void Advance_WithEndsMovingDifferently_SplitsByFartherTravel()
        {
            _tracker.Begin();
            _pivot.Rotate(0f, 90f, 0f);

            Assert.That(_tracker.Advance(0.1f, 64), Is.EqualTo(15));
        }

        [Test]
        public void Advance_WithTravelOverMaxSteps_ReturnsMaxSteps()
        {
            _tracker.Begin();
            _pivot.Rotate(0f, 90f, 0f);

            Assert.That(_tracker.Advance(0.01f, 16), Is.EqualTo(16));
        }

        [Test]
        public void Advance_WithoutMovement_ReturnsOneStep()
        {
            _tracker.Begin();

            Assert.That(_tracker.Advance(0.1f, 64), Is.EqualTo(1));
        }

        // 다음 프레임은 지난번 Advance의 포즈부터 나눠야, 이미 지나간 구간을 다시 내놓지 않는다.
        [Test]
        public void Advance_AfterPreviousAdvance_StartsFromLastPose()
        {
            _tracker.Begin();
            _pivot.Rotate(0f, 90f, 0f);
            _tracker.Advance(0.1f, 64);

            int steps = _tracker.Advance(0.1f, 64);
            _tracker.GetSegment(0f, out Vector3 start, out Vector3 end);

            Assert.That(steps, Is.EqualTo(1));
            AssertNear(start, _base.position);
            AssertNear(end, _tip.position);
        }

        private Transform CreatePoint(string pointName, float distance)
        {
            var point = new GameObject(pointName).transform;
            point.SetParent(_pivot, false);
            point.localPosition = Vector3.forward * distance;
            return point;
        }

        private static void AssertNear(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(Tolerance));
        }
    }
}
