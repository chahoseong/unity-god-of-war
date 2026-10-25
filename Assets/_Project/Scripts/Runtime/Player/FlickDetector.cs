using System;
using GodOfWar.Input;
using UnityEngine;

namespace GodOfWar.Player
{
    /// <summary>
    /// 시점 입력의 좌우 성분에서 "한 번 튕김"을 골라낸다. 락온 대상 전환에 쓴다.
    /// 스틱은 밀린 위치라 두 문턱(발동·재장전)으로, 마우스는 이동량이라 감쇠하는 누적량과 대기 시간으로 판정한다.
    /// </summary>
    public sealed class FlickDetector
    {
        private readonly LockOnSettings _settings;

        private bool _isStickArmed = true;
        private float _mouseAccumulated;
        private float _mouseCooldown;

        public FlickDetector(LockOnSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>이번 프레임 입력으로 플릭이 일어났으면 방향(-1 = 왼쪽, +1 = 오른쪽)을, 아니면 0을 반환한다.</summary>
        public int Tick(Vector2 look, LookInputType type, float deltaTime)
        {
            _mouseCooldown = Mathf.Max(0f, _mouseCooldown - deltaTime);
            return type == LookInputType.Rate
                ? TickStick(look.x)
                : TickMouse(look.x, deltaTime);
        }

        /// <summary>스틱이 중립으로 돌아올 때까지 판정을 막고 마우스 누적량을 비운다.</summary>
        public void Disarm()
        {
            _isStickArmed = false;
            _mouseAccumulated = 0f;
        }

        private int TickStick(float x)
        {
            float magnitude = Mathf.Abs(x);
            if (!_isStickArmed)
            {
                _isStickArmed = magnitude < _settings.StickRearmThreshold;
                return 0;
            }

            if (magnitude < _settings.StickFlickThreshold)
            {
                return 0;
            }

            _isStickArmed = false;
            return Math.Sign(x);
        }

        private int TickMouse(float x, float deltaTime)
        {
            if (_mouseCooldown > 0f)
            {
                _mouseAccumulated = 0f;
                return 0;
            }

            // 천천히 움직인 양은 누적되기 전에 사라지도록 지수적으로 줄인다.
            _mouseAccumulated = _mouseAccumulated * Mathf.Exp(-_settings.MouseDecayRate * deltaTime) + x;
            if (Mathf.Abs(_mouseAccumulated) < _settings.MouseFlickThreshold)
            {
                return 0;
            }

            int direction = Math.Sign(_mouseAccumulated);
            _mouseAccumulated = 0f;
            _mouseCooldown = _settings.MouseFlickCooldown;
            return direction;
        }
    }
}
