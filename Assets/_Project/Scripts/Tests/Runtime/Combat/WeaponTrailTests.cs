// 직렬화 참조를 SerializedObject로 연결하므로 에디터에서만 실행한다.
#if UNITY_EDITOR
using System.Collections;
using GodOfWar.Characters;
using GodOfWar.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GodOfWar.Tests.Combat
{
    // 궤적은 실제 프레임의 LateUpdate에서 칼날을 기록하므로, 칼을 프레임마다 돌린 뒤 한 프레임을 더 넘겨 결과를 본다.
    // 띠는 궤적이 따로 만든 렌더러에 그려지므로, 이 테스트의 머티리얼을 쓰는 렌더러를 찾아 그 메쉬를 읽는다.
    public class WeaponTrailTests
    {
        // 테스트 프레임이 길어져도 띠 두 개를 그리는 동안 앞 띠가 사라지지 않을 만큼 길게 둔다.
        private const float Lifetime = 1f;
        private const float BaseHeight = 0.1f;
        private const float TipHeight = 1f;
        private const float Tolerance = 1e-3f;

        private GameObject _character;
        private CharacterClock _clock;
        private Transform _blade;
        private Transform _tip;
        private WeaponTrail _trail;
        private WeaponTrailProfile _profile;
        private Material _material;

        [SetUp]
        public void SetUp()
        {
            _material = new Material(Shader.Find("GodOfWar/WeaponTrail"));
            _profile = ScriptableObject.CreateInstance<WeaponTrailProfile>();
            var profile = new SerializedObject(_profile);
            profile.FindProperty("_lifetime").floatValue = Lifetime;
            profile.ApplyModifiedPropertiesWithoutUndo();

            // Awake의 설정 검사보다 먼저 연결하도록 비활성 상태로 만든다.
            _character = new GameObject("Character");
            _character.SetActive(false);
            _clock = _character.AddComponent<CharacterClock>();

            // 무기는 손 본 아래에 있듯 자식의 자식에 둔다.
            var hand = new GameObject("Hand");
            hand.transform.SetParent(_character.transform, false);
            _blade = new GameObject("Blade").transform;
            _blade.SetParent(hand.transform, false);
            Transform bladeBase = CreatePoint("BladeBase", BaseHeight);
            _tip = CreatePoint("BladeTip", TipHeight);

            _trail = _blade.gameObject.AddComponent<WeaponTrail>();
            var trail = new SerializedObject(_trail);
            trail.FindProperty("_base").objectReferenceValue = bladeBase;
            trail.FindProperty("_tip").objectReferenceValue = _tip;
            trail.FindProperty("_material").objectReferenceValue = _material;
            trail.ApplyModifiedPropertiesWithoutUndo();
            _character.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            // 궤적이 따로 만든 렌더러는 궤적이 파괴될 때 함께 정리되도록 즉시 파괴한다.
            Object.DestroyImmediate(_character);
            foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>())
            {
                if (renderer.sharedMaterial == _material)
                {
                    Object.DestroyImmediate(renderer.gameObject);
                }
            }

            Object.DestroyImmediate(_profile);
            Object.DestroyImmediate(_material);
        }

        [UnityTest]
        public IEnumerator Begin_WhileBladeRotates_DrawsRibbonAlongBladeArc()
        {
            _trail.Begin(_profile);
            yield return Swing(3, 40f);

            Mesh mesh = RibbonMesh();
            Vector3[] vertices = mesh.vertices;
            // 프레임 사이를 보간해 단면이 프레임 수(첫 단면 포함 4개)보다 많다.
            Assert.That(vertices.Length / 2, Is.GreaterThan(4));
            // 회전을 보간했으므로 모든 단면이 칼날 길이를 유지한다. 양 끝을 직선으로 이었다면 중간 단면이 짧아진다.
            for (int i = 0; i < vertices.Length; i += 2)
            {
                Assert.That(Vector3.Distance(vertices[i], vertices[i + 1]), Is.EqualTo(TipHeight - BaseHeight).Within(Tolerance));
            }

            // 마지막 단면은 지금 칼끝에 있다.
            Assert.That(Vector3.Distance(vertices[vertices.Length - 1], _tip.position), Is.LessThan(Tolerance));
        }

        [UnityTest]
        public IEnumerator End_AfterLifetimeElapses_RemovesRibbon()
        {
            _trail.Begin(_profile);
            yield return Swing(3, 40f);
            _trail.End();
            Assume.That(IsDrawn(), Is.True);

            yield return new WaitForSeconds(Lifetime * 2f);

            Assert.That(_trail.IsEmitting, Is.False);
            Assert.That(IsDrawn(), Is.False);
        }

        [UnityTest]
        public IEnumerator Begin_AfterEnd_StartsSeparateRibbon()
        {
            _trail.Begin(_profile);
            yield return Swing(2, 40f);
            _trail.End();
            yield return Swing(2, -60f);
            _trail.Begin(_profile);
            yield return Swing(2, 40f);

            AssertTwoSeparateRibbons(RibbonMesh());
        }

        [UnityTest]
        public IEnumerator Begin_WhileEmitting_StartsSeparateRibbon()
        {
            _trail.Begin(_profile);
            yield return Swing(2, 40f);
            _trail.Begin(_profile);
            yield return Swing(2, 40f);

            AssertTwoSeparateRibbons(RibbonMesh());
        }

        [UnityTest]
        public IEnumerator End_WhileClockSlowed_FadesAtClockSpeed()
        {
            // 띠의 수명보다 훨씬 길게, 전역 시간의 1/10로 느리게 한다.
            _clock.Slow(10f, 0.1f);
            yield return null;
            _trail.Begin(_profile);
            yield return Swing(3, 40f);
            _trail.End();

            // 전역 시간으로는 수명을 넘겼지만 캐릭터 시계로는 그 1/10만 흘렀다.
            yield return new WaitForSeconds(Lifetime * 2f);

            Assert.That(IsDrawn(), Is.True);
        }

        [UnityTest]
        public IEnumerator Disable_WithRibbon_ClearsRibbon()
        {
            _trail.Begin(_profile);
            yield return Swing(3, 40f);
            Assume.That(IsDrawn(), Is.True);

            _trail.enabled = false;

            Assert.That(_trail.IsEmitting, Is.False);
            Assert.That(IsDrawn(), Is.False);
        }

        // 씬을 내릴 때처럼, 캐릭터 밖에 있는 띠 렌더러가 궤적보다 먼저 파괴된 경우.
        [UnityTest]
        public IEnumerator Disable_AfterRibbonRendererDestroyed_LogsNoError()
        {
            _trail.Begin(_profile);
            yield return Swing(3, 40f);
            Object.DestroyImmediate(RibbonRenderer().gameObject);

            _trail.enabled = false;

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void End_WithoutBegin_DoesNothing()
        {
            Assert.DoesNotThrow(() => _trail.End());
            Assert.That(_trail.IsEmitting, Is.False);
            Assert.That(IsDrawn(), Is.False);
        }

        // 띠 두 개면 이웃 단면 사이 사각형 수가 전체 단면 수보다 2 적다. 두 띠를 이었다면 1 적다.
        private static void AssertTwoSeparateRibbons(Mesh mesh)
        {
            int sections = mesh.vertexCount / 2;
            int quads = mesh.triangles.Length / 6;
            Assert.That(quads, Is.EqualTo(sections - 2));
        }

        // 칼을 손잡이 기준으로 프레임마다 돌리고, 마지막 회전을 기록하도록 한 프레임을 더 넘긴다.
        private IEnumerator Swing(int frames, float degreesPerFrame)
        {
            for (int i = 0; i < frames; i++)
            {
                _blade.Rotate(0f, 0f, degreesPerFrame, Space.Self);
                yield return null;
            }

            yield return null;
        }

        private Transform CreatePoint(string pointName, float height)
        {
            var point = new GameObject(pointName).transform;
            point.SetParent(_blade, false);
            point.localPosition = new Vector3(0f, height, 0f);
            return point;
        }

        private MeshRenderer RibbonRenderer()
        {
            foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>())
            {
                if (renderer.sharedMaterial == _material)
                {
                    return renderer;
                }
            }

            Assert.Fail("The trail has no ribbon renderer.");
            return null;
        }

        private Mesh RibbonMesh()
        {
            return RibbonRenderer().GetComponent<MeshFilter>().sharedMesh;
        }

        private bool IsDrawn()
        {
            MeshRenderer renderer = RibbonRenderer();
            return renderer.enabled && renderer.GetComponent<MeshFilter>().sharedMesh.triangles.Length > 0;
        }
    }
}
#endif
