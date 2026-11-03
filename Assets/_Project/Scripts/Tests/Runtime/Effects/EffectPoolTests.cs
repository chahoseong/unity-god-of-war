using System.Collections;
using GodOfWar.Effects;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GodOfWar.Tests.Effects
{
    // 프리팹 대신 꺼 둔 씬 오브젝트를 원본으로 쓴다. 입자는 시작할 때 한 번에 하나만 내보내 수명이 곧 재생 길이가 되게 한다.
    public class EffectPoolTests
    {
        private const float ShortLifetime = 0.1f;
        private const float LongLifetime = 0.6f;

        private GameObject _prefab;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_prefab);
            // 컨테이너를 지우면 풀이 다음 재생에서 새로 시작하므로, 테스트끼리 인스턴스가 섞이지 않는다.
            Object.DestroyImmediate(GameObject.Find(nameof(EffectPool)));
        }

        // 원본은 반복 재생으로 두어, 풀이 반복을 끄지 않으면 영영 돌아오지 않게 한다.
        [UnityTest]
        public IEnumerator Play_WithLoopingPrefab_DeactivatesInstanceAfterLifetime()
        {
            ParticleSystem prefab = CreatePrefab(ShortLifetime);

            ParticleSystem instance = EffectPool.Play(prefab, Vector3.zero, Quaternion.identity);
            yield return new WaitForSeconds(ShortLifetime + 0.3f);

            Assert.That(instance.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator Play_AfterPreviousInstanceFinished_ReusesInstance()
        {
            ParticleSystem prefab = CreatePrefab(ShortLifetime);
            ParticleSystem first = EffectPool.Play(prefab, Vector3.zero, Quaternion.identity);
            yield return new WaitForSeconds(ShortLifetime + 0.3f);

            ParticleSystem second = EffectPool.Play(prefab, Vector3.zero, Quaternion.identity);

            Assert.That(second, Is.SameAs(first));
            Assert.That(second.isPlaying, Is.True);
        }

        // 연타에서 앞 이펙트가 끊기지 않아야 한다.
        [UnityTest]
        public IEnumerator Play_WhilePreviousInstancePlaying_ReturnsSeparateInstance()
        {
            ParticleSystem prefab = CreatePrefab(LongLifetime);
            ParticleSystem first = EffectPool.Play(prefab, Vector3.zero, Quaternion.identity);
            yield return null;

            ParticleSystem second = EffectPool.Play(prefab, Vector3.right, Quaternion.identity);

            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(first.isPlaying, Is.True);
        }

        // 루트가 먼저 끝나도 자식 입자가 남아 있는 동안은 돌려보내지 않아야 화면에서 이펙트가 잘리지 않는다.
        [UnityTest]
        public IEnumerator Play_WithChildOutlastingRoot_DeactivatesInstanceOnlyAfterChildFinishes()
        {
            ParticleSystem prefab = CreatePrefab(ShortLifetime);
            AddChildSystem(prefab, LongLifetime);

            ParticleSystem instance = EffectPool.Play(prefab, Vector3.zero, Quaternion.identity);
            yield return new WaitForSeconds(ShortLifetime + 0.2f);
            bool activeWhileChildAlive = instance.gameObject.activeSelf;
            yield return new WaitForSeconds(LongLifetime);

            Assert.That(activeWhileChildAlive, Is.True);
            Assert.That(instance.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator Play_WithPositionAndRotation_SetsInstancePositionAndRotation()
        {
            ParticleSystem prefab = CreatePrefab(ShortLifetime);
            var position = new Vector3(1f, 2f, 3f);
            Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);

            ParticleSystem instance = EffectPool.Play(prefab, position, rotation);
            yield return null;

            Assert.That(Vector3.Distance(instance.transform.position, position), Is.LessThan(1e-4f));
            Assert.That(Quaternion.Angle(instance.transform.rotation, rotation), Is.LessThan(0.01f));
        }

        private ParticleSystem CreatePrefab(float lifetime)
        {
            _prefab = new GameObject("TestEffect");
            _prefab.SetActive(false);
            ParticleSystem system = _prefab.AddComponent<ParticleSystem>();
            Configure(system, lifetime);
            return system;
        }

        private static void AddChildSystem(ParticleSystem parent, float lifetime)
        {
            var child = new GameObject("Child");
            child.transform.SetParent(parent.transform, false);
            Configure(child.AddComponent<ParticleSystem>(), lifetime);
        }

        private static void Configure(ParticleSystem system, float lifetime)
        {
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = ShortLifetime;
            main.startLifetime = lifetime;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
        }
    }
}
