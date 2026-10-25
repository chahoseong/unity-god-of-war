using System;
using UnityEngine;

namespace GodOfWar.Player
{
    /// <summary>
    /// 락온 대상을 고르고 유지하는 기준.
    /// 거리는 모두 수평 거리이고, 각도는 카메라 Yaw 기준 수평 각도다.
    /// </summary>
    [Serializable]
    public sealed class LockOnSettings
    {
        [Header("Acquire")]
        [Tooltip("락온을 새로 걸 수 있는 최대 거리(m).")]
        [SerializeField, Min(0.1f)] private float _acquireDistance = 15f;
        [Tooltip("카메라 정면에서 이 각도(도) 이내의 대상만 후보가 된다.")]
        [SerializeField, Range(1f, 180f)] private float _viewHalfAngle = 60f;
        [Tooltip("후보 점수에서 거리 항의 가중치. 1이면 획득 거리만큼 먼 것과 시야각 끝만큼 벗어난 것이 같은 비용이다.")]
        [SerializeField, Min(0f)] private float _distanceWeight = 1f;
        [Tooltip("카메라에서 조준점까지 이 레이어에 막히면 후보에서 제외한다. 캐릭터 레이어는 빼 둔다.")]
        [SerializeField] private LayerMask _occlusionMask = 1;

        [Header("Keep")]
        [Tooltip("락온을 유지하는 최대 거리(m). 획득 거리보다 커야 경계에서 켜졌다 꺼지기를 반복하지 않는다.")]
        [SerializeField, Min(0f)] private float _keepDistance = 20f;

        public LockOnSettings()
        {
        }

        public LockOnSettings(float acquireDistance, float viewHalfAngle, float distanceWeight, float keepDistance)
        {
            _acquireDistance = acquireDistance;
            _viewHalfAngle = viewHalfAngle;
            _distanceWeight = distanceWeight;
            _keepDistance = keepDistance;
        }

        public float AcquireDistance => _acquireDistance;
        public float ViewHalfAngle => _viewHalfAngle;
        public float DistanceWeight => _distanceWeight;
        public LayerMask OcclusionMask => _occlusionMask;
        public float KeepDistance => _keepDistance;
    }
}
