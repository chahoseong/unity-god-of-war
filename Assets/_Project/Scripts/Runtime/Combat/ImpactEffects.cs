using System;
using System.Collections.Generic;
using GodOfWar.Effects;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 맞은 캐릭터의 몸에 타격 이펙트를 띄운다. 같은 캐릭터의 <see cref="HitTarget"/>이 알리는 판정·세기로 설정에서 프리팹을 골라,
    /// 타격의 접점(<see cref="AttackInfo.ImpactPoint"/>)에서 법선 방향으로 재생한다. 판정은 모르고 결과만 관찰하는 연출이라, 켜져 있는 동안만 듣는다.
    /// 이펙트는 월드에 남는 사건이라 캐릭터 시계(역경직)를 따르지 않고, 캐릭터가 사라져도 끝까지 재생한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HitTarget))]
    public sealed class ImpactEffects : MonoBehaviour
    {
        // 칸마다 미리 만들어 둘 인스턴스 수. 연타로 앞 이펙트가 끝나기 전에 다음 타격이 오는 정도를 덮는다.
        private const int PrewarmCount = 2;

        [SerializeField] private ImpactEffectSettings _settings;

        private HitTarget _hitTarget;

        private void Awake()
        {
            if (_settings == null)
            {
                throw new InvalidOperationException($"'{name}' has no {nameof(ImpactEffectSettings)} assigned.");
            }

            // RequireComponent는 이미 만들어진 프리팹에 컴포넌트를 채워 주지 않으므로 직접 확인한다.
            if (!TryGetComponent(out _hitTarget))
            {
                throw new InvalidOperationException($"'{name}' requires a {nameof(HitTarget)} component.");
            }

            var prefabs = new List<ParticleSystem>();
            _settings.CollectPrefabs(prefabs);
            foreach (ParticleSystem prefab in prefabs)
            {
                EffectPool.Prewarm(prefab, PrewarmCount);
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
            if (!_settings.TryGet(hitEvent.Result.Outcome, hitEvent.Attack.Strength, out ParticleSystem prefab))
            {
                return;
            }

            ImpactPoint impact = hitEvent.Attack.ImpactPoint;
            Quaternion rotation = impact.Normal != Vector3.zero ? Quaternion.LookRotation(impact.Normal) : Quaternion.identity;
            EffectPool.Play(prefab, impact.Position, rotation);
        }
    }
}
