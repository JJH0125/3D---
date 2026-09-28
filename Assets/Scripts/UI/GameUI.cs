using UnityEngine;

namespace Squad
{
    public class GameUI : MonoBehaviour
    {
        private void Update()
        {
            // 일시정지 화면은 게임 화면 위에 겹쳐 뜨므로 이 UI는 일시정지 중에도 켜져 있다.
            // 따라서 ESC 입력을 여기 한 곳에서만 받아 일시정지/재개를 토글한다.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GameManager manager = GameManager.Instance;

                if (manager.CurrentState == GameState.Playing)
                    manager.PauseGame();
                else if (manager.CurrentState == GameState.Pause)
                    manager.ResumeGame();
            }
        }
    }
}
