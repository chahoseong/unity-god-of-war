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
    /// 검 궤적은 같은 무기에 붙은 <see cref="WeaponTrail"/>에 맡기며, 공격하는 쪽은 이 무기를 통해서만 켜고 끈다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Weapon : MonoBehaviour
    {
        private const int MaxOverlaps = 16;

        [Tooltip("타격부가 시작하는 지점(손잡이 쪽). 이 오브젝트나 그 자식이어야 한다.")]
        [SerializeField] private Transform _base;

        [Tooltip("타격부 끝. 이 오브젝트나 그 자식이어야 한다.")]
        [SerializeField] private Transform _tip;

        [Tooltip("칼날 캡슐의 반지름(m). 프레임 사이를 보간하는 간격의 기준이기도 하다.")]
        [SerializeField, Min(0.01f)] private float _radius = 0.05f;

        [Tooltip("타격 대상을 찾을 레이어.")]
        [SerializeField] private LayerMask _targetLayers = ~0;

        [Tooltip("한 프레임에 보간하는 최대 단계 수. 프레임이 크게 끊길 때의 비용 상한이다.")]
        [SerializeField, Min(1)] private int _maxSubsteps = 16;

        [Tooltip("검 궤적. 비우면 이 무기는 궤적 없이 판정만 한다.")]
        [SerializeField] private WeaponTrail _trail;

        private readonly Collider[] _overlaps = new Collider[MaxOverlaps];
        private readonly HashSet<IHittable> _hitTargets = new HashSet<IHittable>();

        // 소유자가 Bind로 맡긴 공격자와 판정을 넘길 곳. 무기의 주인은 바뀌지 않는다.
        private Transform _owner;
        private Action<HitEvent> _onDealt;
        // 칼날 타격의 판정을 받는 콜백. 타격마다 할당하지 않도록 한 번만 만든다.
        private Action<HitEvent> _onSwingResolved;

        private HitStrength _strength;
        private SwingTracker _tracker;

#if UNITY_EDITOR
        // 판정 구간을 튜닝할 때 볼 수 있도록, 마지막 스윙에서 검사한 칼날 선분을 다음 스윙이 열릴 때까지 남긴다.
        // 프레임이 아주 잘게 나뉘어도 무한히 쌓이지 않게 상한을 둔다.
        private const int MaxDebugSweeps = 1024;
        private readonly List<(Vector3 start, Vector3 end)> _debugSweeps = new List<(Vector3, Vector3)>();
#endif

        public bool IsOpen { get; private set; }

        /// <summary>지금 검 궤적을 그리는 중인지. 궤적이 없는 무기는 항상 아니다.</summary>
        public bool IsTrailEmitting => _trail != null && _trail.IsEmitting;

        private void Awake()
        {
            if (_base == null || _tip == null)
            {
                throw new InvalidOperationException($"'{name}' has no blade base or tip assigned.");
            }

            _tracker = CreateTracker();
        }

        /// <summary>
        /// 이 무기의 양 끝을 따라가는 새 <see cref="SwingTracker"/>. 같은 무기에 붙은 연출 부품이 각자 하나씩 만들어 쓴다.
        /// 양 끝 참조는 이 무기에만 둔다.
        /// </summary>
        public SwingTracker CreateTracker()
        {
            return new SwingTracker(transform, _base, _tip);
        }

        /// <summary>
        /// 이 무기의 주인을 정한다. 소유자가 한 번만 호출한다.
        /// <paramref name="owner"/>는 이 무기로 친 타격의 공격자가 되고, 그 타격에 피격자가 내린 판정은
        /// 타격을 전한 같은 프레임 안(모든 LateUpdate 뒤)에 <paramref name="onDealt"/>로 넘긴다.
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

        /// <summary><paramref name="profile"/> 외형으로 새 검 궤적을 그리기 시작한다. 궤적이 없는 무기는 아무 일도 하지 않는다.</summary>
        public void BeginTrail(WeaponTrailProfile profile)
        {
            if (_trail != null)
            {
                _trail.Begin(profile);
            }
        }

        /// <summary>검 궤적의 새 기록을 멈춘다. 이미 그린 궤적은 흐려지며 남는다. 궤적이 없는 무기는 아무 일도 하지 않는다.</summary>
        public void EndTrail()
        {
            if (_trail != null)
            {
                _trail.End();
            }
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

        private void Submit(IHittable target, HitStrength strength, Action<HitEvent> onResolved)
        {
            HitSystem.Record(target, new AttackInfo(_owner, strength), onResolved);
        }

        // 칼날 타격의 판정을 받아 스윙 기록을 고친 뒤 소유자에게 넘긴다. 판정을 알리는 것은 HitSystem뿐이다.
        private void OnSwingResolved(HitEvent hitEvent)
        {
            // 흘린 타격은 맞힌 것으로 세지 않아, 무적이 끝난 뒤에도 칼날이 걸쳐 있으면 같은 스윙이 다시 맞힌다.
            // 그 사이 칼이 다시 열렸어도 새 스윙의 기록이라 지워도 해가 없다.
            if (hitEvent.Result.Outcome == HitOutcome.Evaded)
            {
                _hitTargets.Remove(hitEvent.Target);
            }

            _onDealt(hitEvent);
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
