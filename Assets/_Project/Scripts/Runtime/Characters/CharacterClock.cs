using System;
using UnityEngine;

namespace GodOfWar.Characters
{
    /// <summary>
    /// 캐릭터 한 명의 시계. 상태 진행·애니메이션·패링 윈도우처럼 그 캐릭터의 시간 흐름이 이 시계를 따르면,
    /// 다른 캐릭터와 카메라는 그대로 둔 채 이 캐릭터만 잠깐 멈출 수 있다(역경직).
    /// 평소에는 전역 게임 시간과 똑같이 흐른다. 정지 시간도 전역 게임 시간으로 재므로, 전역 배속을 바꾸면 정지도 같은 비율로 늘고 준다.
    /// 읽는 쪽이 같은 프레임의 값을 보도록 캐릭터의 다른 컴포넌트보다 먼저 갱신한다.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.CharacterClock)]
    public sealed class CharacterClock : MonoBehaviour
    {
        private float _freezeRemaining;

        /// <summary>이번 프레임에 이 캐릭터가 진행할 시간(초). 멈춰 있으면 0이고, 정지가 프레임 중간에 끝나면 남은 몫만큼이다.</summary>
        public float DeltaTime { get; private set; }

        /// <summary>이번 프레임에 전역 게임 시간 대비 이 캐릭터가 진행한 비율(0~1). 애니메이터 속도처럼 비율로 적용할 곳에 쓴다.</summary>
        public float TimeScale { get; private set; } = 1f;

        /// <summary>이 캐릭터가 지금까지 진행한 시간(초). 멈춘 동안은 늘지 않는다.</summary>
        public double Now { get; private set; }

        /// <summary>
        /// 전역 게임 시간으로 <paramref name="duration"/>초 동안 멈춘다. 다음 갱신부터 적용된다.
        /// 이미 멈춰 있으면 남은 시간과 비교해 더 긴 쪽으로 맞춘다. 그래서 정지는 가장 긴 요청 하나보다 길게 이어지지 않는다.
        /// </summary>
        public void Freeze(float duration)
        {
            if (duration < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "Must not be negative.");
            }

            _freezeRemaining = Mathf.Max(_freezeRemaining, duration);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            float frozen = Mathf.Min(_freezeRemaining, deltaTime);
            _freezeRemaining -= frozen;

            DeltaTime = deltaTime - frozen;
            // 전역 시간이 멈춘 프레임(일시 정지)에는 비율을 정할 수 없어, 멈춰 있는지만 반영한다.
            TimeScale = deltaTime > 0f ? DeltaTime / deltaTime : _freezeRemaining > 0f ? 0f : 1f;
            Now += DeltaTime;
        }
    }
}
