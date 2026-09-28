using UnityEngine;
using UnityEngine.SceneManagement;

namespace Squad
{
    public class PauseUI : MonoBehaviour
    {
        // 게임으로 돌아가기 버튼에 연결
        public void OnClickResume()
        {
            GameManager.Instance.ResumeGame();
        }

        // 재시작 또는 메인화면 버튼에 연결
        public void OnClickRestart()
        {
            // GameManager가 timeScale을 0으로 만들었으므로 씬 전환 전 원상복구
            Time.timeScale = 1f; 
            
            // 현재 씬을 다시 로드하여 초기화 (씬 이름이나 인덱스에 맞게 수정)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}