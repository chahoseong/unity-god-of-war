using UnityEngine;

namespace GodOfWar.Characters
{
    /// <summary>
    /// 상태가 결정한 수평 속도와 회전을 <see cref="CharacterController"/>에 적용한다.
    /// 수직 이동은 중력만 다루며, 무엇을 할지는 판단하지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMotor : MonoBehaviour
    {
        [SerializeField] private float _gravity = -20f;

        [Tooltip("접지 중에 바닥에 붙여 두는 하향 속도. 경사를 내려갈 때 공중 판정이 나지 않게 한다.")]
        [SerializeField] private float _groundedVerticalSpeed = -2f;

        private CharacterController _controller;
        private float _verticalSpeed;

        /// <summary>마지막 <see cref="Move"/>에 전달된 수평 속도 (월드 공간, m/s).</summary>
        public Vector3 HorizontalVelocity { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        public void Move(Vector3 horizontalVelocity, float deltaTime)
        {
            horizontalVelocity.y = 0f;
            HorizontalVelocity = horizontalVelocity;

            _verticalSpeed = _controller.isGrounded
                ? _groundedVerticalSpeed
                : _verticalSpeed + _gravity * deltaTime;

            Vector3 velocity = horizontalVelocity + Vector3.up * _verticalSpeed;
            _controller.Move(velocity * deltaTime);
        }

        /// <summary>Y축 기준으로 <paramref name="yaw"/>(도)를 향해 최대 <paramref name="maxDegreesDelta"/>만큼 회전한다.</summary>
        public void RotateTowards(float yaw, float maxDegreesDelta)
        {
            Quaternion target = Quaternion.Euler(0f, yaw, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, maxDegreesDelta);
        }
    }
}
