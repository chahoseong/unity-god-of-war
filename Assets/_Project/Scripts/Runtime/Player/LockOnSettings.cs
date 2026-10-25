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

        [Header("Flick Switch")]
        [Tooltip("락온 중 스틱 좌우 입력이 이 크기 이상이면 그 방향의 다음 대상으로 전환한다.")]
        [SerializeField, Range(0.1f, 1f)] private float _stickFlickThreshold = 0.7f;
        [Tooltip("스틱 좌우 입력이 이 크기 아래로 돌아와야 다음 전환을 받는다. 발동 문턱보다 작아야 손떨림으로 연속 전환되지 않는다.")]
        [SerializeField, Range(0f, 1f)] private float _stickRearmThreshold = 0.3f;
        [Tooltip("마우스 좌우 이동량의 누적이 이 값 이상이면 전환한다.")]
        [SerializeField, Min(1f)] private float _mouseFlickThreshold = 50f;
        [Tooltip("마우스 이동량 누적이 줄어드는 속도(1/s). 클수록 짧은 시간에 휘둘러야 전환된다.")]
        [SerializeField, Min(0f)] private float _mouseDecayRate = 10f;
        [Tooltip("마우스로 전환한 뒤 다음 전환을 받지 않는 시간(초). 한 번 휘두른 마우스가 대상을 연달아 넘기지 않게 한다.")]
        [SerializeField, Min(0f)] private float _mouseFlickCooldown = 0.25f;

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

        /// <summary>플릭 전환 설정만 지정한다. 나머지는 기본값이다.</summary>
        public static LockOnSettings ForFlick(
            float stickFlickThreshold, float stickRearmThreshold,
            float mouseFlickThreshold, float mouseDecayRate, float mouseFlickCooldown)
        {
            return new LockOnSettings
            {
                _stickFlickThreshold = stickFlickThreshold,
                _stickRearmThreshold = stickRearmThreshold,
                _mouseFlickThreshold = mouseFlickThreshold,
                _mouseDecayRate = mouseDecayRate,
                _mouseFlickCooldown = mouseFlickCooldown
            };
        }

        public float AcquireDistance => _acquireDistance;
        public float ViewHalfAngle => _viewHalfAngle;
        public float DistanceWeight => _distanceWeight;
        public LayerMask OcclusionMask => _occlusionMask;
        public float KeepDistance => _keepDistance;
        public float StickFlickThreshold => _stickFlickThreshold;
        public float StickRearmThreshold => _stickRearmThreshold;
        public float MouseFlickThreshold => _mouseFlickThreshold;
        public float MouseDecayRate => _mouseDecayRate;
        public float MouseFlickCooldown => _mouseFlickCooldown;
    }
}
