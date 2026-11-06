using System;
using GodOfWar.Effects;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 맞은 캐릭터의 타격 소리를 낸다. 같은 캐릭터의 <see cref="HitTarget"/>이 알리는 판정·세기로 설정에서 소리를 골라,
    /// 타격이 닿은 지점에서 공유 재생기(<see cref="SoundPlayer"/>)로 낸다. 넉다운시키는 타격이면 넉다운 소리를 덧붙인다.
    /// 판정은 모르고 결과만 관찰하는 연출이라, 켜져 있는 동안만 듣는다. 흘린 타격(<see cref="HitOutcome.Evaded"/>)에는 소리를 내지 않는다.
    /// 낸 소리는 재생기에 맡기므로 캐릭터가 꺼지거나 사라져도 끝까지 난다. 캐릭터 시계(역경직)는 따르지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HitTarget))]
    public sealed class HitSounds : MonoBehaviour
    {
        [SerializeField] private HitSoundSettings _settings;

        private HitTarget _hitTarget;

        private void Awake()
        {
            if (_settings == null)
            {
                throw new InvalidOperationException($"'{name}' has no {nameof(HitSoundSettings)} assigned.");
            }

            // RequireComponent는 이미 만들어진 프리팹에 컴포넌트를 채워 주지 않으므로 직접 확인한다.
            if (!TryGetComponent(out _hitTarget))
            {
                throw new InvalidOperationException($"'{name}' requires a {nameof(HitTarget)} component.");
            }
        }

        private void OnEnable()
        {
            // Awake 검증에서 실패하면 Unity가 컴포넌트를 꺼 여기까지 오지 않지만, 찾지 못한 채 구독하지 않도록 확인한다.
            if (_hitTarget != null)
            {
                _hitTarget.HitTaken += OnHitTaken;
            }
        }

        private void OnDisable()
        {
            if (_hitTarget != null)
            {
                _hitTarget.HitTaken -= OnHitTaken;
            }
        }

        private void OnHitTaken(HitEvent hitEvent)
        {
            AttackInfo attack = hitEvent.Attack;
            HitOutcome outcome = hitEvent.Result.Outcome;
            Vector3 position = attack.ImpactPoint.Position;
            if (_settings.TryGet(outcome, attack.Strength, out SoundCue cue))
            {
                SoundPlayer.Play(cue, position, _settings.SpatialBlend);
            }

            if (_settings.TryGetKnockdown(outcome, attack.Knockdown, out SoundCue knockdown))
            {
                SoundPlayer.Play(knockdown, position, _settings.SpatialBlend);
            }
        }
    }
}
