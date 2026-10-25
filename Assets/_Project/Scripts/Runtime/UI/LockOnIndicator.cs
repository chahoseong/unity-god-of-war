using System;
using GodOfWar.Combat;
using GodOfWar.Player;
using UnityEngine;

namespace GodOfWar.UI
{
    /// <summary>
    /// 현재 락온 대상의 조준점 위치에 화면 공간 표식을 그린다.
    /// 대상이 없거나 카메라 뒤에 있으면 표식을 숨긴다.
    /// </summary>
    /// <remarks>
    /// CinemachineBrain(실행 순서 100)이 카메라를 옮긴 뒤 계산해야 표식이 대상보다 한 프레임 뒤처지지 않는다.
    /// </remarks>
    [DefaultExecutionOrder(ExecutionOrder.LockOnIndicator)]
    [DisallowMultipleComponent]
    public sealed class LockOnIndicator : MonoBehaviour
    {
        [Tooltip("락온 대상을 제공하는 플레이어의 TargetLock.")]
        [SerializeField] private TargetLock _targetLock;

        [Tooltip("조준점 위치로 옮길 표식. Screen Space - Overlay 캔버스 아래에 둔다.")]
        [SerializeField] private RectTransform _marker;

        private Camera _camera;

        private void Awake()
        {
            if (_targetLock == null)
            {
                throw new InvalidOperationException($"'{name}' has no {nameof(TargetLock)} assigned.");
            }

            if (_marker == null)
            {
                throw new InvalidOperationException($"'{name}' has no marker assigned.");
            }

            _marker.gameObject.SetActive(false);
        }

        private void Start()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                throw new InvalidOperationException($"'{name}' needs a camera tagged MainCamera.");
            }
        }

        private void LateUpdate()
        {
            // 대상이 같은 프레임에 소멸해도 Unity 비교에서 null이 되어 숨긴다.
            LockOnTarget target = _targetLock.Current;
            Vector3 screenPoint = target != null
                ? _camera.WorldToScreenPoint(target.AimPoint.position)
                : Vector3.zero;

            bool isVisible = target != null && screenPoint.z > 0f;
            if (_marker.gameObject.activeSelf != isVisible)
            {
                _marker.gameObject.SetActive(isVisible);
            }

            if (isVisible)
            {
                _marker.position = new Vector3(screenPoint.x, screenPoint.y, 0f);
            }
        }
    }
}
