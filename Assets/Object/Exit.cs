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
        [SerializeField] private string promptMessage = "모든 발전기를 켜세요! (?/?)";

        private bool isActive;
        private bool _playerInRange;

        void Start()
        {
            isActive = false;
        }

        void Update()
        {
            if (_playerInRange && isActive)
                manager.RoundClear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsInLayerMask(other.gameObject.layer, playerLayer))
                return;
            
            _playerInRange = true;
            if (!isActive)
                ShowPrompt();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsInLayerMask(other.gameObject.layer, playerLayer))
                return;
            
            _playerInRange = false;
            if (!isActive)
                HidePrompt();
        }

        // LayerMask는 비트로 레이어를 표시한다. 해당 레이어 비트가 켜져 있는지 확인.
        private static bool IsInLayerMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        /// 발전기가 모두 켜지면 GameManager가 호출
        public void Activate()
        {
            isActive = true;
        }

        private void ShowPrompt()
        {            
            if (InteractionPrompt.Instance != null)
                InteractionPrompt.Instance.Show(this, promptMessage);
        }

        private void HidePrompt()
        {
            if (InteractionPrompt.Instance != null)
                InteractionPrompt.Instance.Hide(this);
        }
    }
}