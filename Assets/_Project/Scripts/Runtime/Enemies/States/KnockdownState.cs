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
    /// 이 상태에 있는 동안의 타격으로는 반응을 바꾸지 않는다. 그 판단은 판정을 받는 쪽이 이 상태를 보고 한다.
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
        private Phase _phase;
        // 지금 단계를 시작한 뒤 흐른 시간(초).
        private float _phaseElapsed;

        public KnockdownState(ITrainingDummyStateContext context)
        {
            _machine = context.Machine;
            _locator = context.States;
            _motor = context.Motor;
            _animator = context.Animator;
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

            switch (_phase)
            {
                case Phase.Falling when _phaseElapsed >= _reaction.BeginDuration:
                    // 누워 있을 시간이 없으면 쓰러지자마자 일어난다.
                    if (_reaction.LyingDuration > 0f)
                    {
                        StartPhase(Phase.Lying, _reaction.LoopStateName, _reaction.PhaseCrossFadeDuration);
                    }
                    else
                    {
                        StartPhase(Phase.GettingUp, _reaction.EndStateName, _reaction.PhaseCrossFadeDuration);
                    }

                    break;
                case Phase.Lying when _phaseElapsed >= _reaction.LyingDuration:
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
