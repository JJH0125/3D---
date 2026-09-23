using UnityEngine;
using UnityEngine.SceneManagement;

namespace Squad
{
    public class ResultUI : MonoBehaviour
    {
        // 다음 라운드로 가기 버튼에 연결
        public void OnClickNextRound()
        {
            Time.timeScale = 1f;
            
            // 현재는 씬 재시작으로 구현. 
            // 나중에 라운드 데이터를 넘기거나 다음 씬을 로드하도록 수정할 수 있습니다.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}