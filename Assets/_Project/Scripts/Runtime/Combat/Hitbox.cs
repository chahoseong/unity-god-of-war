using System;
using System.Collections.Generic;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 무기 칼날을 따라 타격 대상을 찾는 판정. 열려 있는 동안 칼날이 지나간 공간을 매 프레임 쓸어,
    /// 새로 닿은 <see cref="IHittable"/>에게 한 번씩 타격을 전한다.
    /// 언제 열고 닫을지는 공격하는 쪽이 정하고, 이 컴포넌트는 검출만 한다.
    /// 애니메이션이 칼을 옮긴 뒤에 검사하도록 LateUpdate에서 판정한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Hitbox : MonoBehaviour
    {
        private const int MaxOverlaps = 16;

        [Tooltip("칼날이 시작하는 지점(코등이 쪽). 이 오브젝트나 그 자식이어야 한다.")]
        [SerializeField] private Transform _base;

        [Tooltip("칼끝. 이 오브젝트나 그 자식이어야 한다.")]
        [SerializeField] private Transform _tip;

        [Tooltip("칼날 캡슐의 반지름(m). 프레임 사이를 보간하는 간격의 기준이기도 하다.")]
        [SerializeField, Min(0.01f)] private float _radius = 0.05f;

        [Tooltip("타격 대상을 찾을 레이어.")]
        [SerializeField] private LayerMask _targetLayers = ~0;

        [Tooltip("한 프레임에 보간하는 최대 단계 수. 프레임이 크게 끊길 때의 비용 상한이다.")]
        [SerializeField, Min(1)] private int _maxSubsteps = 16;

        private readonly Collider[] _overlaps = new Collider[MaxOverlaps];
        private readonly HashSet<IHittable> _hitTargets = new HashSet<IHittable>();

        private Transform _attacker;
        private HitStrength _strength;

        // 칼날 양 끝의 이 오브젝트 기준 오프셋(회전만 적용, 배율 미적용). 포즈를 보간해 양 끝을 다시 계산한다.
        private Vector3 _baseOffset;
        private Vector3 _tipOffset;
        private Vector3 _previousPosition;
        private Quaternion _previousRotation;

#if UNITY_EDITOR
        // 판정 구간을 튜닝할 때 볼 수 있도록, 마지막 스윙에서 검사한 칼날 선분을 다음 스윙이 열릴 때까지 남긴다.
        // 프레임이 아주 잘게 나뉘어도 무한히 쌓이지 않게 상한을 둔다.
        private const int MaxDebugSweeps = 1024;
        private readonly List<(Vector3 start, Vector3 end)> _debugSweeps = new List<(Vector3, Vector3)>();
#endif

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (_base == null || _tip == null)
            {
                throw new InvalidOperationException($"'{name}' has no blade base or tip assigned.");
            }
        }

        private void OnDisable()
        {
            Close();
        }

        /// <summary>
        /// 새 스윙의 판정을 연다. 맞은 대상 기록을 비우고, 지금 칼 포즈부터 쓸기 시작한다.
        /// <paramref name="attacker"/>와 그 자식 콜라이더는 대상에서 제외한다.
        /// </summary>
        public void Open(Transform attacker, HitStrength strength)
        {
            _attacker = attacker != null ? attacker : throw new ArgumentNullException(nameof(attacker));
            _strength = strength;
            _hitTargets.Clear();

            Quaternion inverse = Quaternion.Inverse(transform.rotation);
            _baseOffset = inverse * (_base.position - transform.position);
            _tipOffset = inverse * (_tip.position - transform.position);

            // 지난 스윙이 끝난 포즈부터 쓸면 그 사이의 대상을 잘못 친다.
            _previousPosition = transform.position;
            _previousRotation = transform.rotation;
            IsOpen = true;

#if UNITY_EDITOR
            _debugSweeps.Clear();
#endif
        }

        public void Close()
        {
            IsOpen = false;
            _attacker = null;
        }

        private void LateUpdate()
        {
            if (!IsOpen)
            {
                return;
            }

            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;

            // 자동 동기화가 꺼져 있어, 이번 프레임에 움직인 콜라이더를 쿼리 전에 반영한다.
            Physics.SyncTransforms();

            // 칼날은 손잡이를 축으로 돌기 때문에 양 끝을 선형 보간하면 호의 안쪽을 지나간다.
            // 포즈(위치·회전)를 보간해 칼날이 실제로 지나간 호를 따라 검사한다.
            int steps = GetStepCount(position, rotation);
            for (int i = 1; i <= steps && IsOpen; i++)
            {
                float t = (float)i / steps;
                Sweep(Vector3.Lerp(_previousPosition, position, t), Quaternion.Slerp(_previousRotation, rotation, t));
            }

            _previousPosition = position;
            _previousRotation = rotation;
        }

        /// <summary>검사 간격이 칼날 반지름을 넘지 않도록, 칼날 양 끝 중 더 많이 움직인 쪽을 기준으로 단계 수를 정한다.</summary>
        private int GetStepCount(Vector3 position, Quaternion rotation)
        {
            float baseTravel = Vector3.Distance(
                _previousPosition + _previousRotation * _baseOffset, position + rotation * _baseOffset);
            float tipTravel = Vector3.Distance(
                _previousPosition + _previousRotation * _tipOffset, position + rotation * _tipOffset);

            return Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(baseTravel, tipTravel) / _radius), 1, _maxSubsteps);
        }

        private void Sweep(Vector3 position, Quaternion rotation)
        {
            Vector3 start = position + rotation * _baseOffset;
            Vector3 end = position + rotation * _tipOffset;

#if UNITY_EDITOR
            if (_debugSweeps.Count < MaxDebugSweeps)
            {
                _debugSweeps.Add((start, end));
            }
#endif

            int count = Physics.OverlapCapsuleNonAlloc(
                start, end, _radius, _overlaps, _targetLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider other = _overlaps[i];
                if (other.transform.IsChildOf(_attacker))
                {
                    continue;
                }

                // 콜라이더가 여럿인 대상도 한 스윙에 한 번만 맞도록 콜라이더가 아니라 대상으로 기록한다.
                IHittable target = other.GetComponentInParent<IHittable>();
                if (target == null || !_hitTargets.Add(target))
                {
                    continue;
                }

                target.ReceiveHit(new AttackInfo(_attacker, _strength));
                // 피격 처리 중에 공격자가 비활성화되는 등으로 판정이 닫혔으면 남은 대상은 치지 않는다.
                if (!IsOpen)
                {
                    return;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_base == null || _tip == null)
            {
                return;
            }

            // 판정 중인 스윙은 빨강, 닫힌 뒤 남은 마지막 스윙은 노랑으로 그린다.
            Gizmos.color = IsOpen ? Color.red : Color.yellow;
            foreach ((Vector3 start, Vector3 end) in _debugSweeps)
            {
                Gizmos.DrawLine(start, end);
            }

            Gizmos.color = Color.gray;
            Gizmos.DrawLine(_base.position, _tip.position);
            Gizmos.DrawWireSphere(_base.position, _radius);
            Gizmos.DrawWireSphere(_tip.position, _radius);
        }
#endif
    }
}
