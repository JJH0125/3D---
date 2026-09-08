using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private readonly HashSet<GameObject> generators = new();
    
    /// 발전기 집합에 발전기를 추가한다.
    public void AddGenerator(GameObject generator)
    {
        generators.Add(generator);
    }
}