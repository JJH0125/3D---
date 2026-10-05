using UnityEngine;
using UnityEngine.Serialization;

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
    /// 플레이어에게 들리는 작동음은 Emit(적에게 알리기)과 따로 재생한다.
    /// Emit은 0.5초마다 호출되므로 거기서 효과음을 틀면 소리가 계속 겹친다.
    /// 그래서 이 오브젝트에 반복 재생용 AudioSource를 두고, 켜고 끌 때 재생·정지한다.
    /// 발전기가 파괴되면(라운드 종료) 소리도 함께 사라지고, 일시정지는 AudioListener.pause가 처리한다.
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
        [Tooltip("꺼진 발전기의 범위 안에서 화면에 띄울 안내 문구")]
        [FormerlySerializedAs("promptMessage1")]    // 이름을 바꿔도 프리팹에 저장된 문구를 유지
        [SerializeField] private string promptMessage = "[E] 발전기 켜기";
        [Tooltip("작동시키는 키")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [Tooltip("켤 때 한 번 나는 소리(딸깍). 적에게는 들리지 않는다")]
        [SerializeField] private AudioClip turningonSound;
        [Tooltip("작동 중 반복 재생할 소리. 비워두면 적에게만 들리고 플레이어에게는 들리지 않는다")]
        [SerializeField] private AudioClip activeSound;
        [Tooltip("작동음 볼륨")]
        [Range(0f, 1f)]
        [SerializeField] private float activeVolume = 0.6f;
        [Tooltip("이 거리(m)까지는 최대 크기로 들린다")]
        [SerializeField] private float minDistance = 10f;
        [Tooltip("이 거리(m)부터는 들리지 않는다")]
        [SerializeField] private float maxDistance = 40f;

        // 작동 중인지 여부. 외부(플레이어 상호작용)에서 켜고 끌 수 있다.
        public bool IsActive { get; private set; }
        private float _emitTimer;
        // 작동시킬 수 있는 범위 내에 플레이어가 들어와있는지 여부.
        private bool _playerInRange;
        // 작동음을 반복 재생할 AudioSource. Awake에서 직접 만든다.
        private AudioSource _activeSource;

        private void Awake()
        {
            // 기존 프리팹에도 따로 붙일 필요가 없도록 코드에서 만든다.
            _activeSource = gameObject.AddComponent<AudioSource>();
            _activeSource.playOnAwake = false;
            _activeSource.loop = true;
            _activeSource.clip = activeSound;
            // SfxPlayer의 월드 소리와 같은 3D 설정 (쿼터뷰라 거리를 넉넉히 잡은 직선 감쇠)
            _activeSource.spatialBlend = 1f;
            _activeSource.rolloffMode = AudioRolloffMode.Linear;
        }

        private void Start()
        {
            IsActive = startsActive;
            GameManager.Instance.AddGenerator(this);
            UpdateActiveSound();
        }

        private void Update()
        {
            // 일시정지 중에는 상호작용하지 않는다.
            // (소리 방출 타이머도 deltaTime이 0이라 어차피 진행되지 않는다)
            if (Time.timeScale == 0f)
                return;

            // 범위 안 + 꺼져 있음 + 키 입력 → 켠다.
            if (_playerInRange && !IsActive && Input.GetKeyDown(interactKey))
                Activate();

            if (!IsActive)
                return;

            _emitTimer -= Time.deltaTime;

            if (_emitTimer <= 0f)
            {
                SoundEmitter.Emit(transform.position, 
                SoundList.Generator, enemyLayer, Dimension.None, gameObject);
                _emitTimer = emitInterval;
            }
        }

        // ── 상호작용 범위 ─────────────────────────────────────────────

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

        private void ShowPrompt()
        {
            // 켜진 발전기는 더 할 수 있는 게 없으므로 안내를 띄우지 않는다.
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
            UpdateActiveSound();

            // 딸깍 소리는 플레이어에게만 들려야 하므로 Emit을 거치지 않고 SfxPlayer로 직접 재생한다.
            if (SfxPlayer.Instance != null)
                SfxPlayer.Instance.PlayAt(turningonSound, transform.position);

            GameManager.Instance.CheckExit();    // 발전기가 켜질 때마다 manager가 출구 활성화를 검사.

            // 켜진 뒤에는 할 수 있는 게 없으므로 띄워 둔 "켜기" 안내를 내린다.
            if (_playerInRange)
                HidePrompt();
        }

        /// <summary>
        /// 발전기를 끈다. 플레이어는 끌 수 없고, 코드에서만 호출한다.
        /// (나중에 추격자가 발전기를 끄는 기능 등에 쓸 수 있도록 남겨 둔다)
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            UpdateActiveSound();

            // 꺼진 것도 화면의 발전기 수에 반영한다.
            GameManager.Instance.CheckExit();

            // 꺼졌고 플레이어가 아직 범위 안이면 다시 안내를 띄운다.
            if (_playerInRange)
                ShowPrompt();
        }

        // 켜져 있으면 작동음을 틀고, 꺼져 있으면 멈춘다.
        private void UpdateActiveSound()
        {
            if (activeSound == null)
                return;

            if (IsActive)
            {
                // 인스펙터에서 바꾼 값이 다음에 켤 때 반영되도록 재생할 때마다 넣는다.
                _activeSource.volume = activeVolume;
                _activeSource.minDistance = minDistance;
                _activeSource.maxDistance = maxDistance;
                _activeSource.Play();
            }
            else
            {
                _activeSource.Stop();
            }
        }
    }
}