using UnityEngine;
using System.Collections.Generic;

namespace Squad
{
    public enum GameState
    {
        Title,
        Playing,
        Pause,
        GameOver,
        Result
    }

    public class GameManager : MonoBehaviour
    {
        [Header("■ 필수 연결 — 비워두면 에러")]
        [Tooltip("상태 프롬프")]
        [SerializeField] private StatusPrompt1 status;
        public static GameManager Instance { get; private set; }
        public GameState CurrentState { get; private set; }

        /// 클리어 조건이 만족되었는지 검사하기 위한 발전기 집합
        private readonly HashSet<Generator> generators = new();
        /// 켜진 발전기의 수
        private int numberOfActivatedGenerator;
        /// 검사를 토대로 활성화할 출구
        private Exit _exit;
        
        void Awake()
        {
            /// 기존에 있던 GameManager Instance 제거
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CurrentState = GameState.Title;
        }

        void Update()
        {

        }

        public void StartGame()
        {
            ChangeState(GameState.Playing);
        }

        public void PauseGame()
        {
            ChangeState(GameState.Pause);
        }
        
        public void ResumeGame()
        {
            ChangeState(GameState.Playing);
        }

        /// 탈출구에 도달하면 호출
        public void RoundClear()
        {
            ChangeState(GameState.Result);
            /// 탈출 성공을 알리고, 다음 라운드 시작를 알리는 UI를 띄우도록
        }

        /// 적에게 잡히면 호출
        public void GameOver()
        {
            ChangeState(GameState.GameOver);
            /// 게임오버되었음을 알리고, 현재 라운드 재시작을 알리는 UI를 띄우도록
        }

        private void ChangeState(GameState newState)
        {
            CurrentState = newState;

            switch (CurrentState)
            {
            case GameState.Title:
                // titleUI.SetActive(true);
                //gameUI.SetActive(false);
                //pauseUI.SetActive(false);
                Time.timeScale = 0f;
                break;

            case GameState.Playing:
                // titleUI.SetActive(false);
                // gameUI.SetActive(true);
                // pauseUI.SetActive(false);
                Time.timeScale = 1f;
                break;

            case GameState.Pause:
                // pauseUI.SetActive(true);
                Time.timeScale = 0f;
                break;

            case GameState.GameOver:
                // gameOverUI.SetActive(true);
                Time.timeScale = 0f;
                break;
            case GameState.Result:
                // resultUI.SetActive(true);
                Time.timeScale = 0f;
                break;
            }

            Debug.Log("게임 상태가 " + CurrentState + "로 변경되었습니다.");
        }

        /// 게임이 시작되면, 발전기들과 출구가 자신을 manager에 등록한다.
        public void AddGenerator(Generator generator) => generators.Add(generator);
        public void AddExit(Exit exit) => _exit = exit;

        /// 현재 켜진 발전기의 수를 세고,
        /// 모두 켜졌다면 출구를 활성화한다
        public void CheckExit()
        {
            int count = 0;

            foreach (Generator generator in generators)
                if (generator.IsActive)
                    count++;

            numberOfActivatedGenerator = count;
            status.SetGenerators(count, generators.Count);

            if (numberOfActivatedGenerator == generators.Count)
                _exit.Activate();
        }
    }
}
