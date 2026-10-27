using System;

namespace GodOfWar.Player
{
    /// <summary>
    /// 저스트 패링 윈도우와 헛친 뒤의 쿨다운 규칙. 가드 버튼을 누른 시각부터 정해진 시간 동안 윈도우가 열리고,
    /// 패링 없이 닫히면(시간이 지나거나 가드를 풀면) 그때부터 쿨다운 동안은 다시 눌러도 윈도우가 열리지 않는다.
    /// 패링에 성공하면 쿨다운 없이 닫힌다. 시각은 모두 호출하는 쪽이 넘기는 같은 시계(게임 시간, 초)를 쓴다.
    /// </summary>
    public sealed class ParryWindow
    {
        private readonly float _duration;
        private readonly float _cooldown;

        private bool _isOpen;
        private double _closeTime;
        private double _cooldownEnd = double.NegativeInfinity;

        /// <param name="duration">누른 시각부터 윈도우가 열려 있는 시간(초).</param>
        /// <param name="cooldown">헛친 윈도우가 닫힌 뒤 새 윈도우를 열지 않는 시간(초).</param>
        public ParryWindow(float duration, float cooldown)
        {
            if (duration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "Must be positive.");
            }

            if (cooldown < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cooldown), cooldown, "Must not be negative.");
            }

            _duration = duration;
            _cooldown = cooldown;
        }

        /// <summary><paramref name="now"/>에 윈도우가 열려 있는지.</summary>
        public bool IsOpen(double now)
        {
            return _isOpen && now < _closeTime;
        }

        /// <summary>
        /// <paramref name="pressTime"/>에 누른 가드로 윈도우를 연다. 쿨다운 중에 누른 것이면 열지 않고 false를 반환한다.
        /// 선입력처럼 늦게 전달된 누름은 이미 지나간 윈도우를 열 수 있으며, 그 윈도우는 헛친 것으로 닫힌다.
        /// </summary>
        public bool TryOpen(double pressTime)
        {
            if (pressTime < _cooldownEnd)
            {
                return false;
            }

            _isOpen = true;
            _closeTime = pressTime + _duration;
            return true;
        }

        /// <summary>열린 윈도우로 패링에 성공했다. 쿨다운 없이 닫는다.</summary>
        public void Succeed()
        {
            _isOpen = false;
        }

        /// <summary>
        /// 윈도우를 헛친 것으로 닫는다. 시간이 지나 닫히면 윈도우 끝부터, 그 전에 닫으면 <paramref name="now"/>부터 쿨다운을 잰다.
        /// 열려 있지 않으면 아무 일도 하지 않는다.
        /// </summary>
        public void Close(double now)
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;
            _cooldownEnd = Math.Min(now, _closeTime) + _cooldown;
        }
    }
}
