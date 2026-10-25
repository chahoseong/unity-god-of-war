using GodOfWar.Characters;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 콤보를 이루는 공격 한 타의 액션 데이터.
    /// 공격 클립이 끝나면 복귀 클립으로 이어지고, 연계 구간 안의 입력은 <see cref="Next"/> 또는 다른 버튼의 1타로 이어진다.
    /// 시각은 모두 액션 시작부터 잰 초다.
    /// </summary>
    [CreateAssetMenu(menuName = "GodOfWar/Combat/Attack Action", fileName = "AttackAction")]
    public sealed class AttackAction : ScriptableObject
    {
        [Tooltip("공격 동작을 재생할 애니메이터 상태 이름.")]
        [SerializeField] private string _stateName;

        [Tooltip("후속 입력이 없을 때 이어서 재생할 복귀 상태 이름.")]
        [SerializeField] private string _returnStateName;

        [Tooltip("공격 상태의 클립. 길이가 공격 구간의 길이다.")]
        [SerializeField] private AnimationClip _clip;

        [Tooltip("복귀 상태의 클립. 길이가 복귀 구간의 길이다.")]
        [SerializeField] private AnimationClip _returnClip;

        [Tooltip("이 액션으로 들어올 때(이전 상태 → 공격 상태), 공격에서 복귀로, 복귀에서 이동 상태로 넘어갈 때의 크로스페이드 시간(초). "
                 + "다른 타에서 이어질 때는 나가는 타가 아니라 들어오는 이 액션의 값이 쓰인다.")]
        [SerializeField, Min(0f)] private float _crossFadeDuration = 0.1f;

        [Tooltip("이 시각까지 목표 방향으로 몸을 돌린다. 보통 선딜레이(WindUp)가 끝나는 시각이다.")]
        [SerializeField, Min(0f)] private float _trackingEnd;

        [Tooltip("스윙이 끝나 후딜레이 캔슬 구간이 열리는 시각. 이때부터 액션이 끝날 때까지 회피 등 다른 액션으로 캔슬할 수 있다.")]
        [SerializeField, Min(0f)] private float _recoveryStart;

        [Tooltip("후속 입력을 받기 시작하는 시각.")]
        [SerializeField, Min(0f)] private float _comboWindowStart;

        [Tooltip("후속 입력을 받는 마지막 시각. 공격 클립 길이를 넘겨 복귀 구간까지 둘 수 있다.")]
        [SerializeField, Min(0f)] private float _comboWindowEnd;

        [Tooltip("무기 판정이 열리는 시각. 조준이 끝난 뒤여야 한다.")]
        [SerializeField, Min(0f)] private float _hitWindowStart;

        [Tooltip("무기 판정이 닫히는 시각. 후딜레이 캔슬 구간이 열리기 전이어야 한다.")]
        [SerializeField, Min(0f)] private float _hitWindowEnd;

        [Tooltip("이 공격에 맞은 대상에게 전하는 타격 세기. 어떤 반응을 낼지는 피격자가 정한다.")]
        [SerializeField] private HitStrength _hitStrength;

        [Tooltip("공격 구간 동안의 전진 이동.")]
        [SerializeField] private BakedRootMotion _movement;

        [Tooltip("복귀 구간 동안의 이동.")]
        [SerializeField] private BakedRootMotion _returnMovement;

        [Tooltip("같은 버튼을 다시 눌렀을 때 이어지는 다음 타. 비우면 같은 버튼은 1타부터 다시 시작한다.")]
        [SerializeField] private AttackAction _next;

        public string StateName => _stateName;
        public string ReturnStateName => _returnStateName;
        public AnimationClip Clip => _clip;
        public AnimationClip ReturnClip => _returnClip;
        public float CrossFadeDuration => _crossFadeDuration;
        public float TrackingEnd => _trackingEnd;
        public float RecoveryStart => _recoveryStart;
        public float ComboWindowStart => _comboWindowStart;
        public float ComboWindowEnd => _comboWindowEnd;
        public float HitWindowStart => _hitWindowStart;
        public float HitWindowEnd => _hitWindowEnd;
        public HitStrength HitStrength => _hitStrength;
        public BakedRootMotion Movement => _movement;
        public BakedRootMotion ReturnMovement => _returnMovement;
        public AttackAction Next => _next;

        /// <summary>공격 구간의 길이(초).</summary>
        public float Duration => _clip.length;

        /// <summary>복귀 구간의 길이(초).</summary>
        public float ReturnDuration => _returnClip.length;

        /// <summary><paramref name="elapsed"/>가 연계 구간 안인지 판정한다. 구간의 양 끝을 포함한다.</summary>
        public bool IsInComboWindow(float elapsed)
        {
            return elapsed >= _comboWindowStart && elapsed <= _comboWindowEnd;
        }

        /// <summary><paramref name="elapsed"/>가 무기 판정 구간 안인지 판정한다. 시작 시각은 포함하고 끝 시각은 포함하지 않는다.</summary>
        public bool IsInHitWindow(float elapsed)
        {
            return elapsed >= _hitWindowStart && elapsed < _hitWindowEnd;
        }

        /// <summary><paramref name="elapsed"/>가 후딜레이 캔슬 구간 안인지 판정한다. 시작 시각을 포함한다.</summary>
        public bool IsInRecovery(float elapsed)
        {
            return elapsed >= _recoveryStart;
        }
    }
}
