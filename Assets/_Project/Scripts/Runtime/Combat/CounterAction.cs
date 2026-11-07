using GodOfWar.Characters;
using GodOfWar.Effects;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 패링 카운터 한 동작의 액션 데이터. 칼 판정으로 찾지 않고, 타격 시각에 패링한 상대에게 직접 타격을 전한다.
    /// 타격 시각까지 상대 쪽으로 돌며 다가가, 타격 시각에 닿는 거리가 되게 한다. 복귀 클립 없이 클립이 끝나면 이동으로 돌아간다.
    /// 닿는 거리와 다가가는 상한은 클립 모양에 따라 정해지므로 이 데이터에 둔다. 시각은 모두 카운터 시작부터 잰 초다.
    /// </summary>
    [CreateAssetMenu(menuName = "GodOfWar/Combat/Counter Action", fileName = "CounterAction")]
    public sealed class CounterAction : ScriptableObject
    {
        [Tooltip("카운터 동작을 재생할 애니메이터 상태 이름.")]
        [SerializeField] private string _stateName;

        [Tooltip("카운터 상태의 클립. 길이가 카운터 동작의 길이다.")]
        [SerializeField] private AnimationClip _clip;

        [Tooltip("카운터로 들어갈 때와 끝나고 이동으로 돌아갈 때의 크로스페이드 시간(초).")]
        [SerializeField, Min(0f)] private float _crossFadeDuration = 0.1f;

        [Tooltip("카운터 동안의 몸 이동. 클립의 루트 모션 판에서 굽는다.")]
        [SerializeField] private BakedRootMotion _movement;

        [Tooltip("상대에게 타격을 전하는 시각. 이 시각까지 상대 쪽으로 돌며 다가간다.")]
        [SerializeField, Min(0f)] private float _strikeTime;

        [Tooltip("타격 시각에 닿아 보이는 몸 중심 사이 수평 거리(m). 카운터 시작 위치 기준이며, 클립의 몸 이동을 포함한다.")]
        [SerializeField, Min(0f)] private float _contactDistance;

        [Tooltip("닿는 거리보다 멀 때 클립 이동에 더해 다가가는 최대 거리(m). 더 멀면 닿지 않은 채 친다.")]
        [SerializeField, Min(0f)] private float _maxApproach;

        [Tooltip("후속 입력을 받기 시작하는 시각.")]
        [SerializeField, Min(0f)] private float _comboWindowStart;

        [Tooltip("후속 입력을 받는 마지막 시각. 이 뒤로는 이동 입력이 있으면 클립 끝을 기다리지 않고 이동으로 돌아간다.")]
        [SerializeField, Min(0f)] private float _comboWindowEnd;

        [Tooltip("회피 등 다른 액션으로 캔슬할 수 있게 되는 시각.")]
        [SerializeField, Min(0f)] private float _recoveryStart;

        [Tooltip("상대에게 전하는 타격 세기.")]
        [SerializeField] private HitStrength _hitStrength = HitStrength.Heavy;

        [Tooltip("상대에게 전하는 피해량. 실제로 체력을 얼마나 깎을지는 피격자가 정한다.")]
        [SerializeField, Min(0)] private int _damage;

        [Tooltip("검 궤적을 그리기 시작하는 시각. 칼이 빨라지는 스윙 시작에 맞춘다.")]
        [SerializeField, Min(0f)] private float _trailStart;

        [Tooltip("검 궤적을 그만 그리는 시각. 이미 그린 궤적은 외형의 수명 동안 흐려지며 남는다.")]
        [SerializeField, Min(0f)] private float _trailEnd;

        [Tooltip("검 궤적의 외형. 비우면 카운터는 궤적을 그리지 않는다.")]
        [SerializeField] private WeaponTrailProfile _trailProfile;

        [Tooltip("칼을 휘두르는 소리. 클립을 비우면 소리 없이 휘두른다.")]
        [SerializeField] private SoundCue _swingSound = new SoundCue();

        [Tooltip("휘두르는 소리를 내는 시각. 칼이 빨라지는 스윙 시작에 맞춘다. 이 시각 전에 캔슬되면 소리를 내지 않는다.")]
        [SerializeField, Min(0f)] private float _swingSoundTime;

        public string StateName => _stateName;
        public AnimationClip Clip => _clip;
        public float CrossFadeDuration => _crossFadeDuration;
        public BakedRootMotion Movement => _movement;
        public float StrikeTime => _strikeTime;
        public float ContactDistance => _contactDistance;
        public float MaxApproach => _maxApproach;
        public float ComboWindowStart => _comboWindowStart;
        public float ComboWindowEnd => _comboWindowEnd;
        public float RecoveryStart => _recoveryStart;
        public HitStrength HitStrength => _hitStrength;
        public int Damage => _damage;

        /// <summary>카운터 동작의 길이(초).</summary>
        public float Duration => _clip.length;

        /// <summary>카운터 동안 무기가 궤적을 켜고 끄고 휘두르는 소리를 낼 시각. 타격은 칼날 판정 없이 직접 전하므로 판정 구간은 없다.</summary>
        public SwingTiming SwingTiming =>
            SwingTiming.WithoutHit(_trailStart, _trailEnd, _trailProfile, _swingSound, _swingSoundTime);

        /// <summary><paramref name="elapsed"/>가 연계 구간 안인지 판정한다. 구간의 양 끝을 포함한다.</summary>
        public bool IsInComboWindow(float elapsed)
        {
            return elapsed >= _comboWindowStart && elapsed <= _comboWindowEnd;
        }

        /// <summary><paramref name="elapsed"/>가 후딜레이 캔슬 구간 안인지 판정한다. 시작 시각을 포함한다.</summary>
        public bool IsInRecovery(float elapsed)
        {
            return elapsed >= _recoveryStart;
        }
    }
}
