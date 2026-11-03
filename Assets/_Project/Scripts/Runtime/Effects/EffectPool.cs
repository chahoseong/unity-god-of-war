using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace GodOfWar.Effects
{
    /// <summary>
    /// 한 번 재생하고 끝나는 파티클 이펙트를 프리팹마다 하나의 풀로 재사용한다. 씬에 두지 않고 어디서든 <see cref="Play"/>로 쓴다.
    /// 인스턴스를 만들 때 반복 재생과 자동 재생을 끄고 루트의 정지 동작을 콜백으로 바꾸므로, 프리팹의 그 설정은 무시된다(원본 에셋은 그대로다).
    /// 재생이 끝난 인스턴스는 스스로 풀에 돌아온다. 인스턴스는 씬이 바뀌어도 남는 컨테이너 하나 아래에 모은다.
    /// </summary>
    public static class EffectPool
    {
        // 한 프리팹이 동시에 재생하는 수가 이를 넘으면, 넘친 만큼은 돌아올 때 파괴한다.
        private const int MaxSize = 32;

        private static readonly Dictionary<ParticleSystem, ObjectPool<PooledEffect>> s_pools =
            new Dictionary<ParticleSystem, ObjectPool<PooledEffect>>();

        private static Transform s_container;
        // 만든 직후 자동 재생 설정을 끄기 전에 Awake·재생이 돌지 않도록, 꺼진 부모 아래에서 만든다.
        private static Transform s_staging;

        /// <summary>
        /// <paramref name="prefab"/>의 인스턴스 하나를 꺼내 <paramref name="position"/>·<paramref name="rotation"/>에서 처음부터 재생한다.
        /// 앞서 재생 중인 인스턴스는 끊지 않고 따로 꺼낸다.
        /// </summary>
        /// <returns>재생을 시작한 인스턴스. 재생이 끝나면 풀에 돌아가므로 붙잡아 두지 않는다.</returns>
        public static ParticleSystem Play(ParticleSystem prefab, Vector3 position, Quaternion rotation)
        {
            ObjectPool<PooledEffect> pool = GetPool(prefab ?? throw new ArgumentNullException(nameof(prefab)));
            PooledEffect effect = pool.Get();
            effect.transform.SetPositionAndRotation(position, rotation);
            effect.Play();
            return effect.System;
        }

        /// <summary>
        /// <paramref name="prefab"/>의 인스턴스가 모두 <paramref name="count"/>개 이상이 되도록 미리 만들어 둔다.
        /// 첫 재생에서 인스턴스를 만드는 비용을 시작할 때로 옮긴다.
        /// </summary>
        public static void Prewarm(ParticleSystem prefab, int count)
        {
            ObjectPool<PooledEffect> pool = GetPool(prefab ?? throw new ArgumentNullException(nameof(prefab)));
            int missing = Mathf.Min(count, MaxSize) - pool.CountAll;
            if (missing <= 0)
            {
                return;
            }

            var created = new PooledEffect[missing];
            for (int i = 0; i < missing; i++)
            {
                created[i] = pool.Get();
            }

            foreach (PooledEffect effect in created)
            {
                pool.Release(effect);
            }
        }

        // 도메인 리로드를 끈 프로젝트라 정적 목록이 플레이 세션 사이에 남는다. 지난 세션의 인스턴스는 이미 파괴됐으므로 비운다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            s_pools.Clear();
            s_container = null;
            s_staging = null;
        }

        private static ObjectPool<PooledEffect> GetPool(ParticleSystem prefab)
        {
            EnsureContainer();
            if (s_pools.TryGetValue(prefab, out ObjectPool<PooledEffect> pool))
            {
                return pool;
            }

            pool = null;
            pool = new ObjectPool<PooledEffect>(
                () => Create(prefab, pool),
                actionOnRelease: effect => effect.gameObject.SetActive(false),
                actionOnDestroy: DestroyInstance,
                maxSize: MaxSize);
            s_pools.Add(prefab, pool);
            return pool;
        }

        // 컨테이너가 파괴되면(플레이 종료, 직접 파괴) 그 아래 인스턴스도 모두 사라졌으므로 풀을 새로 시작한다.
        private static void EnsureContainer()
        {
            if (s_container != null)
            {
                return;
            }

            s_pools.Clear();
            var container = new GameObject(nameof(EffectPool));
            if (Application.isPlaying)
            {
                UnityEngine.Object.DontDestroyOnLoad(container);
            }

            var staging = new GameObject("Staging");
            staging.SetActive(false);
            staging.transform.SetParent(container.transform, false);
            s_container = container.transform;
            s_staging = staging.transform;
        }

        // 에디터는 플레이 모드가 바뀔 때 모든 ObjectPool을 비우는데, 그때는 지난 세션의 인스턴스가 이미 파괴돼 있다.
        private static void DestroyInstance(PooledEffect effect)
        {
            if (effect != null)
            {
                UnityEngine.Object.Destroy(effect.gameObject);
            }
        }

        private static PooledEffect Create(ParticleSystem prefab, ObjectPool<PooledEffect> pool)
        {
            ParticleSystem system = UnityEngine.Object.Instantiate(prefab, s_staging);
            system.name = prefab.name;
            foreach (ParticleSystem part in system.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = part.main;
                main.loop = false;
                main.playOnAwake = false;
            }

            ParticleSystem.MainModule rootMain = system.main;
            rootMain.stopAction = ParticleSystemStopAction.Callback;

            PooledEffect effect = system.gameObject.AddComponent<PooledEffect>();
            effect.Initialize(system, pool);
            system.gameObject.SetActive(false);
            system.transform.SetParent(s_container, false);
            return effect;
        }
    }
}
