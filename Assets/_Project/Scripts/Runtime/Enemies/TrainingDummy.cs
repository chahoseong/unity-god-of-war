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
    /// 맞으면 공격자가 있는 쪽과 타격 세기에 맞는 피격 반응을 재생하고, 끝나면 대기로 돌아간다. 맞을 때마다 공격의 피해량만큼 체력이 깎인다.
    /// 반응 중에 다시 맞으면 가장 최근 타격으로 반응을 새로 시작한다. 공격자를 향해 돌지 않는다.
    /// 넉다운시키는 타격에는 공격자를 향해 돌아선 뒤 쓰러졌다가 일어난다. 쓰러져 있는 동안은 맞아도 반응을 바꾸지 않는다.
    /// 자동 공격을 켜면 대기가 정해진 시간 동안 이어질 때마다 공격 패턴을 차례로 휘두른다.
    /// 휘두른 공격이 저스트 패링당하면 공격을 끊고 튕겨 나가는 반응(역경직)을 재생한다. 그동안 맞으면 피격 반응으로 바뀐다.
    /// 체력이 바닥나면 처치된다. 마지막 타격이 들어온 쪽의 처치 반응으로 쓰러지고, 넉다운으로 쓰러지는 몸은 일어나지 않는다.
    /// 처치된 동안은 타격을 받지 않고 락온 대상에서 빠지며 공격하지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterMotor), typeof(CharacterClock), typeof(HitTarget))]
    [RequireComponent(typeof(CharacterHealth), typeof(LockOnTarget))]
    public sealed class TrainingDummy : MonoBehaviour, ITrainingDummyStateContext
    {
        [SerializeField] private Animator _animator;

        [Tooltip("타격 세기·방향별로 재생할 피격 반응.")]
        [SerializeField] private HitReactionSet _hitReactions;

        [Tooltip("휘두른 공격이 저스트 패링당했을 때 재생할 반응. 클립 길이 동안 튕겨 나간 뒤 대기로 돌아간다.")]
        [SerializeField] private HitReaction _deflectedReaction = new HitReaction();

        [Header("Auto Attack")]
        [Tooltip("켜면 대기 중에 공격 패턴을 차례로 휘두른다. 끄면 맞기만 한다. 플레이 중에도 바꿀 수 있다.")]
        [SerializeField] private bool _autoAttack;

        [Tooltip("대기로 돌아온 뒤 다음 공격까지 기다리는 시간(초).")]
        [SerializeField, Min(0f)] private float _attackInterval = 1.5f;

        [Tooltip("차례로 반복할 공격. 공격 데이터의 상태 이름이 더미 애니메이터에 있어야 한다.")]
        [SerializeField] private AttackAction[] _attackPattern = Array.Empty<AttackAction>();

        private readonly StateRegistry _registry = new StateRegistry();
        private CharacterMotor _motor;
        private CharacterClock _clock;
        private CharacterHealth _health;
        private HitTarget _hitTarget;
        private LockOnTarget _lockOnTarget;
        private Weapon _weapon;
        private StateMachine _machine;
        private IdleState _idle;
        private AttackState _attack;
        private HitReactState _hitReact;
        private KnockdownState _knockdown;
        private DeathState _death;

        private float _idleTime;
        private int _nextAttackIndex;

        // Awake가 상태 등록까지 마쳤는지. 검증에서 실패하면 켜짐·꺼짐을 상태에 알리지 않는다.
        private bool _isAssembled;

        CharacterMotor ITrainingDummyStateContext.Motor => _motor;
        Animator ITrainingDummyStateContext.Animator => _animator;
        Weapon ITrainingDummyStateContext.Weapon => _weapon;
        StateMachine ITrainingDummyStateContext.Machine => _machine;
        IStateLocator ITrainingDummyStateContext.States => _registry;

        /// <summary>넉다운으로 쓰러지는 몸이 바닥에 부딪혔다. 카메라 흔들림 같은 연출이 구독한다.</summary>
        public event Action GroundImpacted;

        void ITrainingDummyStateContext.NotifyGroundImpact()
        {
            GroundImpacted?.Invoke();
        }

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

            KnockdownReaction knockdown = _hitReactions.Knockdown;
            if (knockdown == null || knockdown.BeginClip == null || knockdown.EndClip == null)
            {
                throw new InvalidOperationException($"'{name}' has no knockdown clips assigned in its {nameof(HitReactionSet)}.");
            }

            foreach (HitDirection direction in (HitDirection[])Enum.GetValues(typeof(HitDirection)))
            {
                DeathReaction death = _hitReactions.GetDeath(direction);
                if (death == null || death.Clip == null)
                {
                    throw new InvalidOperationException(
                        $"'{name}' has no {direction} death clip assigned in its {nameof(HitReactionSet)}.");
                }
            }

            if (_deflectedReaction == null || _deflectedReaction.Clip == null)
            {
                throw new InvalidOperationException($"'{name}' has no deflected reaction clip assigned.");
            }

            ValidateAttackPattern();

            // 상태가 생성자에서 창구의 값을 읽으므로 상태보다 먼저 준비한다.
            _motor = GetComponent<CharacterMotor>();
            // RequireComponent는 이미 만들어진 프리팹에 컴포넌트를 채워 주지 않으므로 직접 확인한다.
            if (!TryGetComponent(out _clock))
            {
                throw new InvalidOperationException($"'{name}' requires a {nameof(CharacterClock)} component.");
            }

            if (!TryGetComponent(out _health))
            {
                throw new InvalidOperationException($"'{name}' requires a {nameof(CharacterHealth)} component.");
            }

            if (!TryGetComponent(out _lockOnTarget))
            {
                throw new InvalidOperationException($"'{name}' requires a {nameof(LockOnTarget)} component.");
            }

            if (!TryGetComponent(out _hitTarget))
            {
                throw new InvalidOperationException($"'{name}' requires a {nameof(HitTarget)} component.");
            }

            _weapon = FindRequiredWeapon();
            _weapon.Bind(transform, OnHitDealt);
            _machine = new StateMachine();
            _idle = Register(new IdleState(this));
            _attack = Register(new AttackState(this));
            _hitReact = Register(new HitReactState(this));
            _knockdown = Register(new KnockdownState(this));
            _death = Register(new DeathState(this));
            // 판정이 상태를 쓰므로, 검증에 실패해 조립이 덜 된 채로 타격을 받지 않도록 상태를 모두 등록한 뒤에 맡긴다.
            _hitTarget.Bind(HandleHit);

            _isAssembled = true;
        }

        private void OnEnable()
        {
            // Awake에서 상태를 모두 등록한 뒤라 등록된 모든 상태가 켜짐을 받는다.
            if (_isAssembled)
            {
                _registry.OnEnable();
            }
        }

        private void OnDisable()
        {
            if (_isAssembled)
            {
                _registry.OnDisable();
            }
        }

        private void Start()
        {
            // 다른 컴포넌트의 Awake가 모두 끝난 뒤 첫 상태에 진입한다.
            _machine.ChangeState(_idle);
        }

        private void Update()
        {
            // 역경직으로 느려진 동안은 애니메이션도 상태도 공격 간격도 그만큼 느리게 흐른다.
            _animator.speed = _clock.TimeScale;
            _machine.Tick(_clock.DeltaTime);
            UpdateAutoAttack(_clock.DeltaTime);
        }

        // HitTarget에 맡긴 판정. 더미는 막지 않으므로 늘 맞고, 어떤 반응이든 체력은 깎인다.
        // 처치하는 타격도 맞은 것이라 판정은 그대로 Hit이고, 그래서 결정타의 피드백도 모두 난다.
        private HitResult HandleHit(in AttackInfo attack)
        {
            _health.TakeDamage(attack.Damage);
            bool isKilled = _health.IsDepleted;

            // 쓰러져 있는 몸에 서서 맞는 반응을 재생하면 선 자세로 튀므로, 맞기만 하고 넉다운을 이어 간다.
            if (_machine.CurrentState == _knockdown)
            {
                if (isKilled)
                {
                    // 이미 일어나는 중이면 누운 자세로 되돌리지 않고 처치 반응으로 쓰러진다.
                    if (_knockdown.TryStayDown())
                    {
                        StopTakingPart();
                    }
                    else
                    {
                        Die(attack);
                    }
                }

                return new HitResult(HitOutcome.Hit);
            }

            if (attack.Knockdown)
            {
                _knockdown.Prepare(_hitReactions.Knockdown, attack.Attacker.position);
                _machine.ChangeState(_knockdown);
                // 넉다운 타격으로 처치되면 날아가 쓰러지는 무게감은 그대로 두고 일어나지만 않는다.
                if (isKilled)
                {
                    _knockdown.TryStayDown();
                    StopTakingPart();
                }

                return new HitResult(HitOutcome.Hit);
            }

            if (isKilled)
            {
                Die(attack);
                return new HitResult(HitOutcome.Hit);
            }

            HitDirection direction = ResolveHitDirection(attack);

            // 반응 중이어도 같은 상태로 다시 전이해 최신 타격의 반응을 처음부터 재생한다.
            _hitReact.Prepare(_hitReactions.Get(attack.Strength, direction));
            _machine.ChangeState(_hitReact);
            return new HitResult(HitOutcome.Hit);
        }

        // 마지막 타격이 들어온 쪽의 처치 반응으로 쓰러진다. 돌아서지 않는다.
        private void Die(in AttackInfo attack)
        {
            _death.Prepare(_hitReactions.GetDeath(ResolveHitDirection(attack)));
            _machine.ChangeState(_death);
            StopTakingPart();
        }

        // 처치된 몸이 어느 상태로 누워 있든 같게 정리한다. 공격 중이었다면 상태를 나가며 칼 판정이 이미 닫혔다.
        // 자동 공격은 대기 상태에서만 하므로 따로 끄지 않는다.
        private void StopTakingPart()
        {
            _hitTarget.CanBeHit = false;
            _lockOnTarget.IsTargetable = false;
        }

        private HitDirection ResolveHitDirection(in AttackInfo attack)
        {
            return HitDirectionResolver.Resolve(transform.position, transform.forward, attack.Attacker.position);
        }

        /// <summary>
        /// 자기 칼로 친 타격의 판정을 받는다. 저스트 패링당하면 지금 상태와 관계없이 역경직으로 바꾼다.
        /// 공격 중이면 공격 상태를 나가며 칼 판정이 닫힌다. 다른 판정에는 반응하지 않는다.
        /// </summary>
        private void OnHitDealt(HitEvent hitEvent)
        {
            // 쓰러진 뒤에 같은 프레임의 판정이 늦게 와도 넉다운을 끊지 않는다.
            // 처치된 뒤에 늦게 온 판정으로 일어서지 않는다.
            if (hitEvent.Result.Outcome != HitOutcome.Parried || _machine.CurrentState == _knockdown || _health.IsDepleted)
            {
                return;
            }

            _hitReact.Prepare(_deflectedReaction);
            _machine.ChangeState(_hitReact);
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
        private Weapon FindRequiredWeapon()
        {
            Weapon[] weapons = GetComponentsInChildren<Weapon>(true);
            if (weapons.Length != 1)
            {
                throw new InvalidOperationException(
                    $"'{name}' requires exactly one {nameof(Weapon)} in its children, but found {weapons.Length}.");
            }

            return weapons[0];
        }

        private T Register<T>(T state) where T : class, IState
        {
            _registry.Add(state);
            return state;
        }
    }
}
