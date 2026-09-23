using UnityEngine;
using UnityEngine.SceneManagement;

namespace Squad
{
    public class GameOverUI : MonoBehaviour
    {
        // 다시 하기 버튼에 연결
        public void OnClickRetry()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}