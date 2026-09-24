using UnityEngine;

namespace Squad
{
    public class GameUI : MonoBehaviour
    {
        private void Update()
        {
            // 플레이 중에만 이 UI가 켜져 있으므로, 여기서 입력을 받으면 안전합니다.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GameManager.Instance.PauseGame();
            }
        }
    }
}