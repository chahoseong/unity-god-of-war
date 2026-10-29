using System;
using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using UnityEngine;

namespace GodOfWar.Player.States
{
    /// <summary>
    /// 가드로 공격을 막은 상태. 전신으로 막는 동작을 재생하며 공격 반대쪽으로 잠깐 밀려나고,
    /// 이어서 막은 자세에서 가드 자세로 돌아가는 동작을 전신으로 재생한다. 두 동작이 끝날 때까지 제자리에서 버틴다.
    /// 그동안 다른 액션이 끼어들 수 없고, Guard 버튼 입력도 받지 않는다. 끝나는 순간 Guard 버튼이 눌려 있으면
    /// 가드를 다시 올리지 않고 <see cref="GuardState"/>로, 아니면 <see cref="LocomotionState"/>로 돌아간다.
    /// 막는 자세 그대로라 동작 중에 들어온 공격도 막고, 그러면 처음부터 다시 반응한다.
    /// 막기 판정으로만 진입하므로 진입 트리거를 선언하지 않는다.
    /// </summary>
    public sealed class BlockState : IState, ICancellable, IChainable, IDefensible
    {
        // 이보다 가까운 공격자는 방향을 정할 수 없는 것으로 본다.
        private const float MinDirectionSqr = 0.0001f;

        private readonly StateMachine _machine;
        private readonly IStateLocator _locator;
        private readonly IPlayerInput _input;
        private readonly CharacterMotor _motor;
        private readonly PlayerAnimator _animator;
        private readonly GuardSettings _settings;

        private LocomotionState _locomotion;
        private GuardState _guard;

        // 다음 Enter에서 밀려날 방향. 공격자와 겹쳐 방향을 정할 수 없으면 영벡터다.
        private bool _hasPendingAttack;
        private Vector3 _pendingPushDirection;

        private float _elapsed;
        private bool _isReturning;

        // 막았을 때의 밀림. 경과 시간이 밀림 시간에 이르면 밀리지 않는다.
        private Vector3 _pushDirection;
        private float _pushElapsed;

        public BlockState(IPlayerStateContext context, GuardSettings settings)
        {
            _machine = context.Machine;
            _locator = context.States;
            _input = context.Input;
            _motor = context.Motor;
            _animator = context.Animator;
            _settings = settings;
            if (_settings.BlockClip == null)
            {
                throw new InvalidOperationException(
                    $"'{_settings.name}' has no {nameof(GuardSettings.BlockClip)} assigned.");
            }

            if (_settings.BlockReturnClip == null)
            {
                throw new InvalidOperationException(
                    $"'{_settings.name}' has no {nameof(GuardSettings.BlockReturnClip)} assigned.");
            }
        }

        public bool IsCancelWindowOpen => false;

        public DefenseStance DefenseStance => DefenseStance.Guarding;

        /// <summary>다음 진입에서 막은 공격을 정한다. 공격 반대쪽으로 밀려난다. 전이 직전에 호출한다.</summary>
        public void Prepare(in AttackInfo attack)
        {
            Vector3 toAttacker = attack.Attacker.position - _motor.transform.position;
            toAttacker.y = 0f;
            _pendingPushDirection = toAttacker.sqrMagnitude < MinDirectionSqr ? Vector3.zero : -toAttacker.normalized;
            _hasPendingAttack = true;
        }

        public void Enter()
        {
            // 생성 시점에는 다른 상태가 아직 등록되지 않았을 수 있어 첫 진입에서 찾는다.
            _locomotion ??= _locator.Get<LocomotionState>();
            _guard ??= _locator.Get<GuardState>();

            if (!_hasPendingAttack)
            {
                throw new InvalidOperationException(
                    $"{nameof(BlockState)} was entered without a blocked attack. Call {nameof(Prepare)} first.");
            }

            _hasPendingAttack = false;
            _pushDirection = _pendingPushDirection;
            _pushElapsed = _pushDirection == Vector3.zero ? _settings.BlockPushDuration : 0f;
            _elapsed = 0f;
            _isReturning = false;
            _animator.PlayBlock(_settings.BlockCrossFadeInDuration);
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            _motor.Move(TickPush(deltaTime), deltaTime);

            if (!_isReturning)
            {
                if (_elapsed < _settings.BlockClip.length)
                {
                    return;
                }

                _elapsed -= _settings.BlockClip.length;
                _isReturning = true;
                _animator.PlayBlockReturn();
                return;
            }

            if (_elapsed < _settings.BlockReturnClip.length)
            {
                return;
            }

            if (_input.IsHeld(PlayerAction.Guard))
            {
                _guard.PrepareReturnFromBlock();
                _machine.ChangeState(_guard);
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
            // 끝날 때 버튼이 눌려 있는지로 가드에 돌아갈지 정하므로, 그동안의 Guard 입력은 받지 않는다(선입력으로도 남기지 않는다).
            return e.Action == PlayerAction.Guard
                && (e.Phase == ActionPhase.Pressed || e.Phase == ActionPhase.Held);
        }

        /// <summary>
        /// 이번 프레임의 밀림 속도. 시작이 가장 빠르고 끝에서 멈추도록 이동 거리를 d·(2u − u²) (u = 경과/시간) 곡선으로 나눈다.
        /// </summary>
        private Vector3 TickPush(float deltaTime)
        {
            float duration = _settings.BlockPushDuration;
            if (_pushElapsed >= duration || deltaTime <= 0f)
            {
                return Vector3.zero;
            }

            float from = PushProgress(_pushElapsed / duration);
            _pushElapsed = Mathf.Min(_pushElapsed + deltaTime, duration);
            float to = PushProgress(_pushElapsed / duration);
            return _pushDirection * ((to - from) * _settings.BlockPushDistance / deltaTime);
        }

        private static float PushProgress(float u)
        {
            return u * (2f - u);
        }
    }
}
