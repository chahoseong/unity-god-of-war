using System;
using System.Collections.Generic;
using GodOfWar.Characters;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 맞은 캐릭터의 몸과 들고 있는 무기를 잠깐 번쩍이게 한다. 언제 반짝일지는 판정을 내린 캐릭터가 정하고, 여기서는 반짝이기만 한다.
    /// 반짝이는 동안만 메쉬 렌더러마다 오버레이 머티리얼 슬롯을 덧붙여 같은 메쉬를 한 번 더 그리므로, 원래 머티리얼은 건드리지 않는다.
    /// 진행은 캐릭터 시계를 따라, 역경직으로 느려진 동안은 반짝임도 그만큼 천천히 사라진다.
    /// 반짝이는 중에 다시 맞으면 피격 반응처럼 가장 최근 타격으로 처음부터 다시 반짝인다.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.HitFlash)]
    [RequireComponent(typeof(CharacterClock))]
    public sealed class HitFlash : MonoBehaviour
    {
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

        [SerializeField] private HitFlashSettings _settings;

        private CharacterClock _clock;
        private Material _overlay;
        private Renderer[] _renderers;

        // 슬롯을 붙이고 뗄 때마다 배열을 만들지 않도록, 렌더러마다 원래 배열과 오버레이를 덧붙인 배열을 미리 만들어 둔다.
        private Material[][] _plainMaterials;
        private Material[][] _flashMaterials;

        private bool _isFlashing;
        private double _startTime;
        private float _duration;
        private float _intensity;

        // 같은 프레임에 받은 요청끼리 비교하려고, 마지막으로 받아들인 요청의 프레임과 세기를 기억한다.
        private int _requestFrame = -1;
        private HitStrength _requestStrength;

        /// <summary>지금 반짝이는 중인지.</summary>
        public bool IsFlashing => _isFlashing;

        private void Awake()
        {
            if (_settings == null)
            {
                throw new InvalidOperationException($"'{name}' has no {nameof(HitFlashSettings)} assigned.");
            }

            if (_settings.OverlayMaterial == null)
            {
                throw new InvalidOperationException($"'{_settings.name}' has no overlay material assigned.");
            }

            // RequireComponent는 이미 만들어진 프리팹에 컴포넌트를 채워 주지 않으므로 직접 확인한다.
            if (!TryGetComponent(out _clock))
            {
                throw new InvalidOperationException($"'{name}' requires a {nameof(CharacterClock)} component.");
            }

            _renderers = FindMeshRenderers();
            _overlay = new Material(_settings.OverlayMaterial) { name = $"{_settings.OverlayMaterial.name} ({name})" };
            _plainMaterials = new Material[_renderers.Length][];
            _flashMaterials = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
            {
                Material[] plain = _renderers[i].sharedMaterials;
                var flash = new Material[plain.Length + 1];
                plain.CopyTo(flash, 0);
                // 머티리얼이 서브메쉬보다 하나 많으면 Unity가 남는 머티리얼로 마지막 서브메쉬를 한 번 더 그린다.
                flash[plain.Length] = _overlay;
                _plainMaterials[i] = plain;
                _flashMaterials[i] = flash;
            }
        }

        private void OnDisable()
        {
            // 꺼진 동안은 진행할 수 없으므로, 슬롯이 붙은 채 남지 않게 바로 끝낸다.
            Stop();
        }

        private void OnDestroy()
        {
            // Awake 검증에서 실패하면 사본을 만들기 전에 멈춘다.
            if (_overlay != null)
            {
                Destroy(_overlay);
            }
        }

        private void Update()
        {
            if (!_isFlashing)
            {
                return;
            }

            float progress = (float)((_clock.Now - _startTime) / _duration);
            if (progress >= 1f)
            {
                Stop();
                return;
            }

            ApplyColor(progress);
        }

        /// <summary>
        /// <paramref name="strength"/> 세기의 타격으로 반짝인다. 요청한 프레임에 바로 보인다.
        /// 앞선 프레임에 시작한 반짝임은 새 요청이 대신한다. 같은 프레임에 여러 요청이 오면 순서와 관계없이 강한 쪽 하나를 따른다.
        /// 지속 시간이 0인 세기는 반짝이지 않고, 진행 중이던 반짝임도 끝낸다. 컴포넌트가 꺼져 있으면 무시한다.
        /// </summary>
        public void Flash(HitStrength strength)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_requestFrame == Time.frameCount && Rank(strength) <= Rank(_requestStrength))
            {
                return;
            }

            _requestFrame = Time.frameCount;
            _requestStrength = strength;
            _duration = _settings.GetDuration(strength);
            if (_duration <= 0f)
            {
                Stop();
                return;
            }

            _intensity = _settings.GetIntensity(strength);
            _startTime = _clock.Now;
            Attach();
            ApplyColor(0f);
        }

        private void Attach()
        {
            if (_isFlashing)
            {
                return;
            }

            _isFlashing = true;
            for (int i = 0; i < _renderers.Length; i++)
            {
                // 렌더러가 붙은 자식이 먼저 파괴됐을 수 있다.
                if (_renderers[i] != null)
                {
                    _renderers[i].sharedMaterials = _flashMaterials[i];
                }
            }
        }

        private void Stop()
        {
            if (!_isFlashing)
            {
                return;
            }

            _isFlashing = false;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].sharedMaterials = _plainMaterials[i];
                }
            }
        }

        private void ApplyColor(float progress)
        {
            float brightness = _intensity * _settings.EvaluateFalloff(progress);
            _overlay.SetColor(FlashColorId, _settings.Color * brightness);
        }

        // 반짝임은 몸과 들고 있는 무기의 표면에만 건다. 파티클·트레일처럼 셰이더가 다른 효과 렌더러는 뺀다.
        // SkinnedMeshRenderer는 MeshRenderer를 상속하지 않으므로 둘을 따로 고른다.
        private Renderer[] FindMeshRenderers()
        {
            var renderers = new List<Renderer>();
            foreach (Renderer candidate in GetComponentsInChildren<Renderer>(true))
            {
                if (candidate is MeshRenderer || candidate is SkinnedMeshRenderer)
                {
                    renderers.Add(candidate);
                }
            }

            if (renderers.Count == 0)
            {
                throw new InvalidOperationException($"'{name}' has no mesh renderer to flash in its children.");
            }

            return renderers.ToArray();
        }

        private static int Rank(HitStrength strength)
        {
            return strength switch
            {
                HitStrength.Light => 0,
                HitStrength.Heavy => 1,
                _ => throw new ArgumentOutOfRangeException(nameof(strength), strength, null)
            };
        }
    }
}
