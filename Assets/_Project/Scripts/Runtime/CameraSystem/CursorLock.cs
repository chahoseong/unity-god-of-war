using UnityEngine;

namespace GodOfWar.CameraSystem
{
    /// <summary>활성화되어 있는 동안 커서를 화면 중앙에 잠그고 숨긴다.</summary>
    [DisallowMultipleComponent]
    public sealed class CursorLock : MonoBehaviour
    {
        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
