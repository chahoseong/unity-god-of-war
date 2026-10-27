using GodOfWar.Combat;
using UnityEngine;

namespace GodOfWar.Player
{
    /// <summary>
    /// 가드와 가드 브레이크의 튜닝 값. 가속·감속·회전은 기본 이동 설정을 그대로 쓴다.
    /// </summary>
    [CreateAssetMenu(menuName = "GodOfWar/Player/Guard Settings", fileName = "GuardSettings")]
    public sealed class GuardSettings : ScriptableObject
    {
        [Tooltip("가드 중 이동 속도(m/s). Locomotion 블렌드 트리의 걷기 반지름과 같아야 다리 클립이 미끄러지지 않는다.")]
        [SerializeField, Min(0f)] private float _walkSpeed = 1.4f;

        [Tooltip("가드를 올리고 내리는 상체 클립과, 진입 시 이동 클립 복귀에 쓰는 크로스페이드 시간(초).")]
        [SerializeField, Min(0f)] private float _crossFadeDuration = 0.1f;

        [Tooltip("막을 수 있는 범위의 반각(도). 몸 정면과 공격자 쪽 사이 수평 각도가 이 값 이하면 막는다. "
                 + "피격 반응 방향을 고르는 구역과는 별개다.")]
        [SerializeField, Range(0f, 180f)] private float _guardHalfAngle = 45f;

        [Tooltip("강공격을 막다 자세가 무너질 때 재생할 반응.")]
        [SerializeField] private HitReaction _guardBreak = new HitReaction();

        [Header("Block Reaction")]
        [Tooltip("공격을 막았을 때 상체에 재생하는 클립. Guard 레이어 Guard_Block 상태의 클립과 같아야 한다. "
                 + "이 클립 길이 동안은 이동 입력을 받지 않고 버틴다.")]
        [SerializeField] private AnimationClip _blockClip;

        [Tooltip("막았을 때 공격 반대쪽으로 밀려나는 거리(m).")]
        [SerializeField, Min(0f)] private float _blockPushDistance = 0.2f;

        [Tooltip("밀려나는 데 걸리는 시간(초). 처음이 가장 빠르고 끝에서 멈춘다. 0이면 밀리지 않는다.")]
        [SerializeField, Min(0f)] private float _blockPushDuration = 0.15f;

        public float WalkSpeed => _walkSpeed;
        public float CrossFadeDuration => _crossFadeDuration;
        public float GuardHalfAngle => _guardHalfAngle;
        public HitReaction GuardBreak => _guardBreak;
        public AnimationClip BlockClip => _blockClip;
        public float BlockPushDistance => _blockPushDistance;
        public float BlockPushDuration => _blockPushDuration;
    }
}
