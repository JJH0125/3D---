using UnityEngine;
using System.Collections.Generic;

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
    public static GameManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }

    /// 클리어 조건이 만족되었는지 검사하기 위한 발전기 집합
    private readonly HashSet<Generator> generators = new();
    
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
    }

    /// 적에게 잡히면 호출
    public void GameOver()
    {
        ChangeState(GameState.GameOver);
    }

    private void ChangeState(GameState newState)
    {
        CurrentState = newState;

        switch (CurrentState)
        {
        case GameState.Title:
            titleUI.SetActive(true);
            gameUI.SetActive(false);
            pauseUI.SetActive(false);
            Time.timeScale = 0f;
            break;

        case GameState.Playing:
            titleUI.SetActive(false);
            gameUI.SetActive(true);
            pauseUI.SetActive(false);
            Time.timeScale = 1f;
            break;

        case GameState.Pause:
            pauseUI.SetActive(true);
            Time.timeScale = 0f;
            break;

        case GameState.GameOver:
            gameOverUI.SetActive(true);
            Time.timeScale = 0f;
            break;
        case GameState.Result:
            resultUI.SetActive(true);
            Time.timeScale = 0f;
            break;
        }

        Debug.Log("게임 상태가 " + CurrentState + "로 변경되었습니다.");
    }

<<<<<<< HEAD
    /// 게임이 시작되면, 발전기들이 자신을 GameManager에 등록한다.
    public void AddGenerator(Generator generator)
=======
    /// 게임이 시작되면, 발전기들은 생성되는 즉시 자신을 Manager에 등록한다.
    public void AddGenerator(GameObject generator)
>>>>>>> bdaab9cce701c9bc43fa7d6d6d694d3f3f63cf4a
    {
        generators.Add(generator);
    }
}