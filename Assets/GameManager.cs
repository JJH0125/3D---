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
    private readonly HashSet<GameObject> generators = new();
    
    void Awake()
    {
        // 기존에 있던 GameManager Instance 제거
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

    private void ChangeState(GameState state)
    {
        CurrentState = state;
        Debug.Log("게임 상태가 " + CurrentState + "로 변경되었습니다.");
    }

    /// 게임이 시작되면, 발전기들이 자신을 GameManager에 등록한다.
    public void AddGenerator(GameObject generator)
    {
        generators.Add(generator);
    }
}