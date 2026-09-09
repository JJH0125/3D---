using UnityEngine;

namespace Squad
{
    public class Exit : MonoBehaviour
    {
        [Header("■ 필수 연결 — 비워두면 에러")]
        [Tooltip("게임매니저")]
        [SerializeField] private GameManager manager;

        [Header("○ 튜닝 값 — 자유롭게 조절")]
        [Tooltip("상호작용할 수 있는 플레이어 대상 레이어")]
        [SerializeField] private LayerMask playerLayer;
        [Tooltip("범위 안에서 화면에 띄울 안내 문구")]
        [SerializeField] private string promptMessage = "[E] 탈출";
        [Tooltip("작동시키는 키")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private bool isActive;
        private bool _playerInRange;

        void Start()
        {
            isActive = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsInLayerMask(other.gameObject.layer, playerLayer))
                return;
            
            _playerInRange = true;
            ShowPrompt();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsInLayerMask(other.gameObject.layer, playerLayer))
                return;
            
            _playerInRange = false;
            HidePrompt();
        }

        // LayerMask는 비트로 레이어를 표시한다. 해당 레이어 비트가 켜져 있는지 확인.
        private static bool IsInLayerMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        private void Activate()
        {
            isActive = true;
        }
    }
}