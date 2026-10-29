using System;
using UnityEngine;

namespace GodOfWar.Characters
{
    /// <summary>
    /// 캐릭터 한 명의 시계. 상태 진행·애니메이션·패링 윈도우처럼 그 캐릭터의 시간 흐름이 이 시계를 따르면,
    /// 다른 캐릭터와 카메라는 그대로 둔 채 이 캐릭터만 잠깐 느리게 할 수 있다(역경직).
    /// 평소에는 전역 게임 시간과 똑같이 흐른다. 슬로 시간도 전역 게임 시간으로 재므로, 전역 배속을 바꾸면 슬로도 같은 비율로 늘고 준다.
    /// 읽는 쪽이 같은 프레임의 값을 보도록 캐릭터의 다른 컴포넌트보다 먼저 갱신한다.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.CharacterClock)]
    public sealed class CharacterClock : MonoBehaviour
    {
        private float _slowRemaining;
        private float _slowTimeScale = 1f;

        // 같은 프레임에 받은 요청끼리 비교하려고, 마지막으로 받아들인 요청의 프레임과 뒤처지게 하는 시간을 기억한다.
        private int _requestFrame = -1;
        private float _requestLag;

        /// <summary>이번 프레임에 이 캐릭터가 진행할 시간(초). 슬로 중이면 배율만큼 줄고, 슬로가 프레임 중간에 끝나면 남은 몫은 제 속도로 흐른다.</summary>
        public float DeltaTime { get; private set; }

        /// <summary>이번 프레임에 전역 게임 시간 대비 이 캐릭터가 진행한 비율(0~1). 애니메이터 속도처럼 비율로 적용할 곳에 쓴다.</summary>
        public float TimeScale { get; private set; } = 1f;

        /// <summary>이 캐릭터가 지금까지 진행한 시간(초). 슬로 중에는 배율만큼 느리게 는다.</summary>
        public double Now { get; private set; }

        /// <summary>
        /// 전역 게임 시간으로 <paramref name="duration"/>초 동안 <paramref name="timeScale"/> 배율(0이면 정지)로 느리게 진행한다.
        /// 다음 갱신부터 적용된다. 피격 반응이 가장 최근 타격으로 바뀌듯, 앞선 프레임에 받은 슬로는 새 요청이 대신한다.
        /// 같은 프레임에 여러 요청이 오면 순서와 관계없이 더 많이 뒤처지게 하는(시간 × (1 − 배율)이 큰) 요청 하나를 따른다.
        /// </summary>
        public void Slow(float duration, float timeScale)
        {
            if (duration < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "Must not be negative.");
            }

            if (timeScale < 0f || timeScale > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(timeScale), timeScale, "Must be between 0 and 1.");
            }

            float lag = duration * (1f - timeScale);
            if (_requestFrame == Time.frameCount && lag <= _requestLag)
            {
                return;
            }

            _requestFrame = Time.frameCount;
            _requestLag = lag;
            _slowRemaining = duration;
            _slowTimeScale = timeScale;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            float slowed = Mathf.Min(_slowRemaining, deltaTime);
            _slowRemaining -= slowed;

            DeltaTime = slowed * _slowTimeScale + (deltaTime - slowed);
            // 전역 시간이 멈춘 프레임(일시 정지)에는 비율을 정할 수 없어, 슬로 중인지만 반영한다.
            TimeScale = deltaTime > 0f ? DeltaTime / deltaTime : _slowRemaining > 0f ? _slowTimeScale : 1f;
            Now += DeltaTime;
        }
    }
}
