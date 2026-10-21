using UnityEngine;

namespace GodOfWar.Characters
{
    /// <summary>
    /// 클립 하나의 루트 궤적을 직선 변위로 구워 둔 데이터.
    /// 에디터의 추출 도구가 원본 클립에서 값을 채우며, 이후 인스펙터에서 튜닝할 수 있다.
    /// </summary>
    public sealed class BakedRootMotion : ScriptableObject
    {
        [Tooltip("값을 구워 낸 원본 클립(_RootMotion). 다시 굽기에만 쓴다.")]
        [SerializeField] private AnimationClip _sourceClip;

        [Tooltip("총 이동 거리(m).")]
        [SerializeField, Min(0f)] private float _distance;

        [Tooltip("이동 지속 시간(초).")]
        [SerializeField, Min(0.01f)] private float _duration = 0.01f;

        [Tooltip("정규화 시간(0~1) → 정규화 변위. (0, 0)에서 시작해 (1, 1)에서 끝나야 총 이동 거리가 Distance와 같다.")]
        [SerializeField] private AnimationCurve _curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public AnimationClip SourceClip => _sourceClip;
        public float Distance => _distance;
        public float Duration => _duration;
        public AnimationCurve Curve => _curve;
    }
}
