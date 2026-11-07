// 직렬화 참조를 SerializedObject로 연결하므로 에디터에서만 실행한다.
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Effects;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GodOfWar.Tests.Combat
{
    // 무기가 스윙 타이밍에 맞춰 부품을 켜고 끄고 소리를 내는지 본다. 부품이 실제로 검출하고 그리는 일은 각 부품의 테스트가 본다.
    public class WeaponTests
    {
        private const float HitStart = 1f;
        private const float HitEnd = 2f;
        private const float TrailStart = 0.5f;
        private const float TrailEnd = 2.5f;
        private const float SwingSoundTime = 0.8f;

        private readonly List<HitEvent> _dealt = new List<HitEvent>();
        private GameObject _character;
        private Weapon _weapon;
        private Transform _strikePoint;
        private WeaponHitbox _hitbox;
        private WeaponTrail _trail;
        private WeaponTrailProfile _profile;
        private Material _material;
        private SwingTiming _timing;
        private AudioClip _swingClip;
        private SwingTiming _soundTiming;

        [SetUp]
        public void SetUp()
        {
            _material = new Material(Shader.Find("GodOfWar/WeaponTrail"));
            _profile = ScriptableObject.CreateInstance<WeaponTrailProfile>();
            _timing = new SwingTiming(HitStart, HitEnd, HitStrength.Heavy, false, 0, TrailStart, TrailEnd, _profile);
            // 무음 10초. 테스트 동안 재생이 끝나지 않는다.
            _swingClip = AudioClip.Create("Swing", 44100 * 10, 1, 44100, false);
            _soundTiming = new SwingTiming(
                HitStart, HitEnd, HitStrength.Heavy, false, 0, TrailStart, TrailEnd, _profile,
                new SoundCue(new[] { _swingClip }, 1f, 1f, 1f), SwingSoundTime);
            _dealt.Clear();
            CreateWeapon(withTrail: true);
        }

        [TearDown]
        public void TearDown()
        {
            // 궤적이 캐릭터 밖에 만든 렌더러도 함께 지운다.
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
            Object.DestroyImmediate(_swingClip);
            Object.DestroyImmediate(GameObject.Find(nameof(SoundPlayer)));
        }

        [Test]
        public void UpdateSwing_InsideHitWindow_OpensHitbox()
        {
            _weapon.BeginSwing();

            _weapon.UpdateSwing(_timing, HitStart);

            Assert.That(_hitbox.IsOpen, Is.True);
        }

        [Test]
        public void UpdateSwing_AfterHitWindow_ClosesHitbox()
        {
            _weapon.BeginSwing();
            _weapon.UpdateSwing(_timing, HitStart);

            _weapon.UpdateSwing(_timing, HitEnd);

            Assert.That(_hitbox.IsOpen, Is.False);
        }

        [Test]
        public void UpdateSwing_InsideTrailWindow_StartsTrail()
        {
            _weapon.BeginSwing();

            _weapon.UpdateSwing(_timing, TrailStart);

            Assert.That(_trail.IsEmitting, Is.True);
        }

        [Test]
        public void UpdateSwing_AfterTrailWindow_EndsTrail()
        {
            _weapon.BeginSwing();
            _weapon.UpdateSwing(_timing, TrailStart);

            _weapon.UpdateSwing(_timing, TrailEnd);

            Assert.That(_trail.IsEmitting, Is.False);
        }

        // 패링 카운터처럼 판정 구간이 없는 스윙은 궤적만 켜고 판정은 열지 않는다.
        [Test]
        public void UpdateSwing_WithoutHitWindow_StartsTrailOnly()
        {
            SwingTiming timing = SwingTiming.WithoutHit(TrailStart, TrailEnd, _profile);
            _weapon.BeginSwing();

            _weapon.UpdateSwing(timing, HitStart);

            Assert.That(_hitbox.IsOpen, Is.False);
            Assert.That(_trail.IsEmitting, Is.True);
        }

        [Test]
        public void UpdateSwing_OnWeaponWithoutTrail_OpensHitbox()
        {
            Object.DestroyImmediate(_character);
            CreateWeapon(withTrail: false);
            _weapon.BeginSwing();

            Assert.DoesNotThrow(() => _weapon.UpdateSwing(_timing, HitStart));
            Assert.That(_hitbox.IsOpen, Is.True);
        }

        // 공격 상태가 캔슬·피격으로 구간 도중에 나가도 판정과 궤적이 켜진 채 남지 않아야 한다.
        [Test]
        public void EndSwing_InsideWindows_ClosesHitboxAndEndsTrail()
        {
            _weapon.BeginSwing();
            _weapon.UpdateSwing(_timing, HitStart);

            _weapon.EndSwing();

            Assert.That(_hitbox.IsOpen, Is.False);
            Assert.That(_trail.IsEmitting, Is.False);
        }

        [Test]
        public void BeginSwing_WhileSwinging_EndsPreviousSwing()
        {
            _weapon.BeginSwing();
            _weapon.UpdateSwing(_timing, HitStart);

            _weapon.BeginSwing();

            Assert.That(_hitbox.IsOpen, Is.False);
            Assert.That(_trail.IsEmitting, Is.False);
        }

        [Test]
        public void Disable_DuringSwing_ClosesHitboxAndEndsTrail()
        {
            _weapon.BeginSwing();
            _weapon.UpdateSwing(_timing, HitStart);

            _weapon.enabled = false;

            Assert.That(_hitbox.IsOpen, Is.False);
            Assert.That(_trail.IsEmitting, Is.False);
        }

        [Test]
        public void UpdateSwing_WithoutBeginSwing_ThrowsInvalidOperationException()
        {
            Assert.Throws<System.InvalidOperationException>(() => _weapon.UpdateSwing(_timing, HitStart));
        }

        [Test]
        public void UpdateSwing_BeforeSwingSoundTime_PlaysNoSound()
        {
            _weapon.BeginSwing();

            _weapon.UpdateSwing(_soundTiming, TrailStart);

            Assert.That(CountPlayingSounds(), Is.EqualTo(0));
        }

        [Test]
        public void UpdateSwing_AtSwingSoundTime_PlaysSwingSound()
        {
            _weapon.BeginSwing();

            _weapon.UpdateSwing(_soundTiming, SwingSoundTime);

            Assert.That(CountPlayingSounds(), Is.EqualTo(1));
        }

        // 소리를 낼 시각을 지난 뒤로도 매 프레임 부르므로, 같은 스윙에서 다시 내면 소리가 겹친다.
        [Test]
        public void UpdateSwing_AfterSwingSoundPlayed_DoesNotPlaySoundAgain()
        {
            _weapon.BeginSwing();
            _weapon.UpdateSwing(_soundTiming, SwingSoundTime);

            _weapon.UpdateSwing(_soundTiming, HitStart);

            Assert.That(CountPlayingSounds(), Is.EqualTo(1));
        }

        // 콤보의 다음 타는 새 스윙이므로 다시 소리를 낸다. 앞 소리는 끊지 않고 겹친다.
        [Test]
        public void UpdateSwing_AtSwingSoundTimeOfNextSwing_PlaysSoundAgain()
        {
            _weapon.BeginSwing();
            _weapon.UpdateSwing(_soundTiming, SwingSoundTime);
            _weapon.BeginSwing();

            _weapon.UpdateSwing(_soundTiming, SwingSoundTime);

            Assert.That(CountPlayingSounds(), Is.EqualTo(2));
        }

        // 칼은 이미 휘둘렸으므로, 캔슬로 스윙이 끝나도 난 소리는 잔향까지 그대로 둔다.
        [Test]
        public void EndSwing_AfterSwingSoundPlayed_KeepsSoundPlaying()
        {
            _weapon.BeginSwing();
            _weapon.UpdateSwing(_soundTiming, SwingSoundTime);

            _weapon.EndSwing();

            Assert.That(CountPlayingSounds(), Is.EqualTo(1));
        }

        // 지정 타격은 칼날 검출을 거치지 않으므로, 판정이 소유자에게 돌아오는 경로를 따로 확인한다.
        [UnityTest]
        public IEnumerator Strike_WithoutSwing_PassesOutcomeToOwner()
        {
            // 칼날에서 떨어진 곳에 두어, 칼날 검출로는 맞을 수 없게 한다.
            var targetObject = new GameObject("Target");
            targetObject.transform.position = _character.transform.position + Vector3.back * 3f;
            SpyHittable target = targetObject.AddComponent<SpyHittable>();
            target.Outcome = HitOutcome.Parried;

            _weapon.Strike(target, HitStrength.Heavy, false, 0);
            yield return null;

            Object.DestroyImmediate(targetObject);
            Assert.That(_dealt.Count, Is.EqualTo(1));
            Assert.That(_dealt[0].Target, Is.SameAs(target));
            Assert.That(_dealt[0].Attack.Attacker, Is.SameAs(_character.transform));
            Assert.That(_dealt[0].Result.Outcome, Is.EqualTo(HitOutcome.Parried));
        }

        // 직접 타격은 칼날로 찾지 않으므로, 접점은 기준점에서 가장 가까운 대상의 몸 표면이어야 한다.
        [UnityTest]
        public IEnumerator Strike_WithTargetCollider_PassesImpactPointOnSurfaceNearestStrikePoint()
        {
            const float targetRadius = 0.4f;
            var targetObject = new GameObject("Target");
            targetObject.transform.position = _strikePoint.position + new Vector3(1f, 0f, 0f);
            targetObject.AddComponent<SphereCollider>().radius = targetRadius;
            SpyHittable target = targetObject.AddComponent<SpyHittable>();

            _weapon.Strike(target, HitStrength.Heavy, false, 0);
            yield return null;

            Vector3 center = targetObject.transform.position;
            Object.DestroyImmediate(targetObject);
            Assert.That(_dealt.Count, Is.EqualTo(1));
            ImpactPoint impact = _dealt[0].Attack.ImpactPoint;
            Assert.That(Vector3.Distance(impact.Position, center + Vector3.left * targetRadius), Is.LessThan(1e-4f));
            Assert.That(Vector3.Distance(impact.Normal, Vector3.left), Is.LessThan(1e-4f));
        }

        private int CountPlayingSounds()
        {
            int count = 0;
            GameObject container = GameObject.Find(nameof(SoundPlayer));
            if (container == null)
            {
                return 0;
            }

            foreach (AudioSource source in container.GetComponentsInChildren<AudioSource>())
            {
                if (source.isPlaying)
                {
                    count++;
                }
            }

            return count;
        }

        // 궤적은 주인의 시계로 나이를 재므로 캐릭터에 시계를 두고, 칼은 그 자식으로 둔다.
        private void CreateWeapon(bool withTrail)
        {
            // Awake의 설정 검사보다 먼저 연결하도록 비활성 상태로 만든다.
            _character = new GameObject("Character");
            _character.SetActive(false);
            _character.AddComponent<CharacterClock>();

            var blade = new GameObject("Blade");
            blade.transform.SetParent(_character.transform, false);
            var tip = new GameObject("Tip").transform;
            tip.SetParent(blade.transform, false);
            tip.localPosition = Vector3.forward;
            // 손잡이로 치는 카운터처럼 칼날 밖에 둔다.
            _strikePoint = new GameObject("StrikePoint").transform;
            _strikePoint.SetParent(blade.transform, false);
            _strikePoint.localPosition = Vector3.back * 0.2f;

            _weapon = blade.AddComponent<Weapon>();
            _hitbox = blade.AddComponent<WeaponHitbox>();
            var weapon = new SerializedObject(_weapon);
            weapon.FindProperty("_base").objectReferenceValue = blade.transform;
            weapon.FindProperty("_tip").objectReferenceValue = tip;
            weapon.FindProperty("_strikePoint").objectReferenceValue = _strikePoint;
            weapon.FindProperty("_hitbox").objectReferenceValue = _hitbox;

            _trail = null;
            if (withTrail)
            {
                _trail = blade.AddComponent<WeaponTrail>();
                var trail = new SerializedObject(_trail);
                trail.FindProperty("_material").objectReferenceValue = _material;
                trail.ApplyModifiedPropertiesWithoutUndo();
                weapon.FindProperty("_trail").objectReferenceValue = _trail;
            }

            weapon.ApplyModifiedPropertiesWithoutUndo();
            _character.SetActive(true);
            _weapon.Bind(_character.transform, _dealt.Add);
        }
    }
}
#endif
