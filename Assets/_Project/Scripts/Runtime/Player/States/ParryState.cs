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
    /// 저스트 패링에 성공한 상태. 전신 패링 동작을 제자리에서 재생하고, 이어서 막은 자세에서 가드 자세로 돌아가는 동작을
    /// 전신으로 재생한 뒤 <see cref="LocomotionState"/>로 돌아간다.
    /// 패링 동작 중에 공격 버튼(약·강)을 누르면 예약해 두었다가, 패링 동작이 끝날 때 돌아가는 동작 대신 패링한 상대에게
    /// <see cref="CounterState"/>로 이어진다. 패링 동작 중에는 다른 액션이 끼어들 수 없고, 누른 버튼은 선입력으로 남는다.
    /// 돌아가는 동작은 공격권을 가져온 뒤의 여유라 공격·회피·새로 누른 가드로 끊을 수 있다. 이동 입력으로는 끊기지 않는다.
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

        private LocomotionState _locomotion;
        private CounterState _counter;

        // 다음 Enter에서 카운터로 칠 상대(패링한 공격자).
        private Transform _pendingAttacker;

        private Transform _attacker;
        private float _elapsed;
        private bool _hasCounterReserved;
        private bool _isReturning;

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

            if (settings.BlockReturnClip == null)
            {
                throw new InvalidOperationException(
                    $"'{settings.name}' has no {nameof(GuardSettings.BlockReturnClip)} assigned.");
            }
        }

        public bool IsCancelWindowOpen => _isReturning;

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
            _isReturning = false;

            _animator.CrossFade(Animator.StringToHash(_reaction.StateName), _reaction.CrossFadeInDuration);
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            // 공격권을 가져온 순간이라 밀리지 않고 제자리에서 버틴다.
            _motor.Move(Vector3.zero, deltaTime);

            if (!_isReturning)
            {
                if (_elapsed < _reaction.Duration)
                {
                    return;
                }

                // 그사이 상대가 사라졌으면 카운터 없이 돌아간다.
                if (_hasCounterReserved && _attacker != null)
                {
                    _counter.Prepare(_attacker);
                    _machine.ChangeState(_counter);
                    return;
                }

                // 패링 동작의 끝 자세가 돌아가는 동작의 첫 자세와 같아 바로 넘긴다.
                _elapsed -= _reaction.Duration;
                _isReturning = true;
                _animator.PlayBlockReturn();
                return;
            }

            if (_elapsed < _settings.BlockReturnClip.length)
            {
                return;
            }

            _animator.ReturnToLocomotion(_settings.BlockCrossFadeOutDuration);
            _machine.ChangeState(_locomotion);
        }

        public void Exit()
        {
        }

        public bool TryChain(PlayerActionEvent e)
        {
            // 돌아가는 동작 중에는 받지 않고 라우터에 넘겨, 공격·회피·새로 누른 가드로 끊게 한다.
            if (_isReturning)
            {
                return false;
            }

            // 패링 동작을 끝까지 보여 주도록 카운터는 동작이 끝날 때 나간다. 약·강 모두 같은 카운터다.
            if (e.Phase == ActionPhase.Pressed
                && (e.Action == PlayerAction.LightAttack || e.Action == PlayerAction.HeavyAttack))
            {
                _hasCounterReserved = true;
                return true;
            }

            // 패링 동작 중의 Guard 입력은 받지 않는다. 선입력으로 남으면 돌아가는 동작이 시작되는 순간 그 누름으로 패링 윈도우가 열린다.
            return e.Action == PlayerAction.Guard
                && (e.Phase == ActionPhase.Pressed || e.Phase == ActionPhase.Held);
        }
    }
}
