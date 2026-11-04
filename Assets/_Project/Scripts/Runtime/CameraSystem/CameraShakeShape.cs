using System;
using Unity.Cinemachine;
using UnityEngine;

namespace GodOfWar.CameraSystem
{
    /// <summary>
    /// 카메라 흔들림의 모양. Cinemachine Impulse의 기본 모양 중 곡선 없이 쓸 수 있는 것만 둔다.
    /// </summary>
    public enum CameraShakeShape
    {
        /// <summary>맞는 순간 가장 크게 튕긴 뒤 잦아든다. 날카로운 타격.</summary>
        Recoil,

        /// <summary>부드럽게 한 번 눌렸다 돌아온다. 가벼운 접촉.</summary>
        Bump,

        /// <summary>반대쪽부터 크게 출렁인다. 자세가 무너지는 충격.</summary>
        Explosion,

        /// <summary>길게 떨린다.</summary>
        Rumble
    }

    /// <summary>
    /// 카메라 흔들림을 낸다. 흔들림은 거리와 관계없이 같은 세기로, 카메라 각도와 관계없이 화면 아래쪽으로 민다.
    /// 리스너가 카메라 공간으로 해석하도록 설정되어 있어야 한다.
    /// </summary>
    public static class CameraShake
    {
        /// <param name="position">흔들림이 시작된 곳.</param>
        /// <param name="duration">흔들리는 시간(초, 전역 게임 시간).</param>
        /// <param name="force">화면 아래쪽으로 밀리는 최대 거리(m). 0 이하면 흔들지 않는다.</param>
        public static void Emit(Vector3 position, CameraShakeShape shape, float duration, float force)
        {
            if (force <= 0f)
            {
                return;
            }

            // 정의는 진행 중인 흔들림이 계속 읽으므로 매번 새로 만든다.
            var impulse = new CinemachineImpulseDefinition
            {
                ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
                ImpulseShape = ToImpulseShape(shape),
                ImpulseDuration = duration
            };
            impulse.CreateEvent(position, Vector3.down * force);
        }

        private static CinemachineImpulseDefinition.ImpulseShapes ToImpulseShape(CameraShakeShape shape)
        {
            return shape switch
            {
                CameraShakeShape.Recoil => CinemachineImpulseDefinition.ImpulseShapes.Recoil,
                CameraShakeShape.Bump => CinemachineImpulseDefinition.ImpulseShapes.Bump,
                CameraShakeShape.Explosion => CinemachineImpulseDefinition.ImpulseShapes.Explosion,
                CameraShakeShape.Rumble => CinemachineImpulseDefinition.ImpulseShapes.Rumble,
                _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null)
            };
        }
    }
}
