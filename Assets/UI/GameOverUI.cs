using UnityEngine;

namespace Squad
{
    public class GameOverUI : MonoBehaviour
    {
        // 이번 라운드 재시도 버튼에 연결
        public void OnClickRetry()
        {
            // 씬을 다시 로드하면 라운드가 1로 초기화되므로,
            // 시작 버튼처럼 현재 라운드의 Stage를 새로 생성한다.
            // (timeScale은 GameManager가 Playing으로 바꿀 때 1로 되돌린다)
            GameManager.Instance.StartGame();
        }
    }
}
