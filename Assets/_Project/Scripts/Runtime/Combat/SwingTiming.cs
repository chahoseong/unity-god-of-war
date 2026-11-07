using GodOfWar.Effects;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 스윙 한 번 동안 무기가 언제 무엇을 켜고 내는지. 시각은 모두 스윙을 시작한 동작의 시작부터 잰 초다.
    /// 액션 데이터가 조립해 돌려주고, 공격하는 상태가 경과 시간과 함께 <see cref="Weapon.UpdateSwing"/>에 넘긴다.
    /// 구간은 시작 시각을 포함하고 끝 시각은 포함하지 않는다. 기본값은 판정도 궤적도 켜지 않고 소리도 내지 않는 스윙이다.
    /// </summary>
    public readonly struct SwingTiming
    {
        private readonly float _hitStart;
        private readonly float _hitEnd;
        private readonly float _trailStart;
        private readonly float _trailEnd;
        private readonly float _swingSoundTime;

        public SwingTiming(
            float hitStart, float hitEnd, HitStrength hitStrength, bool knockdown, int damage,
            float trailStart, float trailEnd, WeaponTrailProfile trailProfile,
            SoundCue swingSound = null, float swingSoundTime = 0f)
        {
            _hitStart = hitStart;
            _hitEnd = hitEnd;
            HitStrength = hitStrength;
            Knockdown = knockdown;
            Damage = damage;
            _trailStart = trailStart;
            _trailEnd = trailEnd;
            TrailProfile = trailProfile;
            SwingSound = swingSound;
            _swingSoundTime = swingSoundTime;
        }

        /// <summary>판정 구간 동안 맞은 대상에게 전하는 타격 세기.</summary>
        public HitStrength HitStrength { get; }

        /// <summary>판정 구간 동안 맞은 대상을 넉다운시키는지.</summary>
        public bool Knockdown { get; }

        /// <summary>판정 구간 동안 맞은 대상에게 전하는 피해량.</summary>
        public int Damage { get; }

        /// <summary>궤적 구간 동안 그릴 궤적의 외형. 비어 있으면 궤적을 그리지 않는다.</summary>
        public WeaponTrailProfile TrailProfile { get; }

        /// <summary>칼을 휘두르는 소리. 비어 있으면 소리를 내지 않는다.</summary>
        public SoundCue SwingSound { get; }

        /// <summary>
        /// 칼날 판정 없이 궤적과 소리만 켜는 스윙. 패링 카운터처럼 타격을 칼날로 찾지 않고 직접 전할 때 쓴다.
        /// </summary>
        public static SwingTiming WithoutHit(
            float trailStart, float trailEnd, WeaponTrailProfile trailProfile,
            SoundCue swingSound = null, float swingSoundTime = 0f)
        {
            return new SwingTiming(0f, 0f, default, false, 0, trailStart, trailEnd, trailProfile, swingSound, swingSoundTime);
        }

        /// <summary><paramref name="elapsed"/>가 판정 구간 안인지 판정한다.</summary>
        public bool IsInHitWindow(float elapsed)
        {
            return elapsed >= _hitStart && elapsed < _hitEnd;
        }

        /// <summary><paramref name="elapsed"/>가 궤적 구간 안인지 판정한다. 외형이 비어 있으면 항상 아니다.</summary>
        public bool IsInTrailWindow(float elapsed)
        {
            return TrailProfile != null && elapsed >= _trailStart && elapsed < _trailEnd;
        }

        /// <summary>
        /// <paramref name="elapsed"/>에 휘두르는 소리를 낼 때가 됐는지 판정한다. 시각을 지난 뒤로는 계속 참이므로,
        /// 스윙마다 한 번만 내는 것은 무기가 맡는다. 소리가 비어 있으면 항상 아니다.
        /// </summary>
        public bool IsPastSwingSoundTime(float elapsed)
        {
            return SwingSound != null && elapsed >= _swingSoundTime;
        }
    }
}
