using System;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 캐릭터 한 명의 무기. 무기에 붙은 부품(<see cref="WeaponHitbox"/> 판정, <see cref="WeaponTrail"/> 궤적)을 스윙에 맞춰 켜고 끈다.
    /// 공격하는 쪽은 스윙을 <see cref="BeginSwing"/>으로 시작하고, 매 프레임 <see cref="SwingTiming"/>과 경과 시간을
    /// <see cref="UpdateSwing"/>에 넘기며, 어떤 경로로 끝나든 <see cref="EndSwing"/>을 부른다.
    /// 경과 시간이 구간에 들어가고 나가는 순간을 이 무기가 판단하므로, 공격하는 쪽은 부품의 상태를 알 필요가 없다.
    /// 패링 카운터처럼 대상이 정해진 타격은 칼날로 찾지 않고 <see cref="Strike"/>로 같은 전달 경로에 넣는다.
    /// 피격자가 내린 판정은 <see cref="Bind"/>로 소유자가 맡긴 콜백으로만 넘긴다.
    /// 타격부 양 끝은 이 무기에만 두고, 부품은 <see cref="CreateTracker"/>로 받아 쓴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Weapon : MonoBehaviour
    {
        [Tooltip("타격부가 시작하는 지점(손잡이 쪽). 이 오브젝트나 그 자식이어야 한다.")]
        [SerializeField] private Transform _base;

        [Tooltip("타격부 끝. 이 오브젝트나 그 자식이어야 한다.")]
        [SerializeField] private Transform _tip;

        [Tooltip("공격 판정. 같은 오브젝트에 있어야 한다.")]
        [SerializeField] private WeaponHitbox _hitbox;

        [Tooltip("검 궤적. 비우면 이 무기는 궤적 없이 판정만 한다.")]
        [SerializeField] private WeaponTrail _trail;

        // 소유자가 Bind로 맡긴 공격자와 판정을 넘길 곳. 무기의 주인은 바뀌지 않는다.
        private Transform _owner;
        private Action<HitEvent> _onDealt;

        private bool _isSwinging;

        private void Awake()
        {
            if (_base == null || _tip == null)
            {
                throw new InvalidOperationException($"'{name}' has no blade base or tip assigned.");
            }

            RequireHitbox();
        }

        /// <summary>
        /// 이 무기의 양 끝을 따라가는 새 <see cref="SwingTracker"/>. 같은 무기에 붙은 부품이 각자 하나씩 만들어 쓴다.
        /// 양 끝 참조는 이 무기에만 둔다.
        /// </summary>
        public SwingTracker CreateTracker()
        {
            return new SwingTracker(transform, _base, _tip);
        }

        /// <summary>
        /// 이 무기의 주인을 정한다. 소유자가 한 번만 호출한다.
        /// <paramref name="owner"/>는 이 무기로 친 타격의 공격자가 되고, 그 타격에 피격자가 내린 판정은
        /// 타격을 전한 같은 프레임 안(모든 LateUpdate 뒤)에 <paramref name="onDealt"/>로 넘긴다. 판정 부품에도 같은 주인을 맡긴다.
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
            // 소유자의 Awake가 비활성 자식인 이 무기의 Awake보다 먼저 올 수 있어 여기서도 확인한다.
            RequireHitbox();
            _hitbox.Bind(owner, onDealt);
        }

        private void OnDisable()
        {
            EndSwing();
        }

        /// <summary>새 스윙을 시작한다. 이전 스윙이 끝나지 않았으면 먼저 끝낸다.</summary>
        public void BeginSwing()
        {
            EndSwing();
            _isSwinging = true;
        }

        /// <summary>
        /// <paramref name="elapsed"/>에 맞춰 판정과 궤적을 켜고 끈다. 공격하는 쪽이 매 프레임 호출한다.
        /// 구간에 들어가면 켜고 나가면 끈다. 경과 시간은 줄지 않으므로 한 스윙에서 각 구간은 한 번만 켜진다.
        /// 무기에 궤적이 없으면 궤적 구간은 무시한다.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="BeginSwing"/> 없이 호출했다.</exception>
        public void UpdateSwing(in SwingTiming timing, float elapsed)
        {
            if (!_isSwinging)
            {
                throw new InvalidOperationException($"'{name}' is not swinging. Call {nameof(BeginSwing)} first.");
            }

            bool isInHitWindow = timing.IsInHitWindow(elapsed);
            if (isInHitWindow && !_hitbox.IsOpen)
            {
                _hitbox.Open(timing.HitStrength);
            }
            else if (!isInHitWindow && _hitbox.IsOpen)
            {
                _hitbox.Close();
            }

            if (_trail == null)
            {
                return;
            }

            bool isInTrailWindow = timing.IsInTrailWindow(elapsed);
            if (isInTrailWindow && !_trail.IsEmitting)
            {
                _trail.Begin(timing.TrailProfile);
            }
            else if (!isInTrailWindow && _trail.IsEmitting)
            {
                _trail.End();
            }
        }

        /// <summary>
        /// 스윙을 끝낸다. 판정을 닫고 궤적의 새 기록을 멈춘다(이미 그린 궤적은 흐려지며 남는다).
        /// 캔슬·피격 등 어떤 경로로 끝나든 공격하는 쪽이 호출한다. 스윙 중이 아니면 아무 일도 하지 않는다.
        /// </summary>
        public void EndSwing()
        {
            if (!_isSwinging)
            {
                return;
            }

            _isSwinging = false;
            _hitbox.Close();
            if (_trail != null)
            {
                _trail.End();
            }
        }

        /// <summary>
        /// 칼날로 찾지 않고 <paramref name="target"/>에게 타격 한 번을 전한다. 스윙·판정 상태와 관계없다.
        /// 칼날로 찾은 타격과 같은 시점(모든 LateUpdate 뒤)에 전하고, 판정도 같은 콜백으로 넘긴다.
        /// </summary>
        /// <exception cref="InvalidOperationException">주인이 정해지지 않았다.</exception>
        public void Strike(IHittable target, HitStrength strength)
        {
            if (_owner == null)
            {
                throw new InvalidOperationException($"'{name}' has no owner. Call {nameof(Bind)} first.");
            }

            HitSystem.Record(
                target ?? throw new ArgumentNullException(nameof(target)), new AttackInfo(_owner, strength), _onDealt);
        }

        private void RequireHitbox()
        {
            if (_hitbox == null)
            {
                throw new InvalidOperationException($"'{name}' has no {nameof(WeaponHitbox)} assigned.");
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_base == null || _tip == null)
            {
                return;
            }

            Gizmos.color = Color.gray;
            Gizmos.DrawLine(_base.position, _tip.position);
            if (_hitbox != null)
            {
                Gizmos.DrawWireSphere(_base.position, _hitbox.Radius);
                Gizmos.DrawWireSphere(_tip.position, _hitbox.Radius);
            }
        }
#endif
    }
}
