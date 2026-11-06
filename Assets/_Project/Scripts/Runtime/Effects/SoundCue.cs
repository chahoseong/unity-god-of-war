using System;
using UnityEngine;

namespace GodOfWar.Effects
{
    /// <summary>
    /// 한 사건에 낼 소리. 클립 묶음에서 하나를 무작위로 골라 정해진 볼륨과 범위 안의 피치로 재생한다.
    /// 같은 소리가 연달아 기계적으로 반복되지 않도록, 고를 수 있는 클립이 둘 이상이면 직전에 고른 클립은 건너뛴다.
    /// 비어 있거나 빠진(Missing) 클립은 없는 것으로 친다. 고를 클립이 하나도 없으면 소리를 내지 않는 칸이다.
    /// </summary>
    [Serializable]
    public sealed class SoundCue
    {
        [Tooltip("무작위로 하나를 고를 클립. 비어 있거나 빠진 클립은 건너뛴다. 하나도 없으면 소리를 내지 않는다.")]
        [SerializeField] private AudioClip[] _clips = Array.Empty<AudioClip>();

        [Tooltip("재생 볼륨.")]
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        [Tooltip("피치 하한. 재생 속도도 함께 바뀌므로 낮출수록 소리가 길고 묵직해진다.")]
        [SerializeField, Range(0.1f, 3f)] private float _minPitch = 0.95f;

        [Tooltip("피치 상한.")]
        [SerializeField, Range(0.1f, 3f)] private float _maxPitch = 1.05f;

        // 에셋에 저장할 값이 아니라 재생하며 생기는 기록이다. 같은 칸을 쓰는 캐릭터끼리 공유한다.
        [NonSerialized] private int? _lastIndex;

        public SoundCue()
        {
        }

        public SoundCue(AudioClip[] clips, float volume, float minPitch, float maxPitch)
        {
            _clips = clips ?? throw new ArgumentNullException(nameof(clips));
            _volume = volume;
            _minPitch = minPitch;
            _maxPitch = maxPitch;
        }

        public float Volume => _volume;

        /// <summary>
        /// 재생할 클립과 피치를 고른다. 고를 수 있는 클립이 둘 이상이면 직전에 고른 클립은 고르지 않는다.
        /// </summary>
        /// <returns>고를 클립이 없으면 false.</returns>
        public bool TryPick(out AudioClip clip, out float pitch)
        {
            int playable = 0;
            bool lastIsPlayable = false;
            for (int i = 0; i < _clips.Length; i++)
            {
                // 빠진 참조도 Unity 오브젝트 비교로 null이 된다.
                if (_clips[i] != null)
                {
                    playable++;
                    lastIsPlayable |= i == _lastIndex;
                }
            }

            if (playable == 0)
            {
                clip = null;
                pitch = 1f;
                return false;
            }

            bool skipLast = lastIsPlayable && playable > 1;
            int choice = UnityEngine.Random.Range(0, skipLast ? playable - 1 : playable);
            for (int i = 0; i < _clips.Length; i++)
            {
                if (_clips[i] == null || (skipLast && i == _lastIndex))
                {
                    continue;
                }

                if (choice-- == 0)
                {
                    _lastIndex = i;
                    clip = _clips[i];
                    pitch = UnityEngine.Random.Range(_minPitch, _maxPitch);
                    return true;
                }
            }

            throw new InvalidOperationException("Unreachable: a playable clip must have been chosen.");
        }
    }
}
