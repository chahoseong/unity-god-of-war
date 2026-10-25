using System;
using GodOfWar.CameraSystem;
using GodOfWar.Combat;
using GodOfWar.Input;
using UnityEngine;

namespace GodOfWar.Player
{
    /// <summary>
    /// LockOn 입력으로 락온 대상을 고르고, 대상이 무효해지거나 멀어지면 다음 대상으로 넘기거나 해제한다.
    /// 상태 전이가 아니므로 액션 라우터를 거치지 않고 입력을 직접 구독한다.
    /// </summary>
    /// <remarks>
    /// CameraLook(-50)보다 먼저 실행되어, 대상이 사라진 프레임에 카메라가 이미 해제된 결과를 읽게 한다.
    /// </remarks>
    [DefaultExecutionOrder(-60)]
    [DisallowMultipleComponent]
    public sealed class TargetLock : MonoBehaviour
    {
        [SerializeField] private LockOnSettings _settings = new LockOnSettings();

        [Tooltip("후보의 시야각 기준이 되는 카메라 피벗.")]
        [SerializeField] private CameraLook _cameraLook;

        private IPlayerInput _input;
        private Transform _viewOrigin;

        /// <summary>현재 락온 대상. 락온하지 않았으면 null이다.</summary>
        public LockOnTarget Current { get; private set; }

        /// <summary>대상이 바뀔 때 새 대상으로 발생한다. 해제되면 null로 발생한다.</summary>
        public event Action<LockOnTarget> TargetChanged;

        /// <summary>
        /// 대상이 획득 거리와 시야각 안에 있으면 점수를 내고 참을 반환한다. 점수가 낮을수록 우선이다.
        /// 점수는 시야각 대비 벗어난 각도와 획득 거리 대비 거리의 가중 합이며, 높이 차는 무시한다.
        /// </summary>
        public static bool TryScore(
            Vector3 origin, float viewYaw, Vector3 aimPoint, LockOnSettings settings, out float score)
        {
            score = 0f;
            Vector3 offset = aimPoint - origin;
            offset.y = 0f;

            float distance = offset.magnitude;
            if (distance > settings.AcquireDistance)
            {
                return false;
            }

            // 수평으로 겹친 대상은 방향이 없으므로 정면으로 본다.
            float angle = distance > Mathf.Epsilon
                ? Mathf.Abs(Mathf.DeltaAngle(viewYaw, Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg))
                : 0f;
            if (angle > settings.ViewHalfAngle)
            {
                return false;
            }

            score = angle / settings.ViewHalfAngle
                + settings.DistanceWeight * distance / settings.AcquireDistance;
            return true;
        }

        /// <summary>대상이 락온 유지 거리 안에 있는지. 높이 차는 무시한다.</summary>
        public static bool IsWithinKeepDistance(Vector3 origin, Vector3 aimPoint, LockOnSettings settings)
        {
            Vector3 offset = aimPoint - origin;
            offset.y = 0f;
            return offset.sqrMagnitude <= settings.KeepDistance * settings.KeepDistance;
        }

        /// <summary>락온을 해제한다. 락온하지 않았으면 아무 일도 없다.</summary>
        public void Release()
        {
            SetTarget(null);
        }

        private void Awake()
        {
            if (_cameraLook == null)
            {
                throw new InvalidOperationException(
                    $"'{name}' has no {nameof(CameraLook)} assigned. Assign the camera pivot.");
            }

            // 인터페이스 타입은 Unity의 가짜 null을 == null로 거를 수 없어 TryGetComponent를 쓴다.
            if (!TryGetComponent(out _input))
            {
                throw new InvalidOperationException($"'{name}' requires an {nameof(IPlayerInput)} component.");
            }
        }

        private void Start()
        {
            Camera viewCamera = Camera.main;
            if (viewCamera == null)
            {
                throw new InvalidOperationException(
                    $"'{name}' needs a camera tagged MainCamera to check whether targets are occluded.");
            }

            _viewOrigin = viewCamera.transform;
        }

        private void OnEnable()
        {
            // Awake가 예외로 중단된 경우 원인 예외만 남기고 후속 NullReferenceException을 내지 않는다.
            if (_input != null)
            {
                _input.ActionTriggered += OnActionTriggered;
            }
        }

        private void OnDisable()
        {
            if (_input != null)
            {
                _input.ActionTriggered -= OnActionTriggered;
            }

            Release();
        }

        private void Update()
        {
            // 락온하지 않은 상태와 대상이 소멸한 상태를 구분하려고 참조 비교를 쓴다.
            if (ReferenceEquals(Current, null))
            {
                return;
            }

            // 대상이 소멸했거나 락온 불가가 되면, 처음 누를 때와 같은 기준으로 다음 대상을 고른다.
            if (Current == null || !Current.IsTargetable)
            {
                SetTarget(FindBestTarget());
                return;
            }

            // 스스로 멀어진 경우라 다른 대상으로 넘기지 않는다.
            if (!IsWithinKeepDistance(transform.position, Current.AimPoint.position, _settings))
            {
                Release();
            }
        }

        private void OnActionTriggered(PlayerActionEvent e)
        {
            if (e.Action != PlayerAction.LockOn || e.Phase != ActionPhase.Pressed)
            {
                return;
            }

            if (Current != null)
            {
                Release();
                return;
            }

            SetTarget(FindBestTarget());
        }

        private LockOnTarget FindBestTarget()
        {
            Vector3 origin = transform.position;
            float viewYaw = _cameraLook.Yaw;

            LockOnTarget best = null;
            float bestScore = float.MaxValue;
            foreach (LockOnTarget target in LockOnTarget.Active)
            {
                if (!target.IsTargetable)
                {
                    continue;
                }

                Vector3 aimPoint = target.AimPoint.position;
                if (!TryScore(origin, viewYaw, aimPoint, _settings, out float score) || score >= bestScore)
                {
                    continue;
                }

                // 레이캐스트는 더 나은 후보에게만 쏜다.
                if (IsOccluded(aimPoint))
                {
                    continue;
                }

                best = target;
                bestScore = score;
            }

            return best;
        }

        private bool IsOccluded(Vector3 aimPoint)
        {
            return Physics.Linecast(
                _viewOrigin.position, aimPoint, _settings.OcclusionMask, QueryTriggerInteraction.Ignore);
        }

        private void SetTarget(LockOnTarget target)
        {
            if (ReferenceEquals(Current, target))
            {
                return;
            }

            Current = target;
            TargetChanged?.Invoke(target);
        }

        private void OnDrawGizmos()
        {
            if (Current == null)
            {
                return;
            }

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, Current.AimPoint.position);
        }
    }
}
