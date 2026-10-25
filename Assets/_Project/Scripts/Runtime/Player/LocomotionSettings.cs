using UnityEngine;

namespace GodOfWar.Player
{
    /// <summary>
    /// 플레이어 이동 상태들이 공유하는 튜닝 값.
    /// 기본값은 Synty 로코모션 클립의 이동 속도에 맞춰 발 미끄러짐을 줄인 값이다.
    /// </summary>
    [CreateAssetMenu(menuName = "GodOfWar/Player/Locomotion Settings", fileName = "LocomotionSettings")]
    public sealed class LocomotionSettings : ScriptableObject
    {
        [Header("Speed (m/s)")]
        [Tooltip("이동 입력 크기 1일 때의 스트레이프 속도. 입력 크기에 비례해 걷기~달리기로 블렌드된다.")]
        [SerializeField] private float _runSpeed = 2.5f;
        [SerializeField] private float _sprintSpeed = 7f;

        [Header("Acceleration (m/s²)")]
        [SerializeField] private float _acceleration = 20f;
        [SerializeField] private float _deceleration = 25f;

        [Header("Turn (deg/s)")]
        [SerializeField] private float _turnSpeed = 720f;
        [SerializeField] private float _sprintTurnSpeed = 540f;

        [Header("Sprint")]
        [Tooltip("전방(입력 위쪽)에서 이 각도(도) 이내의 입력만 스프린트를 허용한다.")]
        [SerializeField, Range(0f, 90f)] private float _sprintMaxAngle = 50f;
        [Tooltip("스프린트를 허용하는 최소 이동 입력 크기.")]
        [SerializeField, Range(0f, 1f)] private float _sprintMinInput = 0.5f;

        [Header("Animator")]
        [SerializeField] private float _animatorDampTime = 0.1f;

        public float RunSpeed => _runSpeed;
        public float SprintSpeed => _sprintSpeed;
        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;
        public float TurnSpeed => _turnSpeed;
        public float SprintTurnSpeed => _sprintTurnSpeed;
        public float SprintMaxAngle => _sprintMaxAngle;
        public float SprintMinInput => _sprintMinInput;
        public float AnimatorDampTime => _animatorDampTime;
    }
}
