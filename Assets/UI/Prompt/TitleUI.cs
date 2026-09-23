using UnityEngine;

namespace Squad
{
    public class TitleUI : MonoBehaviour
    {
        // 시작 버튼의 OnClick 이벤트에 연결
        public void OnClickStartGame()
        {
            // GameManager의 상태를 Playing으로 변경
            GameManager.Instance.StartGame();
        }

        // 종료 버튼의 OnClick 이벤트에 연결
        public void OnClickQuitGame()
        {
            Debug.Log("게임을 종료합니다.");
            Application.Quit();
        }
    }
}