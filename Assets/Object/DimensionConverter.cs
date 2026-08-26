using UnityEngine;

namespace Squad
{
    public class DimensionConverter : MonoBehaviour
    {
        [Header("○ 튜닝 값 — 자유롭게 조절")]
        [Tooltip("상호작용할 수 있는 플레이어 대상 레이어")]
        [SerializeField] private LayerMask playerLayer;
        [Tooltip("범위 안에서 화면에 띄울 안내 문구")]
        [SerializeField] private string promptMessage = "[E] 차원 이동";
        [Tooltip("작동시키는 키")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        
    }
}