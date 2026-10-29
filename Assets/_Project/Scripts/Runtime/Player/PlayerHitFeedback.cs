using System;
using GodOfWar.Characters;
using GodOfWar.Combat;
using Unity.Cinemachine;
using UnityEngine;

namespace GodOfWar.Player
{
    /// <summary>
    /// 플레이어가 치거나 맞은 타격에 피드백(역경직, 카메라 쉐이크)을 낸다. 피드백은 플레이어의 체감을 위한 연출이라 플레이어에만 둔다.
    /// 플레이어 칼의 판정 통지(친 타격)와 컨트롤러의 피격 통지(받은 타격)를 받아, 판정·세기에 맞는 칸으로 플레이어와 상대를 느리게 한다.
    /// 판정과 상태 전이는 모르고 결과만 관찰한다. 그래서 적끼리 친 타격에는 피드백이 없다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController), typeof(CharacterClock))]
    public sealed class PlayerHitFeedback : MonoBehaviour
    {
        [SerializeField] private HitFeedbackSettings _settings;

        private PlayerController _controller;
        private CharacterClock _clock;
        private Hitbox _hitbox;

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

            if (!TryGetComponent(out _clock))
            {
                throw new InvalidOperationException($"'{name}' requires a {nameof(CharacterClock)} component.");
            }

            _hitbox = FindRequiredHitbox();
            _hitbox.HitResolved += OnHitDealt;
            _controller.HitReceived += OnHitTaken;
        }

        private void OnDestroy()
        {
            // Awake 검증에서 실패하면 구독하기 전에 멈춘다.
            if (_hitbox != null)
            {
                _hitbox.HitResolved -= OnHitDealt;
            }

            if (_controller != null)
            {
                _controller.HitReceived -= OnHitTaken;
            }
        }

        private void OnHitDealt(HitReport report)
        {
            if (_settings.TryGetDealt(report.Result.Outcome, report.Attack.Strength, out HitFeedback feedback))
            {
                Apply(feedback, report.Target as Component);
            }
        }

        private void OnHitTaken(HitReport report)
        {
            if (_settings.TryGetTaken(report.Result.Outcome, report.Attack.Strength, out HitFeedback feedback))
            {
                Apply(feedback, report.Attack.Attacker);
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

            Shake(feedback);
        }

        private void Shake(HitFeedback feedback)
        {
            if (feedback.ShakeForce <= 0f)
            {
                return;
            }

            // 플레이어가 늘 당사자라 흔들림이 거리에 따라 약해지지 않게 한다. 정의는 진행 중인 흔들림이 계속 읽으므로 매번 새로 만든다.
            var impulse = new CinemachineImpulseDefinition
            {
                ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
                ImpulseShape = ToImpulseShape(feedback.ShakeShape),
                ImpulseDuration = feedback.ShakeDuration
            };
            // 리스너가 카메라 공간으로 해석하므로, 카메라 각도와 관계없이 화면 아래쪽으로 민다.
            impulse.CreateEvent(transform.position, Vector3.down * feedback.ShakeForce);
        }

        private static CinemachineImpulseDefinition.ImpulseShapes ToImpulseShape(HitShakeShape shape)
        {
            return shape switch
            {
                HitShakeShape.Recoil => CinemachineImpulseDefinition.ImpulseShapes.Recoil,
                HitShakeShape.Bump => CinemachineImpulseDefinition.ImpulseShapes.Bump,
                HitShakeShape.Explosion => CinemachineImpulseDefinition.ImpulseShapes.Explosion,
                HitShakeShape.Rumble => CinemachineImpulseDefinition.ImpulseShapes.Rumble,
                _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null)
            };
        }

        // 칼은 손 본 아래에 있어 루트가 아니라 자식에서 찾는다.
        private Hitbox FindRequiredHitbox()
        {
            Hitbox[] hitboxes = GetComponentsInChildren<Hitbox>(true);
            if (hitboxes.Length != 1)
            {
                throw new InvalidOperationException(
                    $"'{name}' requires exactly one {nameof(Hitbox)} in its children, but found {hitboxes.Length}.");
            }

            return hitboxes[0];
        }
    }
}
