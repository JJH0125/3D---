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
        public Dimension Dimension => dimension;

        void Start()
        {
            // 컨트롤러는 Start에서 한번만 참고하고, 이후에는 몰라도 된다.
            DimensionController dimensionController = DimensionController.Instance;
            if (dimensionController != null)
                dimensionController.AddEnemy(this);
        }

        /// <summary>
        /// 적은 컨트롤러가 명령하는 대로 보이고 숨는다.
        /// </summary>
        public void SetRenderer(bool isVisible)
        {
            if (meshRenderer != null)
            {
                if (isVisible)
                    meshRenderer.enabled = true;
                else
                    meshRenderer.enabled = false;
            }
        }
    }
}