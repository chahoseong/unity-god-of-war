namespace GodOfWar.Combat
{
    /// <summary>
    /// 스윙 한 번 동안 무기가 언제 무엇을 켜는지. 시각은 모두 스윙을 시작한 동작의 시작부터 잰 초다.
    /// 액션 데이터가 조립해 돌려주고, 공격하는 상태가 경과 시간과 함께 <see cref="Weapon.UpdateSwing"/>에 넘긴다.
    /// 구간은 시작 시각을 포함하고 끝 시각은 포함하지 않는다. 기본값은 판정도 궤적도 켜지 않는 스윙이다.
    /// </summary>
    public readonly struct SwingTiming
    {
        private readonly float _hitStart;
        private readonly float _hitEnd;
        private readonly float _trailStart;
        private readonly float _trailEnd;

        public SwingTiming(
            float hitStart, float hitEnd, HitStrength hitStrength, bool knockdown,
            float trailStart, float trailEnd, WeaponTrailProfile trailProfile)
        {
            _hitStart = hitStart;
            _hitEnd = hitEnd;
            HitStrength = hitStrength;
            Knockdown = knockdown;
            _trailStart = trailStart;
            _trailEnd = trailEnd;
            TrailProfile = trailProfile;
        }

        /// <summary>판정 구간 동안 맞은 대상에게 전하는 타격 세기.</summary>
        public HitStrength HitStrength { get; }

        /// <summary>판정 구간 동안 맞은 대상을 넉다운시키는지.</summary>
        public bool Knockdown { get; }

        /// <summary>궤적 구간 동안 그릴 궤적의 외형. 비어 있으면 궤적을 그리지 않는다.</summary>
        public WeaponTrailProfile TrailProfile { get; }

        /// <summary>
        /// 칼날 판정 없이 궤적만 켜는 스윙. 패링 카운터처럼 타격을 칼날로 찾지 않고 직접 전할 때 쓴다.
        /// </summary>
        public static SwingTiming WithoutHit(float trailStart, float trailEnd, WeaponTrailProfile trailProfile)
        {
            return new SwingTiming(0f, 0f, default, false, trailStart, trailEnd, trailProfile);
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
    }
}
