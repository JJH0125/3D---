using UnityEngine;

namespace Squad
{
    /// <summary>
    /// 플레이어가 횃불을 들고 던지는 것을 관리한다.
    ///   들기    모닥불(Campfire)이 TakeTorch를 부른다. 횃불 프리팹을 만들어 손 위치에 붙인다
    ///   던지기  던지는 키를 누르면, 들고 있던 바로 그 횃불을 손에서 떼어 바라보는 방향으로 던진다
    ///
    /// 들고 있는 횃불과 던져진 횃불은 같은 오브젝트다. 손에 있는 동안은 플레이어의 자식이라 따라다니고,
    /// 던지는 순간 Stage의 자식으로 옮겨져 혼자 날아간다. (Stage의 자식이므로 라운드가 끝나면 함께 사라진다)
    ///
    /// 횃불은 한 번에 하나만 들 수 있다. 던진 뒤에는 모닥불에서 다시 집어야 한다.
    ///
    /// 씬 구성:
    ///   Player가 붙은 오브젝트에 붙이고, torchPrefab에 횃불 프리팹(Torch + FireSource)을 연결한다.
    /// </summary>
    [RequireComponent(typeof(Player))]
    public class TorchHolder : MonoBehaviour
    {
        [Header("■ 필수 연결 — 비워두면 에러")]
        [Tooltip("횃불 프리팹. Torch와 FireSource가 붙어 있어야 한다")]
        [SerializeField] private Torch torchPrefab;

        [Header("○ 튜닝 값 — 자유롭게 조절")]
        [Tooltip("횃불을 던지는 키")]
        [SerializeField] private KeyCode throwKey = KeyCode.Q;
        [Tooltip("횃불을 드는 위치. 플레이어 오브젝트 기준 (오른쪽, 위, 앞). 플레이어 오브젝트의 위치는 발이 아니라 몸 한가운데다")]
        [SerializeField] private Vector3 holdPosition = new Vector3(0.65f, 0.2f, 0.35f);
        [Tooltip("횃불을 드는 각도. 플레이어 오브젝트 기준")]
        [SerializeField] private Vector3 holdRotation = new Vector3(10f, 0f, -25f);
        [Tooltip("던질 때 앞으로 나가는 속도(m/s)")]
        [SerializeField] private float throwSpeed = 48f;
        [Tooltip("던질 때 위로 솟는 속도(m/s). 클수록 높이, 멀리 날아간다")]
        [SerializeField] private float throwUpSpeed = 24f;
        [Tooltip("던진 횃불이 떨어지는 소리를 들을 수 있는 적의 레이어")]
        [SerializeField] private LayerMask enemyLayer = (1 << 8) | (1 << 9);   // RealEnemy + FakeEnemy

        private Player _player;
        // 지금 들고 있는 횃불. 빈손이면 null.
        private Torch _torch;

        public bool HasTorch => _torch != null;

        private void Awake() => _player = GetComponent<Player>();

        private void Update()
        {
            // 일시정지 중에는 던지지 않는다.
            if (Time.timeScale == 0f)
                return;

            if (HasTorch && Input.GetKeyDown(throwKey))
                ThrowTorch();
        }

        /// <summary>
        /// 횃불을 하나 만들어 손에 든다. 이미 들고 있으면 아무 일도 하지 않는다.
        /// </summary>
        public void TakeTorch()
        {
            if (HasTorch || torchPrefab == null)
                return;

            _torch = Instantiate(torchPrefab, transform);
            _torch.transform.localPosition = holdPosition;
            _torch.transform.localRotation = Quaternion.Euler(holdRotation);
        }

        private void ThrowTorch()
        {
            Torch torch = _torch;
            _torch = null;

            // 손에서 떼어 Stage의 자식으로 옮긴다. 위치와 방향은 지금 보이는 그대로 유지한다.
            torch.transform.SetParent(transform.parent, true);

            // 비탈에 서 있으면 그 기울기를 따라 던진다.
            // 수평으로만 던지면 오르막을 향했을 때 횃불이 발 앞의 땅에 바로 박힌다.
            Vector3 forward = transform.forward;
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                // GetInterpolatedNormal은 지형의 한쪽 끝을 0, 반대쪽 끝을 1로 본 비율 좌표를 받는다.
                Vector3 local = transform.position - terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                Vector3 groundNormal = terrain.terrainData.GetInterpolatedNormal(local.x / size.x, local.z / size.z);
                forward = Vector3.ProjectOnPlane(forward, groundNormal).normalized;
            }

            Vector3 velocity = forward * throwSpeed + Vector3.up * throwUpSpeed;
            torch.Throw(velocity, _player.myDimension, enemyLayer);
        }
    }
}
