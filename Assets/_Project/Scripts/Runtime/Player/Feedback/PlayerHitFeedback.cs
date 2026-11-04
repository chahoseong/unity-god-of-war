using System;
using GodOfWar.CameraSystem;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Player.Settings;
using UnityEngine;

namespace GodOfWar.Player.Feedback
{
    /// <summary>
    /// 플레이어가 치거나 맞은 타격에 피드백(역경직, 카메라 쉐이크)을 낸다. 피드백은 플레이어의 체감을 위한 연출이라 플레이어에만 둔다.
    /// 컨트롤러가 알리는 친 타격과 <see cref="HitTarget"/>이 알리는 받은 타격의 판정을 받아, 판정·세기에 맞는 칸으로 플레이어와 상대를 느리게 한다.
    /// 판정과 상태 전이는 모르고 결과만 관찰한다. 그래서 적끼리 친 타격에는 피드백이 없다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController), typeof(CharacterClock), typeof(HitTarget))]
    public sealed class PlayerHitFeedback : MonoBehaviour
    {
        [SerializeField] private HitFeedbackSettings _settings;

        private PlayerController _controller;
        private HitTarget _hitTarget;
        private CharacterClock _clock;

        private void Awake()
        {
            if (_settings == null)
            {
                throw new InvalidOperationException($"'{name}' has no {nameof(HitFeedbackSettings)} assigned.");
            }

            // RequireComponent는 이미 만들어진 프리팹에 컴포넌트를 채워 주지 않으므로 직접 확인한다.
            if (!TryGetComponent(out _controller))
            {
                throw new InvalidOperationException($"'{name}' requires a {nameof(PlayerController)} component.");
            }

            if (!TryGetComponent(out _hitTarget))
            {
                throw new InvalidOperationException($"'{name}' requires a {nameof(HitTarget)} component.");
            }

            if (!TryGetComponent(out _clock))
            {
                throw new InvalidOperationException($"'{name}' requires a {nameof(CharacterClock)} component.");
            }

            _controller.HitDealt += OnHitDealt;
            _hitTarget.HitTaken += OnHitTaken;
        }

        private void OnDestroy()
        {
            // Awake 검증에서 실패하면 구독하기 전에 멈춘다. 둘 다 찾은 뒤에야 구독하므로 시계까지 찾았는지로 가린다.
            if (_clock != null)
            {
                _controller.HitDealt -= OnHitDealt;
                _hitTarget.HitTaken -= OnHitTaken;
            }
        }

        private void OnHitDealt(HitEvent hitEvent)
        {
            AttackInfo attack = hitEvent.Attack;
            if (_settings.TryGetDealt(hitEvent.Result.Outcome, attack.Strength, attack.Knockdown, out HitFeedback feedback))
            {
                Apply(feedback, hitEvent.Target as Component);
            }
        }

        private void OnHitTaken(HitEvent hitEvent)
        {
            if (_settings.TryGetTaken(hitEvent.Result.Outcome, hitEvent.Attack.Strength, out HitFeedback feedback))
            {
                Apply(feedback, hitEvent.Attack.Attacker);
            }
        }

        private void Apply(HitFeedback feedback, Component opponent)
        {
            _clock.Slow(feedback.HitstopDuration, feedback.HitstopTimeScale);
            // 시계가 없는 상대는 플레이어만 느려진다. 파괴된 상대도 Unity 오브젝트 비교로 걸러진다.
            if (opponent != null && opponent.TryGetComponent(out CharacterClock opponentClock))
            {
                opponentClock.Slow(feedback.HitstopDuration, feedback.HitstopTimeScale);
            }

            // 플레이어가 늘 당사자라 흔들림이 거리에 따라 약해지지 않는 것이 맞다.
            CameraShake.Emit(transform.position, feedback.ShakeShape, feedback.ShakeDuration, feedback.ShakeForce);
        }
    }
}
