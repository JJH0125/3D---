using UnityEngine;
using System.Collections.Generic;

public enum Dimension { Real, Fake }

namespace Squad
{
    /// <summary>
    /// 적들의 DimensionMember를 모아 MeshRenderer를 관리하는 클래스.
    /// 플레이어와 다른 차원에 있는 적의 Renderer를 비활성화
    /// </summary>
    public class DimensionController : MonoBehaviour
    {
        public Player player { get; private set; }
        private HashSet<DimensionMember> enemies;

        public static DimensionController Instance { get; private set; }

        void Awake()
        {
            // 기존에 있던 DimensionController Instance 제거
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            player = FindObjectOfType<Player>();
            enemies = new HashSet<DimensionMember>();
        }

        public void AddEnemy(DimensionMember enemy)
        {
            if (enemy != null)
                enemies.Add(enemy);
        }

        public bool CompareDimension(DimensionMember enemy)
        {
            if (player == null || enemy == null)
                return false;

            return player.myDimension == enemy.Dimension;
        }

        public void SwitchPlayerDimension()
        {
            player.SwitchMyDimension();
            
            foreach (var enemy in enemies)
                enemy.SetRenderer();
        }
    }    
}