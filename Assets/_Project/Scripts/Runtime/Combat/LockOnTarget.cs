using System.Collections.Generic;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 락온할 수 있는 대상의 표식. 켜져 있는 동안 <see cref="Active"/>에 등록된다.
    /// 큰 적처럼 락온 지점이 여러 개면 지점마다 하나씩 둔다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LockOnTarget : MonoBehaviour
    {
        private static readonly List<LockOnTarget> s_active = new List<LockOnTarget>();

        [Tooltip("카메라와 인디케이터가 겨냥하는 지점. 비워 두면 이 오브젝트의 위치를 쓴다.")]
        [SerializeField] private Transform _aimPoint;

        [Tooltip("락온 가능 여부. 스폰·사망 연출처럼 살아 있지만 락온하면 안 되는 동안 끈다.")]
        [SerializeField] private bool _isTargetable = true;

        /// <summary>현재 켜져 있는 대상 목록. 락온 후보를 찾는 유일한 조회 지점이다.</summary>
        public static IReadOnlyList<LockOnTarget> Active => s_active;

        public Transform AimPoint => _aimPoint != null ? _aimPoint : transform;

        /// <summary>지금 락온할 수 있는지. 컴포넌트가 꺼져 있으면 항상 거짓이다.</summary>
        public bool IsTargetable
        {
            get => _isTargetable && isActiveAndEnabled;
            set => _isTargetable = value;
        }

        // 도메인 리로드를 끈 프로젝트라 정적 목록이 플레이 세션 사이에 남지 않도록 시작할 때 비운다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            s_active.Clear();
        }

        private void OnEnable()
        {
            s_active.Add(this);
        }

        private void OnDisable()
        {
            s_active.Remove(this);
        }
    }
}
