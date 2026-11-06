using System;
using GodOfWar.Effects;
using UnityEngine;

namespace GodOfWar.Enemies
{
    /// <summary>
    /// 넉다운으로 쓰러진 더미의 몸이 바닥에 부딪히는 순간 소리를 낸다. 바닥에 부딪히는 것은 월드 사건이라 쓰러진 쪽에 둔다.
    /// 더미의 판정·상태는 모르고 사건만 받는다. 빼도 더미는 그대로 동작한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TrainingDummy))]
    public sealed class GroundImpactSound : MonoBehaviour
    {
        [Tooltip("바닥에 부딪힐 때 낼 소리.")]
        [SerializeField] private SoundCue _sound = new SoundCue();

        [Tooltip("0이면 거리와 방향이 없는 2D, 1이면 더미 위치에서 나는 3D 소리.")]
        [SerializeField, Range(0f, 1f)] private float _spatialBlend;

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
            SoundPlayer.Play(_sound, transform.position, _spatialBlend);
        }
    }
}
