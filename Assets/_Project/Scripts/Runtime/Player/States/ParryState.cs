using System;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using GodOfWar.Player.Routing;
using GodOfWar.Player.Settings;
using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 저스트 패링에 성공한 상태. 상대를 밀쳐 내는 전신 동작을 재생하며 정해 둔 만큼 나아가고,
    /// 막는 자세로 끝나면 <see cref="LocomotionState"/>로 돌아간다.
    /// 카운터 시각(<see cref="GuardSettings.ParryCounterStart"/>) 전에는 다른 액션이 끼어들 수 없다. 그사이 공격 버튼(약·강)을 누르면
    /// 예약해 두었다가 카운터 시각에 패링한 상대에게 <see cref="CounterState"/>로 이어진다. 다른 버튼은 선입력으로 남는다.
    /// 카운터 시각부터는 공격권을 가져온 뒤의 여유라, 공격은 곧바로 카운터로 나가고 회피·새로 누른 가드로 끊을 수 있다.
    /// 이동 입력으로는 끊기지 않는다.
    /// 패링에 쓴 Guard 누름은 끝났으므로, 버튼을 계속 누르고 있어도 가드로 돌아가지 않는다(다시 누르면 가드한다).
    /// 막는 자세 그대로라 동작 중에 들어온 공격은 막는다(다시 패링하지는 않는다).
    /// 패링 판정으로만 진입하므로 진입 트리거를 선언하지 않는다.
    /// </summary>
    public sealed class ParryState : IState, ICancellable, IChainable, IDefensible
    {
        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly CharacterMotor _motor;
        private readonly PlayerAnimator _animator;
        private readonly HitReaction _reaction;
        private readonly GuardSettings _settings;
        private readonly ActionMovement _movement = new ActionMovement();

        private LocomotionState _locomotion;
        private CounterState _counter;

        // 다음 Enter에서 카운터로 칠 상대(패링한 공격자).
        private Transform _pendingAttacker;

        private Transform _attacker;
        private float _elapsed;
        private bool _hasMovement;
        private bool _hasCounterReserved;
        private bool _isCounterWindowOpen;

        public ParryState(IPlayerStateContext context, GuardSettings settings)
        {
            _machine = context.Machine;
            _locator = context.States;
            _motor = context.Motor;
            _animator = context.Animator;
            _settings = settings;
            _reaction = settings.Parry;
            if (_reaction == null || _reaction.Clip == null)
            {
                throw new InvalidOperationException(
                    $"'{settings.name}' has no {nameof(GuardSettings.Parry)} clip assigned.");
            }
        }

        public bool IsCancelWindowOpen => _isCounterWindowOpen;

        public DefenseStance DefenseStance => DefenseStance.Guarding;

        /// <summary>다음 진입에서 패링한 공격자를 정한다. 카운터를 칠 상대다. 전이 직전에 호출한다.</summary>
        public void Prepare(Transform attacker)
        {
            _pendingAttacker = attacker != null ? attacker : throw new ArgumentNullException(nameof(attacker));
        }

        public void Enter()
        {
            // 생성 시점에는 다른 상태가 아직 등록되지 않았을 수 있어 첫 진입에서 찾는다.
            _locomotion ??= _locator.Get<LocomotionState>();
            _counter ??= _locator.Get<CounterState>();

            _attacker = _pendingAttacker != null ? _pendingAttacker : throw new InvalidOperationException(
                $"{nameof(ParryState)} was entered without an attacker. Call {nameof(Prepare)} first.");
            _pendingAttacker = null;
            _elapsed = 0f;
            _hasCounterReserved = false;
            _isCounterWindowOpen = false;

            _animator.CrossFade(Animator.StringToHash(_reaction.StateName), _reaction.CrossFadeInDuration);

            // 변위는 패링을 시작한 순간의 몸 정면 기준으로 구워져 있다.
            _hasMovement = _reaction.Movement != null;
            if (_hasMovement)
            {
                _movement.Begin(_reaction.Movement, _motor.transform.eulerAngles.y);
            }
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            // 밀쳐 내며 내딛는다. 상대에게 막히면 그 자리에 멈춘다.
            _motor.Move(_hasMovement ? _movement.Tick(deltaTime) : Vector3.zero, deltaTime);

            if (!_isCounterWindowOpen)
            {
                if (_elapsed < _settings.ParryCounterStart)
                {
                    return;
                }

                _isCounterWindowOpen = true;
                if (_hasCounterReserved && TryCounter())
                {
                    return;
                }
            }

            if (_elapsed < _reaction.Duration)
            {
                return;
            }

            _animator.ReturnToLocomotion(_reaction.CrossFadeOutDuration);
            _machine.ChangeState(_locomotion);
        }

        public void Exit()
        {
        }

        public bool TryChain(PlayerActionEvent e)
        {
            bool isAttack = e.Phase == ActionPhase.Pressed
                && (e.Action == PlayerAction.LightAttack || e.Action == PlayerAction.HeavyAttack);

            // 카운터 구간에는 공격을 곧바로 카운터로 내보내고, 나머지는 라우터에 넘겨 회피·새로 누른 가드로 끊게 한다.
            // 상대가 사라져 카운터를 칠 수 없으면 공격도 라우터에 넘긴다.
            if (_isCounterWindowOpen)
            {
                return isAttack && TryCounter();
            }

            // 밀쳐 내는 동작을 보여 주도록 카운터는 카운터 시각에 나간다. 약·강 모두 같은 카운터다.
            if (isAttack)
            {
                _hasCounterReserved = true;
                return true;
            }

            // 패링 동작 중의 Guard 입력은 받지 않는다. 선입력으로 남으면 카운터 구간이 열리는 순간 그 누름으로 패링 윈도우가 열린다.
            return e.Action == PlayerAction.Guard
                && (e.Phase == ActionPhase.Pressed || e.Phase == ActionPhase.Held);
        }

        // 패링한 상대에게 카운터를 친다. 그사이 상대가 사라졌으면 치지 않는다.
        private bool TryCounter()
        {
            if (_attacker == null)
            {
                return false;
            }

            _counter.Prepare(_attacker);
            _machine.ChangeState(_counter);
            return true;
        }
    }
}
