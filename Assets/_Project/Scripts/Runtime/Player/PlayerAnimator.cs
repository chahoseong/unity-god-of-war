using System;
using GodOfWar.Characters;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace GodOfWar.Player
{
    /// <summary>
    /// 플레이어 애니메이터의 유일한 창구. 상태는 이 컴포넌트를 통해서만 클립을 재생한다.
    /// 매 프레임 모터 속도와 애니메이터 상태 태그를 읽어 이동 파라미터, 검 자세·가드 레이어, 가드 리그 가중치를 정한다.
    /// 레이어 구성은 이 컴포넌트만 안다.
    /// 애니메이터와 파라미터·가중치 보간은 캐릭터 시계를 따라, 역경직 동안 함께 느려진다.
    /// </summary>
    /// <remarks>
    /// 상태가 이번 프레임에 시작한 크로스페이드를 같은 프레임에 읽도록 PlayerController 뒤에 실행된다.
    /// 애니메이터는 Update와 LateUpdate 사이에 평가되므로 LateUpdate에서 쓰면 한 프레임 늦게 반영된다.
    /// </remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.PlayerAnimator)]
    [RequireComponent(typeof(CharacterMotor), typeof(CharacterClock))]
    public sealed class PlayerAnimator : MonoBehaviour
    {
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
        private static readonly int FullBodyTagHash = Animator.StringToHash("FullBody");

        private static readonly int GuardBeginTagHash = Animator.StringToHash("GuardBegin");
        private static readonly int GuardTagHash = Animator.StringToHash("Guard");
        private static readonly int GuardEndTagHash = Animator.StringToHash("GuardEnd");

        private static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
        private static readonly int GuardBeginHash = Animator.StringToHash("Guard_Begin");
        private static readonly int GuardEndHash = Animator.StringToHash("Guard_End");
        private static readonly int BlockHash = Animator.StringToHash("Block");
        private static readonly int BlockReturnHash = Animator.StringToHash("BlockReturn");
        private static readonly int GuardLoopHash = Animator.StringToHash("Guard_Loop");

        // 이동 중 검을 든 팔·손 자세를 덮어쓰는 레이어. 전신 클립(FullBody 태그) 재생 중에는 끈다.
        private static readonly string[] SwordPoseLayerNames = { "SwordArm", "SwordHand" };

        // 이동하는 다리 위에 가드 자세를 덮어쓰는 상체 레이어. 전신 클립 재생 중에는 끈다.
        private const string GuardLayerName = "Guard";

        [SerializeField] private Animator _animator;

        [Tooltip("가드 중 걷기 클립의 골반 흔들림이 상체로 번지지 않게 척추를 고정하는 리그. 가중치는 Guard 레이어에 맞춰 관리한다.")]
        [SerializeField] private Rig _guardRig;

        [Tooltip("이동 파라미터(MoveX, MoveZ)가 모터 속도를 따라가는 댐핑 시간(초).")]
        [SerializeField, Min(0f)] private float _moveDampTime = 0.1f;

        [Tooltip("검 든 자세 레이어가 꺼지고 켜지는 데 걸리는 시간(초).")]
        [SerializeField, Min(0.01f)] private float _swordPoseBlendDuration = 0.1f;

        [Tooltip("척추 고정 리그 가중치가 0에서 1까지 바뀌는 데 걸리는 최소 시간(초). 전신 동작에서 가드로 들어올 때 척추가 한 프레임에 튀지 않게 한다.")]
        [SerializeField, Min(0.01f)] private float _guardRigBlendDuration = 0.15f;

        private CharacterMotor _motor;
        private CharacterClock _clock;
        private int[] _swordPoseLayers;
        private float[] _swordPoseWeights;
        private int _guardLayer = -1;

        /// <summary>기본 레이어에서 <paramref name="stateHash"/> 상태로 크로스페이드한다. 공격·회피처럼 상태가 클립을 고를 때 쓴다.</summary>
        public void CrossFade(int stateHash, float duration)
        {
            _animator.CrossFadeInFixedTime(stateHash, duration);
        }

        /// <summary>이동 블렌드 트리로 돌아간다.</summary>
        public void ReturnToLocomotion(float duration)
        {
            _animator.CrossFadeInFixedTime(LocomotionHash, duration);
        }

        /// <summary>상체 가드 레이어에서 가드를 올린다. 다리는 이동 클립으로 걷게 한다.</summary>
        public void RaiseGuard(float duration)
        {
            EnterGuard(GuardBeginHash, duration);
        }

        /// <summary>
        /// 공격을 막는 반응을 전신으로 재생한다. 충격을 몸 전체로 받도록 다리까지 덮고, 그동안 상체 가드 레이어는 꺼진다.
        /// 반응 중에 다시 막으면 처음부터 다시 재생한다. 반응이 끝나면 <see cref="PlayBlockReturn"/>으로 이어 간다.
        /// </summary>
        public void PlayBlock(float duration)
        {
            // 이미 같은 상태로 크로스페이드하는 중이면 무시되므로, 반응 중에는 처음부터 다시 튼다.
            if (GetIncomingStateInfo(0).shortNameHash == BlockHash)
            {
                _animator.Play(BlockHash, 0, 0f);
                return;
            }

            _animator.CrossFadeInFixedTime(BlockHash, duration, 0);
        }

        /// <summary>
        /// 막는 반응을 마치고, 막은 자세에서 가드 자세로 돌아가는 동작을 전신으로 이어 간다.
        /// 몸을 틀며 골반까지 돌아가는 동작이라 상체 레이어로는 토대가 어긋난다. 첫 자세가 막는 반응의 끝 자세와 같아 바로 넘긴다.
        /// </summary>
        public void PlayBlockReturn()
        {
            _animator.Play(BlockReturnHash, 0, 0f);
        }

        /// <summary>전신으로 돌아가는 동작을 마치고, 다리는 이동 클립으로, 상체는 가드 대기 자세로 넘긴다. 끝 자세가 대기 자세와 같아 상체는 바로 넘긴다.</summary>
        public void FinishBlockReturn(float duration)
        {
            _animator.Play(GuardLoopHash, _guardLayer, 0f);
            // 가중치를 서서히 올리면 그사이 아래 레이어의 상체 자세가 섞인다.
            _animator.SetLayerWeight(_guardLayer, 1f);
            _animator.CrossFadeInFixedTime(LocomotionHash, duration, 0);
        }

        /// <summary>상체 가드 레이어에서 가드를 내린다. 전신 클립으로 캔슬되면 레이어가 꺼져 보이지 않는다.</summary>
        public void LowerGuard(float duration)
        {
            _animator.CrossFadeInFixedTime(GuardEndHash, duration, _guardLayer);
        }

        /// <summary>다리는 이동 클립으로 돌리고, 상체 가드 레이어는 <paramref name="guardStateHash"/>로 시작한다.</summary>
        private void EnterGuard(int guardStateHash, float duration)
        {
            // 공격 후딜에서 캔슬되거나 전신 반응에서 돌아오면 다리가 그 클립에 남아 있으므로 이동 클립으로 되돌린다.
            // 이미 이동 클립이면 블렌드 트리를 처음부터 다시 틀지 않는다.
            if (_animator.GetCurrentAnimatorStateInfo(0).shortNameHash != LocomotionHash
                || _animator.IsInTransition(0))
            {
                _animator.CrossFadeInFixedTime(LocomotionHash, duration, 0);
            }

            _animator.CrossFadeInFixedTime(guardStateHash, duration, _guardLayer);
        }

        private void Awake()
        {
            if (_animator == null)
            {
                throw new InvalidOperationException($"'{name}' has no Animator assigned.");
            }

            if (_guardRig == null)
            {
                throw new InvalidOperationException($"'{name}' has no guard Rig assigned.");
            }

            _motor = GetComponent<CharacterMotor>();
            // RequireComponent는 이미 만들어진 프리팹에 컴포넌트를 채워 주지 않으므로 직접 확인한다.
            if (!TryGetComponent(out _clock))
            {
                throw new InvalidOperationException($"'{name}' requires a {nameof(CharacterClock)} component.");
            }
        }

        private void Start()
        {
            // 애니메이터 레이어는 자식 Animator가 초기화된 뒤에 읽는다.
            FindSwordPoseLayers();
            _guardLayer = FindLayer(GuardLayerName);
        }

        private void Update()
        {
            // 이번 프레임에 평가될 애니메이터가 캐릭터 시계의 진행 비율로 재생되게 한다.
            _animator.speed = _clock.TimeScale;
            float deltaTime = _clock.DeltaTime;

            Vector3 localVelocity = transform.InverseTransformDirection(_motor.HorizontalVelocity);
            _animator.SetFloat(MoveXHash, localVelocity.x, _moveDampTime, deltaTime);
            _animator.SetFloat(MoveZHash, localVelocity.z, _moveDampTime, deltaTime);

            bool isFullBody = IsFullBodyPlaying();
            UpdateSwordPoseLayers(isFullBody, deltaTime);
            UpdateGuardLayer(isFullBody, deltaTime);
        }

        /// <summary>검 든 자세 레이어를 찾고, 애니메이터에 설정된 기본 가중치를 켜졌을 때의 가중치로 기억한다.</summary>
        private void FindSwordPoseLayers()
        {
            _swordPoseLayers = new int[SwordPoseLayerNames.Length];
            _swordPoseWeights = new float[SwordPoseLayerNames.Length];
            for (int i = 0; i < SwordPoseLayerNames.Length; i++)
            {
                int layer = FindLayer(SwordPoseLayerNames[i]);
                _swordPoseLayers[i] = layer;
                _swordPoseWeights[i] = _animator.GetLayerWeight(layer);
            }
        }

        private int FindLayer(string layerName)
        {
            int layer = _animator.GetLayerIndex(layerName);
            if (layer < 0)
            {
                throw new InvalidOperationException($"'{_animator.name}' has no '{layerName}' animator layer.");
            }

            return layer;
        }

        private bool IsFullBodyPlaying()
        {
            return GetIncomingStateInfo(0).tagHash == FullBodyTagHash;
        }

        // 크로스페이드 중에는 들어가는 상태를 기준으로 삼아 레이어를 미리 끄거나 켠다.
        private AnimatorStateInfo GetIncomingStateInfo(int layer)
        {
            return _animator.IsInTransition(layer)
                ? _animator.GetNextAnimatorStateInfo(layer)
                : _animator.GetCurrentAnimatorStateInfo(layer);
        }

        private void UpdateSwordPoseLayers(bool isFullBody, float deltaTime)
        {
            for (int i = 0; i < _swordPoseLayers.Length; i++)
            {
                int layer = _swordPoseLayers[i];
                float target = isFullBody ? 0f : _swordPoseWeights[i];
                float step = _swordPoseWeights[i] / _swordPoseBlendDuration * deltaTime;
                _animator.SetLayerWeight(layer, Mathf.MoveTowards(_animator.GetLayerWeight(layer), target, step));
            }
        }

        private void UpdateGuardLayer(bool isFullBody, float deltaTime)
        {
            // 가드를 내리는 클립은 끝까지 보여 주고, 끝난 뒤에는 마지막 포즈를 유지한 채 가중치만 내린다.
            // 모션 없는 상태로 넘기면 Write Defaults 때문에 상체가 기본 포즈 쪽으로 끌려간다.
            AnimatorStateInfo state = GetIncomingStateInfo(_guardLayer);
            float progress = Mathf.Clamp01(state.normalizedTime);
            bool isRaised = state.tagHash == GuardBeginTagHash
                || state.tagHash == GuardTagHash
                || (state.tagHash == GuardEndTagHash && state.normalizedTime < 1f);
            float target = !isFullBody && isRaised ? 1f : 0f;

            float step = deltaTime / _swordPoseBlendDuration;
            float layerWeight = Mathf.MoveTowards(_animator.GetLayerWeight(_guardLayer), target, step);
            _animator.SetLayerWeight(_guardLayer, layerWeight);

            // 척추 고정 기준은 Loop 자세다. 몸을 트는 Begin·End 동안은 진행도만큼만 고정해 클립의 회전을 살린다.
            float stabilize = state.tagHash == GuardBeginTagHash ? progress
                : state.tagHash == GuardTagHash ? 1f
                : state.tagHash == GuardEndTagHash ? 1f - progress
                : 0f;
            // 상태가 바뀌는 순간 목표가 크게 달라지면 척추가 한 프레임에 튀므로, 정해진 속도 이상으로는 따라가지 않는다.
            _guardRig.weight = Mathf.MoveTowards(
                _guardRig.weight, stabilize * layerWeight, deltaTime / _guardRigBlendDuration);
        }
    }
}
