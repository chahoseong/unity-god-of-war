using System;
using GodOfWar.Characters;
using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 회피·공격처럼 입력이 아니라 액션이 정해 둔 이동을 매 프레임 속도로 산출한다.
    /// 매 프레임 루트 궤적의 차분만큼 이동시키므로, 프레임레이트와 무관하게 끝 위치가 궤적의 끝과 같다.
    /// </summary>
    public sealed class ActionMovement
    {
        private BakedRootMotion _rootMotion;
        private Quaternion _rotation;

        public float Elapsed { get; private set; }
        public float Duration => _rootMotion.Duration;
        public bool IsFinished => Elapsed >= _rootMotion.Duration;

        /// <summary>
        /// 클립의 로컬 정면이 월드 Yaw <paramref name="yaw"/>(도)를 향하도록 <paramref name="rootMotion"/>의 궤적을 새로 시작한다.
        /// </summary>
        public void Begin(BakedRootMotion rootMotion, float yaw)
        {
            _rootMotion = rootMotion != null ? rootMotion : throw new ArgumentNullException(nameof(rootMotion));
            _rotation = Quaternion.Euler(0f, yaw, 0f);
            Elapsed = 0f;
        }

        /// <summary>
        /// 진행 중인 궤적의 로컬 정면이 월드 Yaw <paramref name="yaw"/>(도)를 향하게 바꾼다.
        /// 이후 프레임의 변위만 새 방향으로 회전하며, 이미 이동한 거리는 그대로 둔다.
        /// </summary>
        public void SetYaw(float yaw)
        {
            _rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>
        /// 시간을 진행하고, 이번 프레임에 <c>속도 × deltaTime</c>이 궤적 구간의 변위가 되는 수평 속도(m/s)를 반환한다.
        /// </summary>
        public Vector3 Tick(float deltaTime)
        {
            if (deltaTime <= 0f || IsFinished)
            {
                return Vector3.zero;
            }

            Vector3 from = _rootMotion.Sample(Elapsed);
            // 마지막 프레임이 지속 시간을 넘어도 변위는 궤적 끝까지만 낸다.
            Elapsed = Mathf.Min(Elapsed + deltaTime, _rootMotion.Duration);
            Vector3 to = _rootMotion.Sample(Elapsed);

            return _rotation * (to - from) / deltaTime;
        }
    }
}
