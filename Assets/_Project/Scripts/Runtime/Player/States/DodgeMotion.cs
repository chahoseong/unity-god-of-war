using System;
using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 회피 상태들이 공유하는 변위 진행기.
    /// 매 프레임 변위 커브의 차분만큼 이동시키므로, 프레임레이트와 무관하게 총 이동 거리가 프로필 거리와 같다.
    /// </summary>
    public sealed class DodgeMotion
    {
        private DodgeProfile _profile;
        private Vector3 _direction;

        public float Elapsed { get; private set; }
        public bool IsFinished => Elapsed >= _profile.Duration;

        /// <summary>월드 Yaw <paramref name="moveYaw"/>(도) 방향으로 새 회피 이동을 시작한다.</summary>
        public void Begin(DodgeProfile profile, float moveYaw)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _direction = Quaternion.Euler(0f, moveYaw, 0f) * Vector3.forward;
            Elapsed = 0f;
        }

        /// <summary>
        /// 시간을 진행하고, 이번 프레임에 <c>속도 × deltaTime</c>이 커브 구간의 변위가 되는 수평 속도(m/s)를 반환한다.
        /// </summary>
        public Vector3 Tick(float deltaTime)
        {
            if (deltaTime <= 0f || IsFinished)
            {
                return Vector3.zero;
            }

            float duration = _profile.Duration;
            float from = Elapsed / duration;
            // 마지막 프레임이 지속 시간을 넘어도 변위는 커브 끝(t = 1)까지만 낸다.
            Elapsed = Mathf.Min(Elapsed + deltaTime, duration);
            float to = Elapsed / duration;

            AnimationCurve curve = _profile.DisplacementCurve;
            float displacement = (curve.Evaluate(to) - curve.Evaluate(from)) * _profile.Distance;
            return _direction * (displacement / deltaTime);
        }
    }
}
