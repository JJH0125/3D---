using UnityEngine;

namespace Squad
{
    /// <summary>
    /// 발전기. 플레이어가 범위에 들어오면 안내 문구를 띄우고, E 키로
    /// 작동시킬 수 있다. 작동 중에는 주기적으로 발전기 소리를 방출한다.
    ///
    /// 발전기 소리는 지속적이라, 켜져 있는 동안 emitInterval마다 계속
    /// SoundEmitter.Emit을 호출한다. (발소리 같은 일회성 소리와 달리
    /// "계속 나는" 소리이므로, 켜진 동안 반복해서 방출한다.)
    ///
    /// "발전기를 켜야 진행되지만, 켜면 소리로 노출된다"는 긴장이 핵심이다.
    ///
    /// 씬 구성:
    ///   - 이 오브젝트에 Collider를 하나 더 두고 Is Trigger를 켠다
    ///     (상호작용 범위. 발전기 본체 콜라이더와 별개로 크게 잡으면 좋다)
    ///   - 플레이어 오브젝트에 Rigidbody가 있어야 트리거가 감지된다
    ///   - playerLayer에 플레이어 레이어를 지정한다
    /// </summary>
    public class Generator : MonoBehaviour
    {
        [Header("○ 튜닝 값 — 자유롭게 조절")]
        [Tooltip("발전기 소리를 들을 적 대상 레이어")]
        [SerializeField] private LayerMask enemyLayer;
        [Tooltip("작동 중 소리 방출 간격(초)")]
        [SerializeField] private float emitInterval = 0.5f;
        [Tooltip("게임이 시작됐을 때 켜짐/꺼짐 여부")]
        [SerializeField] private bool startsActive = false;
        [Tooltip("상호작용할 수 있는 플레이어 대상 레이어")]
        [SerializeField] private LayerMask playerLayer;
        [Tooltip("범위 안에서 화면에 띄울 안내 문구")]
        [SerializeField] private string promptMessage = "[E] 발전기 작동";
        [Tooltip("작동시키는 키")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        // 작동 중인가. 외부(플레이어 상호작용)에서 켜고 끌 수 있다.
        public bool IsActive { get; private set; }

        private float _emitTimer;
        private bool _playerInRange;
        private bool _isInvestigated;

        private void Start()
        {
            IsActive = startsActive;
        }

        private void Update()
        {
            // 범위 안 + 아직 안 켜짐 + 키 입력 → 작동
            if (_playerInRange && !IsActive && Input.GetKeyDown(interactKey))
                Activate();

            if (!IsActive)
                return;

            _emitTimer -= Time.deltaTime;

            if (_emitTimer <= 0f)
            {
                SoundEmitter.Emit(transform.position, SoundList.Generator, enemyLayer);
                _emitTimer = emitInterval;
            }
        }

        // ── 상호작용 범위 ─────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!IsInLayerMask(other.gameObject.layer, playerLayer))
                return;

            _playerInRange = true;
            ShowPromptIfNeeded();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsInLayerMask(other.gameObject.layer, playerLayer))
                return;

            _playerInRange = false;
            HidePrompt();
        }

        // 이미 켜진 발전기에는 안내 문구를 띄우지 않는다.
        private void ShowPromptIfNeeded()
        {
            if (IsActive)
                return;
            
            if (InteractionPrompt.Instance != null)
                InteractionPrompt.Instance.Show(this, promptMessage);
        }

        private void HidePrompt()
        {
            if (InteractionPrompt.Instance != null)
                InteractionPrompt.Instance.Hide(this);
        }

        // LayerMask는 비트로 레이어를 표시한다. 해당 레이어 비트가 켜져 있는지 확인.
        private static bool IsInLayerMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        // ── 켜고 끄기 ────────────────────────────────────────────────

        /// <summary>발전기를 켠다(플레이어가 작동시킬 때 호출).</summary>
        public void Activate()
        {
            IsActive = true;
            _emitTimer = 0f;   // 켜자마자 첫 소리가 바로 나도록

            // 이미 켰으니 안내 문구는 내린다.
            HidePrompt();
        }

        /// <summary>발전기를 끈다.</summary>
        public void Deactivate()
        {
            IsActive = false;

            // 꺼졌고 플레이어가 아직 범위 안이면 다시 안내를 띄운다.
            if (_playerInRange)
                ShowPromptIfNeeded();
        }
    }
}