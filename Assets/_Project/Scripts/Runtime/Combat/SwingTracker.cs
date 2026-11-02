using System;
using UnityEngine;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 휘두르는 무기의 타격부 양 끝을 프레임 사이로 따라가는 도구. 지난번 포즈부터 지금 포즈까지를 정한 간격으로 나눠,
    /// 그 사이 각 시점의 양 끝 두 점을 내놓는다.
    /// 무기는 손잡이를 축으로 돌기 때문에 양 끝을 직선으로 보간하면 호의 안쪽을 지나간다.
    /// 그래서 무기 오브젝트의 포즈(위치·회전)를 보간하고, 양 끝은 그 포즈에서 다시 계산한다.
    /// 시작 시점과 간격은 쓰는 쪽마다 다르므로, 쓰는 쪽이 하나씩 만들어 각자 기록한다.
    /// </summary>
    public sealed class SwingTracker
    {
        private readonly Transform _pivot;
        private readonly Transform _base;
        private readonly Transform _tip;

        // 양 끝의 축 기준 오프셋(회전만 적용, 배율 미적용). 포즈를 보간해 양 끝을 다시 계산한다.
        private Vector3 _baseOffset;
        private Vector3 _tipOffset;
        private Vector3 _previousPosition;
        private Quaternion _previousRotation;
        private Vector3 _currentPosition;
        private Quaternion _currentRotation;

        /// <param name="pivot">포즈를 보간할 무기 오브젝트. 양 끝은 이 오브젝트나 그 자식이어야 한다.</param>
        /// <param name="base">타격부가 시작하는 지점(손잡이 쪽).</param>
        /// <param name="tip">타격부 끝.</param>
        public SwingTracker(Transform pivot, Transform @base, Transform tip)
        {
            _pivot = pivot != null ? pivot : throw new ArgumentNullException(nameof(pivot));
            _base = @base != null ? @base : throw new ArgumentNullException(nameof(@base));
            _tip = tip != null ? tip : throw new ArgumentNullException(nameof(tip));
        }

        /// <summary>
        /// 지금 포즈에서 기록을 새로 시작한다. 양 끝 오프셋도 다시 잰다.
        /// 다음 <see cref="Advance"/> 전까지 <see cref="GetSegment"/>는 지금 양 끝을 돌려준다.
        /// </summary>
        public void Begin()
        {
            Quaternion inverse = Quaternion.Inverse(_pivot.rotation);
            _baseOffset = inverse * (_base.position - _pivot.position);
            _tipOffset = inverse * (_tip.position - _pivot.position);
            _currentPosition = _pivot.position;
            _currentRotation = _pivot.rotation;
            _previousPosition = _currentPosition;
            _previousRotation = _currentRotation;
        }

        /// <summary>
        /// 지난번 포즈부터 지금 포즈까지를 나눌 단계 수를 정하고, 지금 포즈를 다음 프레임의 출발점으로 삼는다.
        /// 양 끝 중 더 많이 움직인 쪽의 이동 거리가 <paramref name="spacing"/>을 넘지 않게 나누되,
        /// 1 이상 <paramref name="maxSteps"/> 이하로 자른다.
        /// </summary>
        public int Advance(float spacing, int maxSteps)
        {
            _previousPosition = _currentPosition;
            _previousRotation = _currentRotation;
            _currentPosition = _pivot.position;
            _currentRotation = _pivot.rotation;

            float baseTravel = Vector3.Distance(
                _previousPosition + _previousRotation * _baseOffset, _currentPosition + _currentRotation * _baseOffset);
            float tipTravel = Vector3.Distance(
                _previousPosition + _previousRotation * _tipOffset, _currentPosition + _currentRotation * _tipOffset);

            return Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(baseTravel, tipTravel) / spacing), 1, maxSteps);
        }

        /// <summary>지난번 포즈(0)부터 지금 포즈(1) 사이 <paramref name="t"/> 시점의 양 끝 두 점.</summary>
        public void GetSegment(float t, out Vector3 start, out Vector3 end)
        {
            Vector3 position = Vector3.Lerp(_previousPosition, _currentPosition, t);
            Quaternion rotation = Quaternion.Slerp(_previousRotation, _currentRotation, t);
            start = position + rotation * _baseOffset;
            end = position + rotation * _tipOffset;
        }
    }
}
