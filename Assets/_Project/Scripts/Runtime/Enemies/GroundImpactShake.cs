using System;
using GodOfWar.CameraSystem;
using UnityEngine;

namespace GodOfWar.Enemies
{
    /// <summary>
    /// 넉다운으로 쓰러진 더미의 몸이 바닥에 부딪히는 순간 카메라를 흔든다. 바닥에 부딪히는 것은 월드 사건이라 쓰러진 쪽에 둔다.
    /// 더미의 판정·상태는 모르고 사건만 받는다. 빼도 더미는 그대로 동작한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TrainingDummy))]
    public sealed class GroundImpactShake : MonoBehaviour
    {
        [Tooltip("흔들림의 모양.")]
        [SerializeField] private CameraShakeShape _shape = CameraShakeShape.Bump;

        [Tooltip("흔들리는 시간(초, 전역 게임 시간).")]
        [SerializeField, Min(0f)] private float _duration = 0.2f;

        [Tooltip("화면 아래쪽으로 밀리는 최대 거리(m). 회전 떨림도 이 값에 비례한다. 0이면 흔들지 않는다.")]
        [SerializeField, Min(0f)] private float _force = 0.04f;

        private TrainingDummy _dummy;

        private void Awake()
        {
            // RequireComponent는 이미 만들어진 프리팹에 컴포넌트를 채워 주지 않으므로 직접 확인한다.
            if (!TryGetComponent(out _dummy))
            {
                throw new InvalidOperationException($"'{name}' requires a {nameof(TrainingDummy)} component.");
            }
        }

        private void OnEnable()
        {
            _dummy.GroundImpacted += OnGroundImpacted;
        }

        private void OnDisable()
        {
            _dummy.GroundImpacted -= OnGroundImpacted;
        }

        private void OnGroundImpacted()
        {
            CameraShake.Emit(transform.position, _shape, _duration, _force);
        }
    }
}
