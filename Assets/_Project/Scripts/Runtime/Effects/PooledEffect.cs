using UnityEngine;
using UnityEngine.Pool;

namespace GodOfWar.Effects
{
    /// <summary>
    /// <see cref="EffectPool"/>이 만든 이펙트 인스턴스에 붙어, 재생이 끝나면 인스턴스를 풀에 돌려준다. 풀만 만들고 붙인다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PooledEffect : MonoBehaviour
    {
        private IObjectPool<PooledEffect> _pool;
        private bool _isPlaying;

        public ParticleSystem System { get; private set; }

        internal void Initialize(ParticleSystem system, IObjectPool<PooledEffect> pool)
        {
            System = system;
            _pool = pool;
        }

        internal void Play()
        {
            gameObject.SetActive(true);
            _isPlaying = true;
            System.Play(true);
        }

        // 루트의 정지 동작이 콜백이라, 루트가 멈추고 남은 입자가 모두 사라지면 Unity가 부른다.
        private void OnParticleSystemStopped()
        {
            // 재생하지 않은 채 멈춘 경우(만든 직후 등)에 두 번 돌려주지 않는다.
            if (!_isPlaying)
            {
                return;
            }

            _isPlaying = false;
            _pool.Release(this);
        }
    }
}
