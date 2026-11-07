using System;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using UnityEngine;

namespace GodOfWar.Enemies.States
{
    /// <summary>
    /// 넉다운 상태. <see cref="Prepare"/>로 받은 공격자를 향해 곧바로 돌아선 뒤 뒤로 쓰러지고,
    /// 누운 채 정해진 시간을 기다렸다가 일어나 <see cref="IdleState"/>로 돌아간다.
    /// 쓰러지는 몸이 바닥에 부딪히는 시각에 한 번 알린다. 그 전에 상태를 나가면 알리지 않는다.
    /// 이 상태에 있는 동안의 타격으로는 반응을 바꾸지 않는다. 그 판단은 판정을 받는 쪽이 이 상태를 보고 한다.
    /// 일어나기 전에 <see cref="TryStayDown"/>을 받으면 일어나지 않고 누운 채로 남는다(넉다운 중 처치).
    /// </summary>
    public sealed class KnockdownState : IState
    {
        private static readonly int IdleHash = Animator.StringToHash("Idle");

        private enum Phase
        {
            Falling,
            Lying,
            GettingUp
        }

        private readonly ITrainingDummyStateContext _context;
        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly CharacterMotor _motor;
        private readonly Animator _animator;
        private readonly ActionMovement _movement = new ActionMovement();

        private IdleState _idle;

        // 다음 Enter에서 재생할 반응과 돌아설 공격자 위치.
        private KnockdownReaction _pendingReaction;
        private Vector3 _pendingAttackerPosition;

        private KnockdownReaction _reaction;
        private bool _hasMovement;
        private bool _hasHitGround;
        private bool _staysDown;
        private Phase _phase;
        // 지금 단계를 시작한 뒤 흐른 시간(초).
        private float _phaseElapsed;

        public KnockdownState(ITrainingDummyStateContext context)
        {
            _context = context;
            _machine = context.Machine;
            _locator = context.States;
            _motor = context.Motor;
            _animator = context.Animator;
        }

        /// <summary>
        /// 이번 넉다운에서 일어나지 않고 누운 채로 남게 한다. 쓰러지는 중이면 쓰러진 뒤 누운 자세로, 누워 있는 중이면 그대로 남는다.
        /// 이미 일어나는 중이면 몸이 반쯤 서 있어 누운 자세로 되돌리지 않고 거짓을 돌려준다. 이 상태가 아닐 때도 거짓이다.
        /// </summary>
        public bool TryStayDown()
        {
            if (_machine.CurrentState != this || _phase == Phase.GettingUp)
            {
                return false;
            }

            _staysDown = true;
            return true;
        }

        /// <summary>다음 진입에서 재생할 반응과 돌아설 공격자의 위치를 정한다. 전이 직전에 호출한다.</summary>
        public void Prepare(KnockdownReaction reaction, Vector3 attackerPosition)
        {
            _pendingReaction = reaction ?? throw new ArgumentNullException(nameof(reaction));
            _pendingAttackerPosition = attackerPosition;
        }

        public void Enter()
        {
            // 생성 시점에는 다른 상태가 아직 등록되지 않았을 수 있어 첫 진입에서 찾는다.
            _idle ??= _locator.Get<IdleState>();

            _reaction = _pendingReaction ?? throw new InvalidOperationException(
                $"{nameof(KnockdownState)} was entered without a reaction. Call {nameof(Prepare)} first.");
            _pendingReaction = null;
            _hasHitGround = false;
            _staysDown = false;

            // 뒤로 넘어지는 동작뿐이라, 공격자를 마주 봐야 공격자에게서 멀어지는 쪽으로 쓰러진다.
            FaceAttacker(_pendingAttackerPosition);
            StartPhase(Phase.Falling, _reaction.BeginStateName, _reaction.CrossFadeInDuration);

            // 변위는 돌아선 뒤의 몸 정면 기준으로 구워져 있다.
            _hasMovement = _reaction.BeginMovement != null;
            if (_hasMovement)
            {
                _movement.Begin(_reaction.BeginMovement, _motor.transform.eulerAngles.y);
            }
        }

        public void Tick(float deltaTime)
        {
            _phaseElapsed += deltaTime;
            bool isFalling = _phase == Phase.Falling && _hasMovement;
            _motor.Move(isFalling ? _movement.Tick(deltaTime) : Vector3.zero, deltaTime);

            // 시각은 경과 시간으로 재므로, 한 프레임이 길어 시각을 건너뛰어도 그 프레임에 한 번 알린다.
            if (_phase == Phase.Falling && !_hasHitGround && _phaseElapsed >= _reaction.GroundImpactTime)
            {
                _hasHitGround = true;
                _context.NotifyGroundImpact();
            }

            switch (_phase)
            {
                case Phase.Falling when _phaseElapsed >= _reaction.BeginDuration:
                    // 누워 있을 시간이 없으면 쓰러지자마자 일어난다. 일어나지 않을 몸은 시간과 관계없이 눕는다.
                    if (_staysDown || _reaction.LyingDuration > 0f)
                    {
                        StartPhase(Phase.Lying, _reaction.LoopStateName, _reaction.PhaseCrossFadeDuration);
                    }
                    else
                    {
                        StartPhase(Phase.GettingUp, _reaction.EndStateName, _reaction.PhaseCrossFadeDuration);
                    }

                    break;
                case Phase.Lying when !_staysDown && _phaseElapsed >= _reaction.LyingDuration:
                    StartPhase(Phase.GettingUp, _reaction.EndStateName, _reaction.PhaseCrossFadeDuration);
                    break;
                case Phase.GettingUp when _phaseElapsed >= _reaction.EndDuration:
                    _animator.CrossFadeInFixedTime(IdleHash, _reaction.CrossFadeOutDuration);
                    _machine.ChangeState(_idle);
                    break;
            }
        }

        public void Exit()
        {
        }

        private void StartPhase(Phase phase, string stateName, float crossFadeDuration)
        {
            _phase = phase;
            _phaseElapsed = 0f;
            _animator.CrossFadeInFixedTime(Animator.StringToHash(stateName), crossFadeDuration);
        }

        private void FaceAttacker(Vector3 attackerPosition)
        {
            Vector3 toAttacker = attackerPosition - _motor.transform.position;
            toAttacker.y = 0f;
            // 공격자와 수평으로 겹치면 향할 방향이 없어 지금 방향 그대로 쓰러진다.
            if (toAttacker.sqrMagnitude < 1e-6f)
            {
                return;
            }

            float yaw = Quaternion.LookRotation(toAttacker).eulerAngles.y;
            _motor.RotateTowards(yaw, 180f);
        }
    }
}
