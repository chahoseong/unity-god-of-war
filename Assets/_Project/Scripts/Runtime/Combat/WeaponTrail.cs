using System;
using System.Collections.Generic;
using GodOfWar.Characters;
using UnityEngine;
using UnityEngine.Rendering;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 칼날이 지나간 자리에 남는 검 궤적. 켜져 있는 동안 매 프레임 칼날 양 끝을 기록해, 그 사이를 띠(리본)로 잇는다.
    /// 끄면 새 기록만 멈추고, 이미 그린 띠는 외형의 수명이 다할 때까지 흐려지며 남는다. 다시 켜면 앞 띠와 잇지 않고 새 띠로 시작한다.
    /// 띠의 나이는 이 무기를 든 캐릭터의 시계로 재므로, 역경직으로 느려진 동안은 띠도 그만큼 천천히 사라진다.
    /// 칼날 양 끝은 같은 오브젝트의 <see cref="Weapon"/>에서 받고, 프레임 사이는 <see cref="SwingTracker"/>로 보간해
    /// 빠른 스윙에서도 띠가 실제로 지나간 호를 따르게 한다.
    /// 띠는 캐릭터 밖 월드 원점에 따로 만든 렌더러로 그려, 정점을 월드 좌표 그대로 쓰고 캐릭터 렌더러를 모으는 연출에 섞이지 않는다.
    /// 애니메이션이 칼을 옮긴 뒤에 기록하도록 LateUpdate에서 갱신한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Weapon))]
    public sealed class WeaponTrail : MonoBehaviour
    {
        [Tooltip("띠를 그릴 머티리얼. 색은 정점 색으로 넘기므로 모든 띠가 이 머티리얼 하나를 공유한다.")]
        [SerializeField] private Material _material;

        [Tooltip("프레임 사이에 단면을 채우는 간격(m). 칼날 양 끝 중 더 많이 움직인 쪽이 이 간격을 넘지 않도록 나눈다. 작을수록 호가 매끄럽다.")]
        [SerializeField, Min(0.01f)] private float _sectionSpacing = 0.05f;

        [Tooltip("한 프레임에 채우는 최대 단면 수. 프레임이 크게 끊길 때의 비용 상한이다.")]
        [SerializeField, Min(1)] private int _maxSubsteps = 16;

        private readonly List<Strip> _strips = new List<Strip>();

        // 메쉬를 다시 만들 때마다 할당하지 않도록 버퍼를 재사용한다.
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<Vector3> _uvs = new List<Vector3>();
        private readonly List<int> _indices = new List<int>();

        private CharacterClock _clock;
        private GameObject _rendererObject;
        private MeshRenderer _renderer;
        private Mesh _mesh;
        private SwingTracker _tracker;

        // 새 단면이 붙는 띠. 꺼져 있으면 null이다.
        private Strip _current;
        private double _previousTime;

        /// <summary>지금 새 띠를 그리는 중인지.</summary>
        public bool IsEmitting => _current != null;

        private void Awake()
        {
            if (_material == null)
            {
                throw new InvalidOperationException($"'{name}' has no trail material assigned.");
            }

            _clock = GetComponentInParent<CharacterClock>();
            if (_clock == null)
            {
                throw new InvalidOperationException($"'{name}' requires a {nameof(CharacterClock)} in its parents.");
            }

            // 무기의 Awake보다 먼저 올 수 있지만, 양 끝은 직렬화된 참조라 이미 연결되어 있다.
            _tracker = GetComponent<Weapon>().CreateTracker();

            _mesh = new Mesh { name = $"{name} Trail" };
            _mesh.MarkDynamic();

            // 월드 원점에 두어 오브젝트 좌표가 곧 월드 좌표가 되게 한다. 캐릭터의 자식이 아니므로 칼을 따라 움직이지 않는다.
            _rendererObject = new GameObject($"{name} Trail");
            _rendererObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = _rendererObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = _material;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _renderer.enabled = false;
        }

        private void OnDisable()
        {
            // 꺼진 동안은 띠를 흐려지게 할 수 없으므로, 공중에 멈춘 채 남지 않게 바로 지운다.
            _current = null;
            _strips.Clear();
            RebuildMesh();
        }

        private void OnDestroy()
        {
            // Awake 검증에서 실패하면 렌더러를 만들기 전에 멈춘다.
            if (_rendererObject != null)
            {
                Destroy(_rendererObject);
            }

            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }

        /// <summary>
        /// <paramref name="profile"/> 외형으로 새 띠를 그리기 시작한다. 지금 칼날 위치가 띠의 첫 단면이 된다.
        /// 그리던 띠가 있으면 끝내고, 앞 띠와 잇지 않는다. 컴포넌트가 꺼져 있으면 무시한다.
        /// </summary>
        public void Begin(WeaponTrailProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (!isActiveAndEnabled)
            {
                return;
            }

            End();

            _tracker.Begin();
            _previousTime = _clock.Now;

            _current = new Strip(profile);
            _strips.Add(_current);
            AddSection(1f, _previousTime);
        }

        /// <summary>새 기록을 멈춘다. 이미 그린 띠는 수명이 다할 때까지 흐려지며 남는다. 그리는 중이 아니면 아무 일도 하지 않는다.</summary>
        public void End()
        {
            _current = null;
        }

        private void LateUpdate()
        {
            if (_current != null)
            {
                Record();
            }

            RemoveExpired();
            RebuildMesh();
        }

        // 지난 프레임의 포즈부터 지금 포즈까지를 보간해 단면을 붙인다. 단면의 생성 시각도 같은 비율로 나눠 나이가 고르게 늘게 한다.
        private void Record()
        {
            double now = _clock.Now;
            int steps = _tracker.Advance(_sectionSpacing, _maxSubsteps);
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                AddSection(t, _previousTime + (now - _previousTime) * t);
            }

            _previousTime = now;
        }

        // 보간 구간의 t 시점 칼날 양 끝을 지금 띠의 단면으로 붙인다.
        private void AddSection(float t, double time)
        {
            _tracker.GetSegment(t, out Vector3 start, out Vector3 end);
            _current.Sections.Add(new Section(start, end, time));
        }

        // 수명이 다한 단면을 오래된 쪽부터 뗀다. 그리기를 마친 띠가 단면 두 개 미만이 되면 더 그릴 면이 없으므로 버린다.
        private void RemoveExpired()
        {
            double now = _clock.Now;
            for (int i = _strips.Count - 1; i >= 0; i--)
            {
                Strip strip = _strips[i];
                float lifetime = strip.Profile.Lifetime;
                int expired = 0;
                while (expired < strip.Sections.Count && now - strip.Sections[expired].Time >= lifetime)
                {
                    expired++;
                }

                strip.Sections.RemoveRange(0, expired);
                if (strip != _current && strip.Sections.Count < 2)
                {
                    _strips.RemoveAt(i);
                }
            }
        }

        // 띠마다 이웃한 단면 사이를 사각형(삼각형 두 개)으로 잇는다. 띠끼리는 잇지 않는다.
        // 색은 외형 색에 나이에 따른 밝기 비율을 곱해 정점 색으로 넘긴다.
        // 폭 방향 감쇠는 픽셀마다 곡선으로 계산해야 하므로, 폭 위치(손잡이 0, 칼끝 1)와 외형의 감쇠 지수를 UV(y, z)로 넘긴다.
        private void RebuildMesh()
        {
            _vertices.Clear();
            _colors.Clear();
            _uvs.Clear();
            _indices.Clear();

            double now = _clock != null ? _clock.Now : 0d;
            foreach (Strip strip in _strips)
            {
                if (strip.Sections.Count < 2)
                {
                    continue;
                }

                WeaponTrailProfile profile = strip.Profile;
                int first = _vertices.Count;
                foreach (Section section in strip.Sections)
                {
                    float progress = profile.Lifetime > 0f ? (float)((now - section.Time) / profile.Lifetime) : 1f;
                    Color color = profile.Color * profile.EvaluateFade(progress);
                    _vertices.Add(section.Base);
                    _vertices.Add(section.Tip);
                    _colors.Add(color);
                    _colors.Add(color);
                    _uvs.Add(new Vector3(0f, 0f, profile.WidthFalloff));
                    _uvs.Add(new Vector3(0f, 1f, profile.WidthFalloff));
                }

                for (int i = 0; i < strip.Sections.Count - 1; i++)
                {
                    int baseIndex = first + i * 2;
                    _indices.Add(baseIndex);
                    _indices.Add(baseIndex + 2);
                    _indices.Add(baseIndex + 1);
                    _indices.Add(baseIndex + 1);
                    _indices.Add(baseIndex + 2);
                    _indices.Add(baseIndex + 3);
                }
            }

            // Awake 검증에서 실패하면 메쉬를 만들기 전에 꺼진다.
            // 렌더러 오브젝트는 캐릭터 밖에 있어, 씬을 내릴 때 이 컴포넌트보다 먼저 파괴될 수 있다.
            if (_mesh == null || _renderer == null)
            {
                return;
            }

            _mesh.Clear();
            bool hasRibbon = _indices.Count > 0;
            if (hasRibbon)
            {
                _mesh.SetVertices(_vertices);
                _mesh.SetColors(_colors);
                _mesh.SetUVs(0, _uvs);
                _mesh.SetTriangles(_indices, 0, calculateBounds: true);
            }

            _renderer.enabled = hasRibbon;
        }

        private sealed class Strip
        {
            public readonly WeaponTrailProfile Profile;
            public readonly List<Section> Sections = new List<Section>();

            public Strip(WeaponTrailProfile profile)
            {
                Profile = profile;
            }
        }

        private readonly struct Section
        {
            public readonly Vector3 Base;
            public readonly Vector3 Tip;
            public readonly double Time;

            public Section(Vector3 @base, Vector3 tip, double time)
            {
                Base = @base;
                Tip = tip;
                Time = time;
            }
        }
    }
}
