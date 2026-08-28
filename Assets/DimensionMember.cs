using UnityEngine;

namespace Squad
{
    /// <summary>
    /// 적들의 DimensionMember를 모아 MeshRenderer를 관리하는 클래스.
    /// 플레이어와 다른 차원에 있는 적의 Renderer를 비활성화
    /// </summary>
    public class DimensionMember : MonoBehaviour
    {
        [Header("■ 필수 연결 — 비워두면 에러")]
        [Tooltip("적의 MeshRenderer")]
        [SerializeField] private MeshRenderer meshRenderer;

        [Header("○ 튜닝 값 — 자유롭게 조절")]
        [Tooltip("적이 속한 차원")]
        [SerializeField] private Dimension dimension;

        public MeshRenderer Renderer => meshRenderer;
        public Dimension Dimension => dimension;
    }
}