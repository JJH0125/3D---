using UnityEngine;

namespace Squad
{
    /// <summary>
    /// 차원을 초월하지 못하고, 넘나들지도 못하는, 차원 정보가 고정되어 있는 적에게
    /// 붙이는 딱지같은 개념. DimensionController가 이 딱지를 보고, 플레이어와 같은 차원에 있는 적만
    /// MeshRenderer를 켜고, 다른 차원에 있는 적은 MeshRenderer를 꺼서 보이지 않게 한다.
    /// </summary>
    public class DimensionMember : MonoBehaviour
    {
        [Header("■ 필수 연결 — 비워두면 에러")]
        [Tooltip("적의 MeshRenderer")]
        [SerializeField] private MeshRenderer meshRenderer;

        [Header("○ 튜닝 값 — 자유롭게 조절")]
        [Tooltip("적이 속한 차원")]
        [SerializeField] private Dimension dimension;

        private DimensionController dimensionController;

        public MeshRenderer Renderer => meshRenderer;
        public Dimension Dimension => dimension;

        void Start()
        {
            dimensionController = DimensionController.Instance;
            if (dimensionController != null)
                dimensionController.enemies.Add(this);
        }

        public void ToggleRenderer()
        {
            if (meshRenderer != null)
            {
                if (meshRenderer.enabled == true)
                    meshRenderer.enabled = false;
                else
                    meshRenderer.enabled = true;
            }
        }
    }
}