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

    /// <summary>
    /// 게임 전체의 진행 상태를 관리한다.
    ///
    /// 담당하는 것:
    ///   - 게임 상태(타이틀/플레이/일시정지/게임오버/결과) 전환
    ///   - 발전기 등록 및 작동 수 집계
    ///   - 클리어 조건 판정과 탈출구 활성화
    ///
    /// UI는 상태에 맞춰 켜고 끄기만 하며, 내용 갱신은 각 UI 클래스에 맡긴다.
    ///
    /// 화면 규칙:
    ///   Playing        게임 화면만
    ///   Pause          게임 화면 위에 일시정지 화면을 겹침
    ///   그 외          게임 화면을 끄고 해당 화면만
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("■ 필수 연결 — 비워두면 에러")]
        [Tooltip("타이틀 화면")]
        [SerializeField] private GameObject titleUI;
        [Tooltip("게임 플레이 중 표시되는 UI 묶음")]
        [SerializeField] private GameObject gameUI;
        [Tooltip("일시정지 화면")]
        [SerializeField] private GameObject pauseUI;
        [Tooltip("게임 오버 화면")]
        [SerializeField] private GameObject gameOverUI;
        [Tooltip("라운드 클리어 화면")]
        [SerializeField] private GameObject resultUI;

        [Tooltip("진행 상황 표시 (라운드, 발전기)")]
        [SerializeField] private StatusPrompt statusPrompt;
        [Tooltip("탈출구")]
        [SerializeField] private Exit exit;

        [Header("○ 튜닝 값 — 자유롭게 조절")]
        [Tooltip("전체 라운드 수")]
        [SerializeField] private int totalRounds = 5;

        /// 현재 게임 상태
        public GameState CurrentState { get; private set; }

        /// 현재 라운드 (1부터 시작)
        public int CurrentRound { get; private set; } = 1;

        /// 켜진 발전기의 수
        public int ActivatedGeneratorCount { get; private set; }

        /// 클리어 조건을 검사하기 위한 발전기 집합.
        /// 각 발전기가 시작할 때 스스로 등록한다.
        private readonly HashSet<Generator> generators = new();

        private void Awake()
        {
            // 기존에 있던 GameManager Instance 제거
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // 발전기들의 등록(Start)이 끝난 뒤에 화면을 구성해야 하므로
            // Awake가 아니라 Start에서 초기 상태를 설정한다.
            ChangeState(GameState.Title);
            RefreshStatus();
        }

        // ── 상태 전환 ────────────────────────────────────────────────

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

        /// <summary>탈출구에 도달하면 호출</summary>
        public void RoundClear()
        {
            // 같은 프레임에 여러 번 호출되어도 한 번만 처리한다.
            if (CurrentState != GameState.Playing) return;

            ChangeState(GameState.Result);
        }

        /// <summary>적에게 잡히면 호출</summary>
        public void GameOver()
        {
            if (CurrentState != GameState.Playing) return;

            ChangeState(GameState.GameOver);
        }

        private void ChangeState(GameState newState)
        {
            CurrentState = newState;

            switch (CurrentState)
            {
                case GameState.Title:
                    ShowOnly(titleUI);
                    Time.timeScale = 0f;
                    break;

                case GameState.Playing:
                    ShowOnly(gameUI);
                    Time.timeScale = 1f;
                    break;

                // 일시정지만 게임 화면 위에 겹친다.
                case GameState.Pause:
                    SetActiveSafe(pauseUI, true);
                    Time.timeScale = 0f;
                    break;

                case GameState.GameOver:
                    ShowOnly(gameOverUI);
                    Time.timeScale = 0f;
                    break;

                case GameState.Result:
                    ShowOnly(resultUI);
                    Time.timeScale = 0f;
                    break;
            }

            Debug.Log("게임 상태가 " + CurrentState + "로 변경되었습니다.");
        }

        /// <summary>
        /// 모든 화면을 끄고 지정한 화면만 켠다.
        /// 각 상태마다 모든 UI를 일일이 지정하면 빠뜨리기 쉬우므로,
        /// "전부 끄고 하나만 켠다"로 단순화한다.
        /// </summary>
        private void ShowOnly(GameObject target)
        {
            SetActiveSafe(titleUI, false);
            SetActiveSafe(gameUI, false);
            SetActiveSafe(pauseUI, false);
            SetActiveSafe(gameOverUI, false);
            SetActiveSafe(resultUI, false);

            SetActiveSafe(target, true);
        }

        // 연결이 비어 있어도 예외 없이 넘어가도록 감싼다.
        private static void SetActiveSafe(GameObject target, bool value)
        {
            if (target != null)
                target.SetActive(value);
        }

        // ── 발전기 ───────────────────────────────────────────────────

        /// <summary>발전기가 시작할 때 스스로 등록한다.</summary>
        public void AddGenerator(Generator generator)
        {
            if (generator != null)
                generators.Add(generator);
        }

        /// <summary>
        /// 발전기가 켜질 때 호출한다.
        /// 켜진 수를 다시 세어 표시를 갱신하고, 전부 켜졌으면 탈출구를 연다.
        /// </summary>
        public void CheckExit()
        {
            RefreshStatus();

            if (generators.Count > 0 && ActivatedGeneratorCount >= generators.Count)
            {
                if (exit != null)
                    exit.Activate();
            }
        }

        /// <summary>
        /// 켜진 발전기 수를 다시 세고 화면 표시를 갱신한다.
        /// 호출할 때마다 0부터 세므로 여러 번 불러도 값이 누적되지 않는다.
        /// </summary>
        private void RefreshStatus()
        {
            int count = 0;
            foreach (Generator generator in generators)
                if (generator != null && generator.IsActive)
                    count++;

            ActivatedGeneratorCount = count;

            if (statusPrompt != null)
            {
                statusPrompt.SetGenerators(count, generators.Count);
                statusPrompt.SetRound(CurrentRound, totalRounds);
            }
        }
    }
}