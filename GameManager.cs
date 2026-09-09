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
    private GameState CurrentState { get; private set; }
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

    public void GameOver()
    {
        ChangeState(GameState.GameOver);
    }

    public void 

    private void ChangeState(GameState state)
    {
        CurrentState = state;
        Debug.Log("게임 상태가 " + CurrentState + "로 변경되었습니다.");
    }

    /// 발전기 집합에 발전기를 추가한다.
    public void AddGenerator(GameObject generator)
    {
        generators.Add(generator);
    }

    /// 탈출구에 도달하면 호출
    public void RoundClear()
    {

    }
}