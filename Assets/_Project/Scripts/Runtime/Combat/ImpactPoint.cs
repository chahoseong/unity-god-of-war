using System;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 타격이 피격자의 몸에 닿은 지점. 위치는 몸 콜라이더의 표면 위에 있고, 법선은 몸 바깥을 향한다.
    /// 공격하는 쪽이 타격을 기록하는 순간 <see cref="OnSurface"/>로 구해 <see cref="AttackInfo"/>에 담는다.
    /// </summary>
    public readonly struct ImpactPoint
    {
        // 이보다 가까우면 기준이 몸의 축 위에 있어 표면 방향을 정할 수 없는 것으로 본다.
        private const float MinDirectionSqr = 1e-8f;

        public ImpactPoint(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
        }

        /// <summary>몸 표면 위의 위치(월드).</summary>
        public Vector3 Position { get; }

        /// <summary>몸 바깥을 향하는 단위 벡터(월드).</summary>
        public Vector3 Normal { get; }

        /// <summary>
        /// 기준 선분 <paramref name="start"/>~<paramref name="end"/>에 가장 가까운 <paramref name="surface"/>의 표면 지점.
        /// 점 하나를 기준으로 삼으려면 두 끝을 같게 넘긴다.
        /// 캡슐(<see cref="CharacterController"/>, <see cref="CapsuleCollider"/>)과 구는 축과 선분의 가장 가까운 두 점으로 구하므로
        /// 기준이 몸 안으로 파고들어 있어도 표면 지점이 나온다. 반지름은 물리 엔진 내부 값이 아니라 설정된 값(보이는 몸)을 쓴다.
        /// 그 밖의 콜라이더는 <see cref="Collider.ClosestPoint"/>로 대신하므로, 기준이 콜라이더 안에 있으면 그 기준점을 돌려준다.
        /// 기준이 몸의 축 위에 있어 방향을 정할 수 없으면, 몸에서 <paramref name="attackerPosition"/>을 향하는 수평 방향을 법선으로 쓴다.
        /// </summary>
        public static ImpactPoint OnSurface(Collider surface, Vector3 start, Vector3 end, Vector3 attackerPosition)
        {
            if (surface == null)
            {
                throw new ArgumentNullException(nameof(surface));
            }

            if (TryGetCapsule(surface, out Vector3 axisStart, out Vector3 axisEnd, out float radius))
            {
                ClosestPoints(start, end, axisStart, axisEnd, out Vector3 onReference, out Vector3 onAxis);
                Vector3 normal = Outward(onReference - onAxis, onAxis, attackerPosition, surface.transform);
                return new ImpactPoint(onAxis + normal * radius, normal);
            }

            Vector3 center = surface.bounds.center;
            Vector3 query = ClosestPointOnSegment(start, end, center);
            Vector3 position = surface.ClosestPoint(query);
            return new ImpactPoint(position, Outward(position - center, center, attackerPosition, surface.transform));
        }

        // 몸의 축을 양 끝과 반지름으로 돌려준다. 구는 두 끝이 같은 캡슐로 본다.
        private static bool TryGetCapsule(Collider surface, out Vector3 axisStart, out Vector3 axisEnd, out float radius)
        {
            Transform transform = surface.transform;
            Vector3 scale = Abs(transform.lossyScale);
            switch (surface)
            {
                // 캐릭터 컨트롤러의 캡슐은 회전과 관계없이 늘 월드 위쪽으로 선다.
                case CharacterController controller:
                    radius = controller.radius * Mathf.Max(scale.x, scale.z);
                    SetAxis(transform.TransformPoint(controller.center), Vector3.up,
                        controller.height * scale.y, radius, out axisStart, out axisEnd);
                    return true;

                case CapsuleCollider capsule:
                    int axis = capsule.direction;
                    Vector3 localAxis = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
                    radius = capsule.radius * Mathf.Max(scale[(axis + 1) % 3], scale[(axis + 2) % 3]);
                    SetAxis(transform.TransformPoint(capsule.center), transform.TransformDirection(localAxis),
                        capsule.height * scale[axis], radius, out axisStart, out axisEnd);
                    return true;

                case SphereCollider sphere:
                    radius = sphere.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
                    axisStart = axisEnd = transform.TransformPoint(sphere.center);
                    return true;

                default:
                    axisStart = axisEnd = default;
                    radius = 0f;
                    return false;
            }
        }

        // 높이가 지름보다 짧으면 구가 되므로 축 길이는 0 아래로 내려가지 않는다.
        private static void SetAxis(
            Vector3 center, Vector3 direction, float height, float radius, out Vector3 axisStart, out Vector3 axisEnd)
        {
            Vector3 half = direction * Mathf.Max(0f, height * 0.5f - radius);
            axisStart = center - half;
            axisEnd = center + half;
        }

        private static Vector3 Outward(Vector3 offset, Vector3 origin, Vector3 attackerPosition, Transform body)
        {
            if (offset.sqrMagnitude > MinDirectionSqr)
            {
                return offset.normalized;
            }

            Vector3 toAttacker = attackerPosition - origin;
            toAttacker.y = 0f;
            return toAttacker.sqrMagnitude > MinDirectionSqr ? toAttacker.normalized : body.forward;
        }

        private static Vector3 ClosestPointOnSegment(Vector3 start, Vector3 end, Vector3 point)
        {
            Vector3 direction = end - start;
            float lengthSqr = direction.sqrMagnitude;
            if (lengthSqr <= MinDirectionSqr)
            {
                return start;
            }

            return start + direction * Mathf.Clamp01(Vector3.Dot(point - start, direction) / lengthSqr);
        }

        // 두 선분 p1~q1, p2~q2에서 서로 가장 가까운 두 점(Ericson, Real-Time Collision Detection 5.1.9). 선분이 점이어도 된다.
        private static void ClosestPoints(
            Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2, out Vector3 onFirst, out Vector3 onSecond)
        {
            Vector3 d1 = q1 - p1;
            Vector3 d2 = q2 - p2;
            Vector3 r = p1 - p2;
            float a = d1.sqrMagnitude;
            float e = d2.sqrMagnitude;
            float f = Vector3.Dot(d2, r);
            float s;
            float t;

            if (a <= MinDirectionSqr && e <= MinDirectionSqr)
            {
                s = t = 0f;
            }
            else if (a <= MinDirectionSqr)
            {
                s = 0f;
                t = Mathf.Clamp01(f / e);
            }
            else
            {
                float c = Vector3.Dot(d1, r);
                if (e <= MinDirectionSqr)
                {
                    t = 0f;
                    s = Mathf.Clamp01(-c / a);
                }
                else
                {
                    float b = Vector3.Dot(d1, d2);
                    float denominator = a * e - b * b;
                    // 평행하면 아무 점이나 골라도 되므로 첫 선분의 시작점에서 출발한다.
                    s = denominator > MinDirectionSqr ? Mathf.Clamp01((b * f - c * e) / denominator) : 0f;
                    t = (b * s + f) / e;
                    if (t < 0f)
                    {
                        t = 0f;
                        s = Mathf.Clamp01(-c / a);
                    }
                    else if (t > 1f)
                    {
                        t = 1f;
                        s = Mathf.Clamp01((b - c) / a);
                    }
                }
            }

            onFirst = p1 + d1 * s;
            onSecond = p2 + d2 * t;
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }
    }
}
