using UnityEngine;

namespace Squad
{
    public class ResultUI : MonoBehaviour
    {
        // 다음 라운드 버튼에 연결
        public void OnClickNextRound()
        {
            // 라운드를 올리고 새 Stage를 생성한다.
            // (timeScale은 GameManager가 Playing으로 바꿀 때 1로 되돌린다)
            GameManager.Instance.NextRound();
        }
    }
}
