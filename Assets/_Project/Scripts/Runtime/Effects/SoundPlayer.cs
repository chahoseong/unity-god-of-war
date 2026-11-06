using System;
using System.Collections.Generic;
using UnityEngine;

namespace GodOfWar.Effects
{
    /// <summary>
    /// 게임 전체가 함께 쓰는 효과음 재생기. 소리를 내려는 쪽은 무엇을 어디서 낼지만 정해 <see cref="Play"/>로 던지고,
    /// 재생은 이 재생기가 맡는다. 보이스(재생 채널)는 요청한 쪽이 아니라 이 재생기에 붙으므로, 요청한 캐릭터가 꺼지거나 사라져도 소리는 끝까지 난다.
    /// 피치는 AudioSource마다 하나라 소리마다 보이스를 따로 쓴다. 쉬는 보이스를 먼저 쓰고, 모두 바쁘면 상한까지 새로 만들며,
    /// 상한에 이르면 가장 먼저 시작한 소리를 끊고 그 보이스를 쓴다.
    /// </summary>
    public static class SoundPlayer
    {
        // 동시에 낼 수 있는 소리의 상한. 미리 만들지 않고 겹친 만큼만 만든다.
        // 가장 바쁜 순간(콤보 마지막 타로 쓰러뜨릴 때)에 7~8개가 겹치므로 그 두 배를 둔다.
        private const int MaxVoices = 16;

        private static readonly List<AudioSource> s_voices = new List<AudioSource>();
        // 보이스마다 재생을 시작한 순번. 모두 바쁠 때 가장 오래된 소리를 고르는 데 쓴다.
        private static readonly List<long> s_startOrder = new List<long>();
        private static long s_nextOrder;
        private static Transform s_container;

        /// <summary>
        /// <paramref name="cue"/>에서 클립을 골라 <paramref name="position"/>에서 재생한다. 고를 클립이 없으면 아무것도 하지 않는다.
        /// </summary>
        /// <param name="spatialBlend">0이면 거리와 방향이 없는 2D, 1이면 위치에서 나는 3D 소리.</param>
        /// <returns>재생을 시작한 보이스. 다른 소리가 빼앗아 갈 수 있으므로 붙잡아 두지 않는다. 고를 클립이 없으면 null.</returns>
        public static AudioSource Play(SoundCue cue, Vector3 position, float spatialBlend)
        {
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            if (!cue.TryPick(out AudioClip clip, out float pitch))
            {
                return null;
            }

            EnsureContainer();
            int index = PickVoice();
            AudioSource voice = s_voices[index];
            voice.Stop();
            voice.transform.position = position;
            voice.clip = clip;
            voice.volume = cue.Volume;
            voice.pitch = pitch;
            voice.spatialBlend = spatialBlend;
            voice.Play();
            s_startOrder[index] = ++s_nextOrder;
            return voice;
        }

        // 도메인 리로드를 끈 프로젝트라 정적 목록이 플레이 세션 사이에 남는다. 지난 세션의 보이스는 이미 파괴됐으므로 비운다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            s_voices.Clear();
            s_startOrder.Clear();
            s_nextOrder = 0;
            s_container = null;
        }

        // 컨테이너가 파괴되면(플레이 종료, 직접 파괴) 그 아래 보이스도 모두 사라졌으므로 새로 시작한다.
        private static void EnsureContainer()
        {
            if (s_container != null)
            {
                return;
            }

            s_voices.Clear();
            s_startOrder.Clear();
            var container = new GameObject(nameof(SoundPlayer));
            if (Application.isPlaying)
            {
                UnityEngine.Object.DontDestroyOnLoad(container);
            }

            s_container = container.transform;
        }

        private static int PickVoice()
        {
            int oldest = -1;
            for (int i = 0; i < s_voices.Count; i++)
            {
                if (!s_voices[i].isPlaying)
                {
                    return i;
                }

                if (oldest < 0 || s_startOrder[i] < s_startOrder[oldest])
                {
                    oldest = i;
                }
            }

            if (s_voices.Count < MaxVoices)
            {
                s_voices.Add(CreateVoice(s_voices.Count));
                s_startOrder.Add(0);
                return s_voices.Count - 1;
            }

            return oldest;
        }

        // 보이스마다 자식 오브젝트를 따로 둬, 3D 소리가 요청한 위치에서 나게 한다.
        private static AudioSource CreateVoice(int index)
        {
            var voiceObject = new GameObject($"Voice {index}");
            voiceObject.transform.SetParent(s_container, false);
            AudioSource source = voiceObject.AddComponent<AudioSource>();
            // 켜진 오브젝트에 붙이면 끄기 전에 Play On Awake가 먼저 돌아, 클립 없이도 재생 중으로 남는다.
            source.playOnAwake = false;
            source.Stop();
            source.loop = false;
            return source;
        }
    }
}
