using UnityEngine;

namespace GodOfWar.Characters
{
    /// <summary>
    /// 클립 하나의 루트 수평 궤적을 축별 커브로 구워 둔 데이터.
    /// 값은 클립 시작 시점의 캐릭터 정면 기준 로컬 변위(m)이고, 키 시간은 초다.
    /// 에디터의 추출 도구가 원본 클립에서 값을 채우며, 이후 인스펙터에서 튜닝할 수 있다.
    /// </summary>
    public sealed class BakedRootMotion : ScriptableObject
    {
        [Tooltip("값을 구워 낸 원본 클립(_RootMotion). 다시 굽기에만 쓴다.")]
        [SerializeField] private AnimationClip _sourceClip;

        [Tooltip("이동 지속 시간(초).")]
        [SerializeField, Min(0.01f)] private float _duration = 0.01f;

        [Tooltip("시간(초) → 로컬 옆(+X: 오른쪽) 변위(m). 0에서 시작한다.")]
        [SerializeField] private AnimationCurve _x = AnimationCurve.Constant(0f, 0.01f, 0f);

        [Tooltip("시간(초) → 로컬 앞(+Z) 변위(m). 0에서 시작한다.")]
        [SerializeField] private AnimationCurve _z = AnimationCurve.Constant(0f, 0.01f, 0f);

        public AnimationClip SourceClip => _sourceClip;
        public float Duration => _duration;

        /// <summary><paramref name="time"/>(초) 시점의 시작점 기준 로컬 변위(m). 수직 성분은 없다.</summary>
        public Vector3 Sample(float time)
        {
            return new Vector3(_x.Evaluate(time), 0f, _z.Evaluate(time));
        }
    }
}
