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
        private Player player;
        private List<DimensionMember> enemies;
        private Dimension dimension;

        void Awake()
        {
            player = FindObjectOfType<Player>();
            enemies = new List<DimensionMember>();
            enemies.AddRange(FindObjectsOfType<DimensionMember>());
        }

        public void SwitchPlayerDimension()
        {
            dimension = player.SwitchMyDimension();
            
            foreach (var enemy in enemies)
            {
                if (enemy.Dimension == dimension)
                    enemy.Renderer.enabled = true;
                else
                    enemy.Renderer.enabled = false;
            }
        }
    }    
}