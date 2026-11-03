using GodOfWar.Combat;
using NUnit.Framework;
using UnityEngine;

namespace GodOfWar.Tests.Combat
{
    // 몸은 캐릭터와 같은 캡슐(높이 1.8, 반지름 0.3, 중심 0.9)로 두고, 원점에서 떨어뜨려 월드 원점에 기대지 않는지도 함께 본다.
    public class ImpactPointTests
    {
        private const float Height = 1.8f;
        private const float Radius = 0.3f;
        private const float Tolerance = 1e-4f;

        private static readonly Vector3 BodyPosition = new Vector3(10f, 0f, 5f);

        private GameObject _body;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_body);
        }

        // 겹침을 검출한 순간에는 칼날이 이미 몸 안에 들어가 있으므로, 안에 있는 칼날에서도 표면 지점이 나와야 한다.
        [Test]
        public void OnSurface_WithBladeInsideCapsule_ReturnsPointOnSurfaceTowardBlade()
        {
            CharacterController body = CreateController(Vector3.one);
            Vector3 start = BodyPosition + new Vector3(-1f, 1.2f, 0.1f);
            Vector3 end = BodyPosition + new Vector3(0.1f, 1.2f, 0.1f);

            ImpactPoint impact = ImpactPoint.OnSurface(body, start, end, Attacker());

            AssertNear(impact.Normal, Vector3.forward);
            AssertNear(impact.Position, BodyPosition + new Vector3(0f, 1.2f, Radius));
        }

        // 칼날이 축을 지나면 몸 바깥 방향을 정할 수 없으므로 공격자 쪽 수평 방향을 쓴다.
        [Test]
        public void OnSurface_WithBladeThroughAxis_ReturnsHorizontalNormalTowardAttacker()
        {
            CharacterController body = CreateController(Vector3.one);
            Vector3 start = BodyPosition + new Vector3(-1f, 1f, 0f);
            Vector3 end = BodyPosition + new Vector3(1f, 1f, 0f);
            Vector3 attacker = BodyPosition + new Vector3(0f, 3f, -2f);

            ImpactPoint impact = ImpactPoint.OnSurface(body, start, end, attacker);

            AssertNear(impact.Normal, Vector3.back);
            AssertNear(impact.Position, BodyPosition + new Vector3(0f, 1f, -Radius));
        }

        // 축 끝보다 위에서 닿으면 몸 위쪽 반구에 닿으므로, 법선이 수평이 아니라 위를 향해야 한다.
        [Test]
        public void OnSurface_WithPointAboveCapsule_ReturnsPointOnTopHemisphere()
        {
            CharacterController body = CreateController(Vector3.one);
            Vector3 point = BodyPosition + Vector3.up * 3f;

            ImpactPoint impact = ImpactPoint.OnSurface(body, point, point, Attacker());

            AssertNear(impact.Normal, Vector3.up);
            AssertNear(impact.Position, BodyPosition + Vector3.up * Height);
        }

        // 물리 엔진 내부 캡슐은 설정보다 작으므로, 반지름은 설정값에 스케일을 곱한 값(보이는 몸)이어야 한다.
        [Test]
        public void OnSurface_WithScaledController_ReturnsPointAtConfiguredRadiusTimesScale()
        {
            CharacterController body = CreateController(Vector3.one * 2f);
            Vector3 point = BodyPosition + new Vector3(3f, 2f, 0f);

            ImpactPoint impact = ImpactPoint.OnSurface(body, point, point, Attacker());

            AssertNear(impact.Position, BodyPosition + new Vector3(Radius * 2f, 2f, 0f));
        }

        // 캡슐 콜라이더는 축 방향과 회전을 따라야 한다. 앞쪽(Z) 축 캡슐을 Y로 90° 돌려 축이 월드 X가 되게 한다.
        [Test]
        public void OnSurface_WithRotatedCapsuleCollider_FollowsColliderAxis()
        {
            _body = new GameObject("Body");
            _body.transform.SetPositionAndRotation(BodyPosition, Quaternion.Euler(0f, 90f, 0f));
            var capsule = _body.AddComponent<CapsuleCollider>();
            capsule.direction = 2;
            capsule.height = 2f;
            capsule.radius = 0.25f;
            Vector3 point = BodyPosition + new Vector3(0.5f, 1f, 0f);

            ImpactPoint impact = ImpactPoint.OnSurface(capsule, point, point, Attacker());

            AssertNear(impact.Normal, Vector3.up);
            AssertNear(impact.Position, BodyPosition + new Vector3(0.5f, 0.25f, 0f));
        }

        // 캡슐·구가 아닌 콜라이더는 Unity의 최근접점으로 대신한다.
        [Test]
        public void OnSurface_WithNonCapsuleCollider_ReturnsClosestPoint()
        {
            _body = new GameObject("Body");
            _body.transform.position = BodyPosition;
            var box = _body.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            Vector3 point = BodyPosition + new Vector3(2f, 0.2f, 0f);

            ImpactPoint impact = ImpactPoint.OnSurface(box, point, point, Attacker());

            AssertNear(impact.Position, BodyPosition + new Vector3(0.5f, 0.2f, 0f));
        }

        private CharacterController CreateController(Vector3 scale)
        {
            _body = new GameObject("Body");
            _body.transform.position = BodyPosition;
            _body.transform.localScale = scale;
            var controller = _body.AddComponent<CharacterController>();
            controller.height = Height;
            controller.radius = Radius;
            controller.center = Vector3.up * (Height * 0.5f);
            return controller;
        }

        private static Vector3 Attacker()
        {
            return BodyPosition + Vector3.forward * 2f;
        }

        private static void AssertNear(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(Tolerance), $"expected {expected:F4}, actual {actual:F4}");
        }
    }
}
