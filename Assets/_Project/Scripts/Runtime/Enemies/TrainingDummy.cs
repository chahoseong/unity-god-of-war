using System;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using GodOfWar.Enemies.States;
using UnityEngine;

namespace GodOfWar.Enemies
{
    /// <summary>
    /// 공격을 받기만 하는 훈련용 더미. 상태 머신의 컴포지션 루트이자 상태들이 속한 Context다.
    /// 맞으면 공격자가 있는 쪽과 타격 세기에 맞는 피격 반응을 재생하고, 끝나면 대기로 돌아간다.
    /// 반응 중에 다시 맞으면 가장 최근 타격으로 반응을 새로 시작한다. 공격자를 향해 돌지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class TrainingDummy : MonoBehaviour, IHittable, ITrainingDummyStateContext
    {
        [SerializeField] private Animator _animator;

        [Tooltip("타격 세기·방향별로 재생할 피격 반응.")]
        [SerializeField] private HitReactionSet _hitReactions;

        private readonly StateRegistry _registry = new StateRegistry();
        private CharacterMotor _motor;
        private StateMachine _machine;
        private HitReactState _hitReact;

        CharacterMotor ITrainingDummyStateContext.Motor => _motor;
        Animator ITrainingDummyStateContext.Animator => _animator;
        StateMachine ITrainingDummyStateContext.Machine => _machine;
        IStateLocator ITrainingDummyStateContext.States => _registry;

        private void Awake()
        {
            if (_animator == null)
            {
                throw new InvalidOperationException($"'{name}' has no Animator assigned.");
            }

            if (_hitReactions == null)
            {
                throw new InvalidOperationException($"'{name}' has no {nameof(HitReactionSet)} assigned.");
            }

            // 상태가 생성자에서 창구의 값을 읽으므로 상태보다 먼저 준비한다.
            _motor = GetComponent<CharacterMotor>();
            _machine = new StateMachine();
            Register(new IdleState(this));
            _hitReact = Register(new HitReactState(this));
        }

        private void Start()
        {
            // 다른 컴포넌트의 Awake가 모두 끝난 뒤 첫 상태에 진입한다.
            _machine.ChangeState(_registry.Get<IdleState>());
        }

        private void Update()
        {
            _machine.Tick(Time.deltaTime);
        }

        public HitResult ReceiveHit(in AttackInfo attack)
        {
            HitDirection direction = HitDirectionResolver.Resolve(
                transform.position, transform.forward, attack.Attacker.position);

            // 반응 중이어도 같은 상태로 다시 전이해 최신 타격의 반응을 처음부터 재생한다.
            _hitReact.Prepare(_hitReactions.Get(attack.Strength, direction));
            _machine.ChangeState(_hitReact);
            return new HitResult(HitOutcome.Hit);
        }

        private T Register<T>(T state) where T : class, IState
        {
            _registry.Add(state);
            return state;
        }
    }
}
