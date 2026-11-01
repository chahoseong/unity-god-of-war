using System;
using System.Collections.Generic;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 캐릭터 한 명의 무기. 열려 있는 동안 칼날이 지나간 공간을 매 프레임 쓸어,
    /// 새로 닿은 <see cref="IHittable"/>에게 한 번씩 타격을 전한다. 피격자가 흘린(<see cref="HitOutcome.Evaded"/>) 타격은 세지 않는다.
    /// 언제 열고 닫을지는 공격하는 쪽이 정하고, 이 컴포넌트는 검출만 한다.
    /// 애니메이션이 칼을 옮긴 뒤에 검사하도록 LateUpdate에서 판정한다.
    /// 검출한 타격은 <see cref="HitSystem"/>에 기록해, 모든 LateUpdate가 끝난 뒤 같은 프레임 안에 전한다.
    /// 패링 카운터처럼 대상이 정해진 타격은 칼날로 찾지 않고 <see cref="Strike"/>로 같은 경로에 넣는다.
    /// 피격자가 내린 판정은 <see cref="Bind"/>로 소유자가 맡긴 콜백으로만 넘긴다.
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

        // 소유자가 Bind로 맡긴 공격자와 판정을 넘길 곳. 무기의 주인은 바뀌지 않는다.
        private Transform _owner;
        private Action<HitReport> _onDealt;
        // 칼날 타격의 판정을 받는 콜백. 타격마다 할당하지 않도록 한 번만 만든다.
        private Action<HitReport> _onSwingResolved;

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

        /// <summary>
        /// 이 무기의 주인을 정한다. 소유자가 한 번만 호출한다.
        /// <paramref name="owner"/>는 이 무기로 친 타격의 공격자가 되고, 그 타격에 피격자가 내린 판정은
        /// 타격을 전한 같은 프레임 안(모든 LateUpdate 뒤)에 <paramref name="onDealt"/>로 넘긴다.
        /// </summary>
        /// <exception cref="InvalidOperationException">이미 주인이 정해져 있다.</exception>
        public void Bind(Transform owner, Action<HitReport> onDealt)
        {
            if (_owner != null)
            {
                throw new InvalidOperationException($"'{name}' is already bound to '{_owner.name}'.");
            }

            _owner = owner != null ? owner : throw new ArgumentNullException(nameof(owner));
            _onDealt = onDealt ?? throw new ArgumentNullException(nameof(onDealt));
            // 소유자의 Awake가 비활성 자식인 이 무기의 Awake보다 먼저 올 수 있어, 쓰기 전에 반드시 거치는 여기서 만든다.
            _onSwingResolved = OnSwingResolved;
        }

        private void OnDisable()
        {
            Close();
        }

        /// <summary>
        /// 새 스윙의 판정을 연다. 맞은 대상 기록을 비우고, 지금 칼 포즈부터 쓸기 시작한다.
        /// 주인과 그 자식 콜라이더는 대상에서 제외한다.
        /// </summary>
        /// <exception cref="InvalidOperationException">주인이 정해지지 않았다.</exception>
        public void Open(HitStrength strength)
        {
            RequireBound();
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
        }

        /// <summary>
        /// 칼날로 찾지 않고 <paramref name="target"/>에게 타격 한 번을 전한다. 칼이 열려 있는지와 관계없고, 스윙 기록도 건드리지 않는다.
        /// 칼날로 찾은 타격과 같은 시점(모든 LateUpdate 뒤)에 전하고, 판정도 같은 콜백으로 넘긴다.
        /// </summary>
        /// <exception cref="InvalidOperationException">주인이 정해지지 않았다.</exception>
        public void Strike(IHittable target, HitStrength strength)
        {
            RequireBound();
            Submit(target ?? throw new ArgumentNullException(nameof(target)), strength, _onDealt);
        }

        private void Submit(IHittable target, HitStrength strength, Action<HitReport> onResolved)
        {
            HitSystem.Record(target, new AttackInfo(_owner, strength), onResolved);
        }

        // 칼날 타격의 판정을 받아 스윙 기록을 고친 뒤 소유자에게 넘긴다. 판정을 알리는 것은 HitSystem뿐이다.
        private void OnSwingResolved(HitReport report)
        {
            // 흘린 타격은 맞힌 것으로 세지 않아, 무적이 끝난 뒤에도 칼날이 걸쳐 있으면 같은 스윙이 다시 맞힌다.
            // 그 사이 칼이 다시 열렸어도 새 스윙의 기록이라 지워도 해가 없다.
            if (report.Result.Outcome == HitOutcome.Evaded)
            {
                _hitTargets.Remove(report.Target);
            }

            _onDealt(report);
        }

        private void RequireBound()
        {
            if (_owner == null)
            {
                throw new InvalidOperationException($"'{name}' has no owner. Call {nameof(Bind)} first.");
            }
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
            for (int i = 1; i <= steps; i++)
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
                if (other.transform.IsChildOf(_owner))
                {
                    continue;
                }

                // 콜라이더가 여럿인 대상도 한 스윙에 한 번만 맞도록 콜라이더가 아니라 대상으로 기록한다.
                IHittable target = other.GetComponentInParent<IHittable>();
                if (target == null || !_hitTargets.Add(target))
                {
                    continue;
                }

                Submit(target, _strength, _onSwingResolved);
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
