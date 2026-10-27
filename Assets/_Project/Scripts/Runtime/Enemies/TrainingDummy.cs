using System;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using GodOfWar.Enemies.States;
using UnityEngine;

namespace GodOfWar.Enemies
{
    /// <summary>
    /// 피격·방어 테스트용 훈련용 더미. 상태 머신의 컴포지션 루트이자 상태들이 속한 Context다.
    /// 맞으면 공격자가 있는 쪽과 타격 세기에 맞는 피격 반응을 재생하고, 끝나면 대기로 돌아간다.
    /// 반응 중에 다시 맞으면 가장 최근 타격으로 반응을 새로 시작한다. 공격자를 향해 돌지 않는다.
    /// 자동 공격을 켜면 대기가 정해진 시간 동안 이어질 때마다 공격 패턴을 차례로 휘두른다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class TrainingDummy : MonoBehaviour, IHittable, ITrainingDummyStateContext
    {
        [SerializeField] private Animator _animator;

        [Tooltip("타격 세기·방향별로 재생할 피격 반응.")]
        [SerializeField] private HitReactionSet _hitReactions;

        [Header("Auto Attack")]
        [Tooltip("켜면 대기 중에 공격 패턴을 차례로 휘두른다. 끄면 맞기만 한다. 플레이 중에도 바꿀 수 있다.")]
        [SerializeField] private bool _autoAttack;

        [Tooltip("대기로 돌아온 뒤 다음 공격까지 기다리는 시간(초).")]
        [SerializeField, Min(0f)] private float _attackInterval = 1.5f;

        [Tooltip("차례로 반복할 공격. 공격 데이터의 상태 이름이 더미 애니메이터에 있어야 한다.")]
        [SerializeField] private AttackAction[] _attackPattern = Array.Empty<AttackAction>();

        private readonly StateRegistry _registry = new StateRegistry();
        private CharacterMotor _motor;
        private Hitbox _hitbox;
        private StateMachine _machine;
        private IdleState _idle;
        private AttackState _attack;
        private HitReactState _hitReact;

        private float _idleTime;
        private int _nextAttackIndex;

        CharacterMotor ITrainingDummyStateContext.Motor => _motor;
        Animator ITrainingDummyStateContext.Animator => _animator;
        Hitbox ITrainingDummyStateContext.Hitbox => _hitbox;
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

            ValidateAttackPattern();

            // 상태가 생성자에서 창구의 값을 읽으므로 상태보다 먼저 준비한다.
            _motor = GetComponent<CharacterMotor>();
            _hitbox = FindRequiredHitbox();
            _machine = new StateMachine();
            _idle = Register(new IdleState(this));
            _attack = Register(new AttackState(this));
            _hitReact = Register(new HitReactState(this));
        }

        private void Start()
        {
            // 다른 컴포넌트의 Awake가 모두 끝난 뒤 첫 상태에 진입한다.
            _machine.ChangeState(_idle);
        }

        private void Update()
        {
            _machine.Tick(Time.deltaTime);
            UpdateAutoAttack(Time.deltaTime);
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

        /// <summary>대기가 공격 간격만큼 이어지면 패턴의 다음 공격을 시작한다. 대기를 벗어나면 기다린 시간을 처음부터 다시 잰다.</summary>
        private void UpdateAutoAttack(float deltaTime)
        {
            if (!_autoAttack || _machine.CurrentState != _idle)
            {
                _idleTime = 0f;
                return;
            }

            _idleTime += deltaTime;
            if (_idleTime < _attackInterval)
            {
                return;
            }

            _idleTime = 0f;
            // 플레이 중 패턴을 고치다 빈 칸이 생기면 그 차례는 건너뛰고 계속 대기한다.
            AttackAction action = NextAttack();
            if (action == null)
            {
                return;
            }

            _attack.Prepare(action);
            _machine.ChangeState(_attack);
        }

        private AttackAction NextAttack()
        {
            if (_attackPattern == null || _attackPattern.Length == 0)
            {
                return null;
            }

            // 플레이 중 패턴이 줄어들어도 처음부터 다시 돈다.
            int index = _nextAttackIndex % _attackPattern.Length;
            _nextAttackIndex = index + 1;
            return _attackPattern[index];
        }

        private void ValidateAttackPattern()
        {
            if (!_autoAttack)
            {
                return;
            }

            if (_attackPattern == null || _attackPattern.Length == 0)
            {
                throw new InvalidOperationException($"'{name}' has auto attack on but no attack pattern.");
            }

            for (int i = 0; i < _attackPattern.Length; i++)
            {
                if (_attackPattern[i] == null)
                {
                    throw new InvalidOperationException($"'{name}' has an empty attack pattern slot at {i}.");
                }
            }
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

        private T Register<T>(T state) where T : class, IState
        {
            _registry.Add(state);
            return state;
        }
    }
}
