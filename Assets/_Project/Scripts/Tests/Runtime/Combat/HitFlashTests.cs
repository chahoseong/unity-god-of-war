// 직렬화 참조를 SerializedObject로 연결하고 오버레이 머티리얼 에셋을 불러오므로 에디터에서만 실행한다.
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
    // 반짝임은 실제 프레임의 캐릭터 시계로 진행하므로, 지속 시간보다 충분히 긴 전역 시간을 기다려 끝난 뒤의 상태를 본다.
    // 진행 중인 밝기는 오버레이 슬롯에 붙은 머티리얼의 색으로 읽는다.
    public class HitFlashTests
    {
        private const string OverlayMaterialPath = "Assets/_Project/Materials/HitFlashOverlay.mat";
        private const float Duration = 0.1f;
        private const float LightIntensity = 1f;
        private const float HeavyIntensity = 2f;
        private const float Tolerance = 1e-4f;

        private GameObject _character;
        private CharacterClock _clock;
        private HitFlash _flash;
        private HitTarget _hitTarget;
        private GameObject _attacker;
        private HitFlashSettings _settings;
        private Material _original;
        private SkinnedMeshRenderer _body;
        private MeshRenderer _weapon;
        private TrailRenderer _trail;

        [SetUp]
        public void SetUp()
        {
            _original = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _settings = CreateSettings();

            // Awake의 설정 검사보다 먼저 연결하도록 비활성 상태로 만든다.
            _character = new GameObject("Character");
            _character.SetActive(false);
            _clock = _character.AddComponent<CharacterClock>();
            _body = _character.AddComponent<SkinnedMeshRenderer>();
            _body.sharedMaterial = _original;

            // 무기는 손 본 아래에 있듯 자식의 자식에 둔다.
            var hand = new GameObject("Hand");
            hand.transform.SetParent(_character.transform, false);
            var weapon = new GameObject("Weapon");
            weapon.transform.SetParent(hand.transform, false);
            weapon.AddComponent<MeshFilter>();
            _weapon = weapon.AddComponent<MeshRenderer>();
            _weapon.sharedMaterial = _original;

            var trail = new GameObject("Trail");
            trail.transform.SetParent(weapon.transform, false);
            _trail = trail.AddComponent<TrailRenderer>();
            _trail.sharedMaterial = _original;

            // RequireComponent가 피격 입구를 함께 붙인다.
            _flash = _character.AddComponent<HitFlash>();
            _hitTarget = _character.GetComponent<HitTarget>();
            _attacker = new GameObject("Attacker");
            var serialized = new SerializedObject(_flash);
            serialized.FindProperty("_settings").objectReferenceValue = _settings;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _character.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            // 파괴를 확인하는 테스트는 캐릭터를 먼저 없앤다.
            if (_character != null)
            {
                Object.DestroyImmediate(_character);
            }

            Object.DestroyImmediate(_attacker);
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_original);
        }

        [UnityTest]
        public IEnumerator Flash_WhenRequested_AddsOverlayToMeshRenderers()
        {
            yield return null;

            _flash.Flash(HitStrength.Light);

            Assert.That(_body.sharedMaterials, Is.EqualTo(new[] { _original, Overlay(_body) }));
            Assert.That(_weapon.sharedMaterials, Is.EqualTo(new[] { _original, Overlay(_body) }));
        }

        // 파티클·트레일처럼 셰이더가 다른 효과 렌더러는 반짝임 대상이 아니다.
        [UnityTest]
        public IEnumerator Flash_WithEffectRendererInChildren_LeavesEffectRendererUntouched()
        {
            yield return null;

            _flash.Flash(HitStrength.Light);

            Assert.That(_trail.sharedMaterials, Is.EqualTo(new[] { _original }));
        }

        // 요청한 프레임에 렌더링되도록 갱신을 기다리지 않고 시작 밝기를 바로 건다.
        [UnityTest]
        public IEnumerator Flash_WhenRequested_AppliesStartBrightnessImmediately()
        {
            yield return null;

            _flash.Flash(HitStrength.Heavy);

            Assert.That(Brightness(), Is.EqualTo(HeavyIntensity).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator Flash_AfterDuration_RestoresOriginalMaterials()
        {
            yield return null;

            _flash.Flash(HitStrength.Light);
            yield return WaitGlobal(Duration * 3f);

            Assert.That(_body.sharedMaterials, Is.EqualTo(new[] { _original }));
            Assert.That(_weapon.sharedMaterials, Is.EqualTo(new[] { _original }));
        }

        // 피격자 시계가 멈춘 동안(역경직)은 전역 시간이 지속 시간을 넘겨도 시작 밝기 그대로 남는다.
        [UnityTest]
        public IEnumerator Flash_WhileClockIsStopped_KeepsStartBrightness()
        {
            yield return null;

            _clock.Slow(1f, 0f);
            _flash.Flash(HitStrength.Light);
            yield return WaitGlobal(Duration * 3f);

            Assert.That(Brightness(), Is.EqualTo(LightIntensity).Within(Tolerance));
        }

        // 피격 반응이 최근 타격으로 바뀌듯, 사라지던 반짝임을 새 타격의 시작 밝기로 되돌린다.
        [UnityTest]
        public IEnumerator Flash_InLaterFrameWhileFlashing_RestartsFromStartBrightness()
        {
            yield return null;

            _flash.Flash(HitStrength.Light);
            yield return WaitGlobal(Duration * 0.5f);
            Assume.That(_flash.IsFlashing, Is.True, "The first flash must still be running.");
            Assume.That(Brightness(), Is.LessThan(LightIntensity), "The first flash must have faded.");
            _flash.Flash(HitStrength.Light);

            Assert.That(Brightness(), Is.EqualTo(LightIntensity).Within(Tolerance));
        }

        // 같은 프레임의 요청 순서는 의미가 없어, 강한 쪽을 따른다.
        [UnityTest]
        public IEnumerator Flash_InSameFrameAfterLight_FollowsHeavy()
        {
            yield return null;

            _flash.Flash(HitStrength.Light);
            _flash.Flash(HitStrength.Heavy);

            Assert.That(Brightness(), Is.EqualTo(HeavyIntensity).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator Flash_InSameFrameAfterHeavy_KeepsHeavy()
        {
            yield return null;

            _flash.Flash(HitStrength.Heavy);
            _flash.Flash(HitStrength.Light);

            Assert.That(Brightness(), Is.EqualTo(HeavyIntensity).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator Disable_WhileFlashing_RestoresOriginalMaterials()
        {
            yield return null;

            _flash.Flash(HitStrength.Light);
            _flash.enabled = false;

            Assert.That(_body.sharedMaterials, Is.EqualTo(new[] { _original }));
            Assert.That(_weapon.sharedMaterials, Is.EqualTo(new[] { _original }));
        }

        // 캐릭터마다 만든 오버레이 사본이 캐릭터와 함께 사라져야 누수가 없다.
        [UnityTest]
        public IEnumerator Destroy_AfterFlash_DestroysOverlayCopy()
        {
            yield return null;

            _flash.Flash(HitStrength.Light);
            Material overlay = Overlay(_body);
            Object.Destroy(_character);
            yield return null;

            Assert.That(overlay == null, Is.True);
        }

        // 판정은 피격 입구에 맡긴 처리자가 정한 대로 나오고, 반짝임은 그 알림만 본다.
        [Test]
        public void HitTaken_WithHitOutcome_FlashesWithAttackStrength()
        {
            _hitTarget.Bind((in AttackInfo _) => new HitResult(HitOutcome.Hit));

            _hitTarget.ReceiveHit(new AttackInfo(_attacker.transform, HitStrength.Heavy));

            Assert.That(Brightness(), Is.EqualTo(HeavyIntensity).Within(Tolerance));
        }

        // 몸에 들어가지 않은 타격은 반짝이지 않는다.
        [TestCase(HitOutcome.Evaded)]
        [TestCase(HitOutcome.Blocked)]
        [TestCase(HitOutcome.GuardBroken)]
        [TestCase(HitOutcome.Parried)]
        public void HitTaken_WithOutcomeOtherThanHit_DoesNotFlash(HitOutcome outcome)
        {
            _hitTarget.Bind((in AttackInfo _) => new HitResult(outcome));

            _hitTarget.ReceiveHit(new AttackInfo(_attacker.transform, HitStrength.Heavy));

            Assert.That(_flash.IsFlashing, Is.False);
        }

        // 꺼진 연출은 듣지 않으므로, 꺼진 채로 슬롯이 붙지 않는다.
        [Test]
        public void HitTaken_WhileDisabled_DoesNotFlash()
        {
            _hitTarget.Bind((in AttackInfo _) => new HitResult(HitOutcome.Hit));
            _flash.enabled = false;

            _hitTarget.ReceiveHit(new AttackInfo(_attacker.transform, HitStrength.Heavy));

            Assert.That(_body.sharedMaterials, Is.EqualTo(new[] { _original }));
        }

        private Material Overlay(Renderer renderer)
        {
            Material[] materials = renderer.sharedMaterials;
            return materials[materials.Length - 1];
        }

        // 색이 흰색이라 빨강 성분이 곧 밝기다. 반짝임이 끝나 오버레이 슬롯이 없으면 밝기는 0이다.
        private float Brightness()
        {
            return _flash.IsFlashing ? Overlay(_body).GetColor("_FlashColor").r : 0f;
        }

        private static HitFlashSettings CreateSettings()
        {
            var settings = ScriptableObject.CreateInstance<HitFlashSettings>();
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("_overlayMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>(OverlayMaterialPath);
            serialized.FindProperty("_color").colorValue = Color.white;
            serialized.FindProperty("_falloff").animationCurveValue = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            serialized.FindProperty("_lightDuration").floatValue = Duration;
            serialized.FindProperty("_lightIntensity").floatValue = LightIntensity;
            serialized.FindProperty("_heavyDuration").floatValue = Duration;
            serialized.FindProperty("_heavyIntensity").floatValue = HeavyIntensity;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        private static IEnumerator WaitGlobal(float seconds)
        {
            double end = Time.timeAsDouble + seconds;
            while (Time.timeAsDouble < end)
            {
                yield return null;
            }
        }
    }
}
#endif
