using UnityEngine;
using System.Collections.Generic;

/// Real 차원, Fake 차원이 존재하며
/// 발전기가 소리를 전달할 때 쓸 None 값이 따로 존재한다
public enum Dimension { None, Real, Fake }

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

        /// <summary>
        /// 적들이 자기 자신을 컨트롤러에 등록하도록 하는 함수
        /// </summary>
        /// <param name="enemy"></param>
        public void AddEnemy(DimensionMember enemy)
        {
            if (enemy != null)
            {
                enemies.Add(enemy);
                enemy.SetRenderer(CompareDimension(enemy));
            }
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

            string layerName = player.myDimension == Dimension.Real ? "RealPlayer" : "FakePlayer";
            SetLayerRecursively(player.gameObject, LayerMask.NameToLayer(layerName));

            foreach (var enemy in enemies)
                enemy.SetRenderer(CompareDimension(enemy));
        }

        /// <summary>
        /// 오브젝트와 그 모든 자식의 레이어를 재귀적으로 변경
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;

            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}