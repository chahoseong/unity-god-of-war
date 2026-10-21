using System;
using GodOfWar.Characters;
using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 회피·공격처럼 입력이 아니라 액션이 정해 둔 이동을 매 프레임 속도로 산출한다.
    /// 매 프레임 변위 커브의 차분만큼 이동시키므로, 프레임레이트와 무관하게 총 이동 거리가 데이터의 거리와 같다.
    /// </summary>
    public sealed class ActionMovement
    {
        private BakedRootMotion _rootMotion;
        private Vector3 _direction;

        public float Elapsed { get; private set; }
        public bool IsFinished => Elapsed >= _rootMotion.Duration;

        /// <summary>
        /// <paramref name="rootMotion"/>의 변위로, 월드 Yaw <paramref name="moveYaw"/>(도) 방향의 새 이동을 시작한다.
        /// </summary>
        public void Begin(BakedRootMotion rootMotion, float moveYaw)
        {
            _rootMotion = rootMotion != null ? rootMotion : throw new ArgumentNullException(nameof(rootMotion));
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

            float duration = _rootMotion.Duration;
            AnimationCurve curve = _rootMotion.Curve;
            float from = Elapsed / duration;
            // 마지막 프레임이 지속 시간을 넘어도 변위는 커브 끝(t = 1)까지만 낸다.
            Elapsed = Mathf.Min(Elapsed + deltaTime, duration);
            float to = Elapsed / duration;

            float displacement = (curve.Evaluate(to) - curve.Evaluate(from)) * _rootMotion.Distance;
            return _direction * (displacement / deltaTime);
        }
    }
}
