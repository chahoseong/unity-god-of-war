using System;
using System.Collections.Generic;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 무기의 공격 판정. 열려 있는 동안 칼날이 지나간 공간을 매 프레임 쓸어,
    /// 새로 닿은 <see cref="IHittable"/>에게 한 번씩 타격을 전한다. 피격자가 흘린(<see cref="HitOutcome.Evaded"/>) 타격은 세지 않는다.
    /// 콜라이더가 아니라, 같은 오브젝트의 <see cref="Weapon"/> 양 끝을 잇는 캡슐로 물리 쿼리를 한다.
    /// 프레임 사이는 <see cref="SwingTracker"/>로 보간해 빠른 스윙에서도 칼날이 실제로 지나간 호를 따라 검사한다.
    /// 언제 열고 닫을지는 <see cref="Weapon"/>이 정하고, 공격자와 판정을 넘길 곳도 <see cref="Weapon.Bind"/>를 거쳐 받는다.
    /// 애니메이션이 칼을 옮긴 뒤에 검사하도록 LateUpdate에서 판정한다.
    /// 검출한 타격은 <see cref="HitSystem"/>에 기록해, 모든 LateUpdate가 끝난 뒤 같은 프레임 안에 전한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Weapon))]
    public sealed class WeaponHitbox : MonoBehaviour
    {
        private const int MaxOverlaps = 16;

        [Tooltip("칼날 캡슐의 반지름(m). 프레임 사이를 보간하는 간격의 기준이기도 하다.")]
        [SerializeField, Min(0.01f)] private float _radius = 0.05f;

        [Tooltip("타격 대상을 찾을 레이어.")]
        [SerializeField] private LayerMask _targetLayers = ~0;

        [Tooltip("한 프레임에 보간하는 최대 단계 수. 프레임이 크게 끊길 때의 비용 상한이다.")]
        [SerializeField, Min(1)] private int _maxSubsteps = 16;

        private readonly Collider[] _overlaps = new Collider[MaxOverlaps];
        private readonly HashSet<IHittable> _hitTargets = new HashSet<IHittable>();

        // 무기가 Bind로 넘긴 공격자와 판정을 넘길 곳. 무기의 주인은 바뀌지 않는다.
        private Transform _owner;
        private Action<HitEvent> _onDealt;
        // 칼날 타격의 판정을 받는 콜백. 타격마다 할당하지 않도록 한 번만 만든다.
        private Action<HitEvent> _onResolved;

        private HitStrength _strength;
        private bool _knockdown;
        private int _damage;
        private SwingTracker _tracker;

#if UNITY_EDITOR
        // 판정 구간을 튜닝할 때 볼 수 있도록, 마지막 스윙에서 검사한 칼날 선분을 다음 스윙이 열릴 때까지 남긴다.
        // 프레임이 아주 잘게 나뉘어도 무한히 쌓이지 않게 상한을 둔다.
        private const int MaxDebugSweeps = 1024;
        private readonly List<(Vector3 start, Vector3 end)> _debugSweeps = new List<(Vector3, Vector3)>();
#endif

        public bool IsOpen { get; private set; }

        /// <summary>칼날 캡슐의 반지름(m).</summary>
        public float Radius => _radius;

        private void Awake()
        {
            _tracker = GetComponent<Weapon>().CreateTracker();
        }

        /// <summary>
        /// 이 판정으로 친 타격의 공격자와 판정을 넘길 곳을 정한다. <see cref="Weapon.Bind"/>가 한 번만 호출한다.
        /// 주인과 그 자식 콜라이더는 대상에서 제외한다.
        /// </summary>
        /// <exception cref="InvalidOperationException">이미 주인이 정해져 있다.</exception>
        public void Bind(Transform owner, Action<HitEvent> onDealt)
        {
            if (_owner != null)
            {
                throw new InvalidOperationException($"'{name}' is already bound to '{_owner.name}'.");
            }

            _owner = owner != null ? owner : throw new ArgumentNullException(nameof(owner));
            _onDealt = onDealt ?? throw new ArgumentNullException(nameof(onDealt));
            // 소유자의 Awake가 비활성 자식인 이 판정의 Awake보다 먼저 올 수 있어, 쓰기 전에 반드시 거치는 여기서 만든다.
            _onResolved = OnResolved;
        }

        private void OnDisable()
        {
            Close();
        }

        /// <summary>새 스윙의 판정을 연다. 맞은 대상 기록을 비우고, 지금 칼 포즈부터 쓸기 시작한다.</summary>
        /// <exception cref="InvalidOperationException">주인이 정해지지 않았다.</exception>
        public void Open(HitStrength strength, bool knockdown, int damage)
        {
            if (_owner == null)
            {
                throw new InvalidOperationException($"'{name}' has no owner. Call {nameof(Weapon)}.{nameof(Weapon.Bind)} first.");
            }

            _strength = strength;
            _knockdown = knockdown;
            _damage = damage;
            _hitTargets.Clear();

            // 지난 스윙이 끝난 포즈부터 쓸면 그 사이의 대상을 잘못 친다.
            _tracker.Begin();
            IsOpen = true;

#if UNITY_EDITOR
            _debugSweeps.Clear();
#endif
        }

        public void Close()
        {
            IsOpen = false;
        }

        // 칼날 타격의 판정을 받아 스윙 기록을 고친 뒤 소유자에게 넘긴다. 판정을 알리는 것은 HitSystem뿐이다.
        private void OnResolved(HitEvent hitEvent)
        {
            // 흘린 타격은 맞힌 것으로 세지 않아, 무적이 끝난 뒤에도 칼날이 걸쳐 있으면 같은 스윙이 다시 맞힌다.
            // 그 사이 칼이 다시 열렸어도 새 스윙의 기록이라 지워도 해가 없다.
            if (hitEvent.Result.Outcome == HitOutcome.Evaded)
            {
                _hitTargets.Remove(hitEvent.Target);
            }

            _onDealt(hitEvent);
        }

        private void LateUpdate()
        {
            if (!IsOpen)
            {
                return;
            }

            // 자동 동기화가 꺼져 있어, 이번 프레임에 움직인 콜라이더를 쿼리 전에 반영한다.
            Physics.SyncTransforms();

            // 칼날이 실제로 지나간 호를 따라, 검사 간격이 칼날 반지름을 넘지 않게 나눠 검사한다.
            int steps = _tracker.Advance(_radius, _maxSubsteps);
            for (int i = 1; i <= steps; i++)
            {
                _tracker.GetSegment((float)i / steps, out Vector3 start, out Vector3 end);
                Sweep(start, end);
            }
        }

        private void Sweep(Vector3 start, Vector3 end)
        {
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

                // 이 선분은 이번 단계에만 있으므로, 처음 닿은 순간에 몸 표면의 접점을 구해 둔다.
                ImpactPoint impactPoint = ImpactPoint.OnSurface(other, start, end, _owner.position);
                HitSystem.Record(target, new AttackInfo(_owner, _strength, _knockdown, _damage, impactPoint), _onResolved);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 판정 중인 스윙은 빨강, 닫힌 뒤 남은 마지막 스윙은 노랑으로 그린다. 칼날 굵기는 무기가 그린다.
            Gizmos.color = IsOpen ? Color.red : Color.yellow;
            foreach ((Vector3 start, Vector3 end) in _debugSweeps)
            {
                Gizmos.DrawLine(start, end);
            }
        }
#endif
    }
}
