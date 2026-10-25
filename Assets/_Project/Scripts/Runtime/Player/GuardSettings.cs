using UnityEngine;

namespace GodOfWar.Player
{
    /// <summary>
    /// 가드 상태의 튜닝 값. 가속·감속·회전은 기본 이동 설정을 그대로 쓴다.
    /// </summary>
    [CreateAssetMenu(menuName = "GodOfWar/Player/Guard Settings", fileName = "GuardSettings")]
    public sealed class GuardSettings : ScriptableObject
    {
        [Tooltip("가드 중 이동 속도(m/s). Locomotion 블렌드 트리의 걷기 반지름과 같아야 다리 클립이 미끄러지지 않는다.")]
        [SerializeField, Min(0f)] private float _walkSpeed = 1.4f;

        [Tooltip("가드를 올리고 내리는 상체 클립과, 진입 시 이동 클립 복귀에 쓰는 크로스페이드 시간(초).")]
        [SerializeField, Min(0f)] private float _crossFadeDuration = 0.1f;

        public float WalkSpeed => _walkSpeed;
        public float CrossFadeDuration => _crossFadeDuration;
    }
}
