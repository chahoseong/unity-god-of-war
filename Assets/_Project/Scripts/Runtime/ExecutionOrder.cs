namespace GodOfWar
{
    /// <summary>
    /// 프로젝트 MonoBehaviour들의 실행 순서. 각 스크립트는 <c>[DefaultExecutionOrder]</c>에 이 값을 쓴다.
    /// 값이 같은 스크립트끼리는 순서가 보장되지 않으므로 겹치지 않게 둔다.
    /// 외부 패키지의 순서도 피해야 한다. CinemachineBrain은 100이다.
    /// </summary>
    public static class ExecutionOrder
    {
        /// <summary>소비자가 같은 프레임의 입력을 보도록 다른 게임플레이 컴포넌트보다 먼저 갱신한다.</summary>
        public const int PlayerInputReader = -100;

        /// <summary>대상이 사라진 프레임에 카메라가 이미 해제된 결과를 읽도록 CameraLook보다 먼저 실행한다.</summary>
        public const int TargetLock = -60;

        /// <summary>이동이 같은 프레임의 Yaw를 읽도록 PlayerController보다 먼저 실행한다.</summary>
        public const int CameraLook = -50;

        /// <summary>상태 머신과 액션 라우터를 갱신한다.</summary>
        public const int PlayerController = 0;

        /// <summary>상태가 이번 프레임에 시작한 크로스페이드를 같은 프레임에 읽도록 PlayerController 뒤에 실행한다.</summary>
        public const int PlayerAnimator = 50;

        /// <summary>표식이 대상보다 한 프레임 뒤처지지 않도록 CinemachineBrain이 카메라를 옮긴 뒤 계산한다.</summary>
        public const int LockOnIndicator = 200;
    }
}
