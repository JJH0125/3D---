using System.Collections;
using UnityEngine;

namespace Squad
{
    /// <summary>
    /// 플레이어가 횃불을 들고 던지는 것을 관리한다.
    ///   들기    모닥불(Campfire)이 TakeTorch를 부른다. 횃불 프리팹을 만들어 손뼈에 붙인다
    ///   던지기  던지는 키를 누르면, 들고 있던 바로 그 횃불을 손에서 떼어 바라보는 방향으로 던진다
    ///
    /// 들고 있는 횃불과 던져진 횃불은 같은 오브젝트다. 손에 있는 동안은 플레이어의 자식이라 따라다니고,
    /// 던지는 순간 Stage의 자식으로 옮겨져 혼자 날아간다. (Stage의 자식이므로 라운드가 끝나면 함께 사라진다)
    ///
    /// 횃불은 한 번에 하나만 들 수 있다. 던진 뒤에는 모닥불에서 다시 집어야 하며,
    /// 던지고 나서 pickupCooldown(2.5초)이 지나야 다시 집을 수 있다.
    ///
    /// 모델에서 손뼈를 찾으면 횃불을 손에 쥐여서, 달리거나 걸을 때 팔과 함께 흔들리게 한다.
    /// 손뼈를 찾지 못하면(뼈 이름이 다른 모델 등) 예전처럼 몸 옆의 정해진 자리에 든다.
    ///
    /// 손에 든 동안에는 횃불의 불빛을 플레이어 앞쪽으로 내어 둬서, 밝은 곳이 진행 방향으로 쏠리게 한다.
    /// 던지면 불빛은 횃불 자리로 돌아가므로, 앞을 비추던 빛을 잃는다. (미끼로 쓸지, 앞을 볼지의 선택)
    /// 플레이어 주변의 작은 기본 조명은 Player 아래의 일반 Light가 맡고, 적의 시야 규칙과는 상관없다.
    ///
    /// 모델의 Animator에 던지기 Trigger 파라미터가 있으면, 던지는 애니메이션을 재생하고
    /// 팔을 휘두르는 순간(releaseDelay 뒤)에 횃불을 놓는다. 파라미터가 없으면 키를 누르는 즉시 던진다.
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
        [Tooltip("횃불을 쥘 손뼈의 이름. 켜져 있는 모델에서 찾는다")]
        [SerializeField] private string handBoneName = "mixamorig:RightHand";
        [Tooltip("손뼈를 기준으로 한, 주먹 안쪽(횃불을 쥐는 자리)의 위치(m, 모델 Scale 1 기준). 손뼈의 축 방향 기준이다. " +
                 "Mixamo 뼈대에서는 Y가 손가락 쪽, Z가 손바닥이 향하는 쪽, -X가 엄지 쪽이다")]
        [SerializeField] private Vector3 handOffset = new Vector3(0f, 0.07f, 0.04f);
        [Tooltip("손뼈를 기준으로 한 횃불의 각도. Z가 90이면 횃불의 불꽃 쪽이 엄지 방향을 향한다")]
        [SerializeField] private Vector3 handRotation = new Vector3(0f, 0f, 120f);
        [Tooltip("횃불에서 손으로 쥐는 지점. 횃불 프리팹의 모델 기준이다. " +
                 "횃불 모델의 기준점은 막대의 한가운데이고 불꽃 쪽이 +Y이므로, 음수일수록 손잡이 끝 쪽을 쥔다")]
        [SerializeField] private Vector3 gripPoint = new Vector3(0f, -0.12f, 0f);
        [Tooltip("손뼈를 찾지 못했을 때 횃불을 드는 위치. 플레이어 오브젝트 기준 (오른쪽, 위, 앞). 플레이어 오브젝트의 위치는 발이 아니라 몸 한가운데다")]
        [SerializeField] private Vector3 holdPosition = new Vector3(0.65f, 0.2f, 0.35f);
        [Tooltip("손뼈를 찾지 못했을 때 횃불을 드는 각도. 플레이어 오브젝트 기준")]
        [SerializeField] private Vector3 holdRotation = new Vector3(10f, 0f, -25f);
        [Tooltip("던질 때 앞으로 나가는 속도(m/s)")]
        [SerializeField] private float throwSpeed = 48f;
        [Tooltip("던질 때 위로 솟는 속도(m/s). 클수록 높이, 멀리 날아간다")]
        [SerializeField] private float throwUpSpeed = 24f;
        [Tooltip("던지는 애니메이션을 재생할 Animator의 Trigger 파라미터 이름. Animator에 없으면 애니메이션 없이 바로 던진다")]
        [SerializeField] private string throwTrigger = "throw";
        [Tooltip("던지는 애니메이션이 시작된 뒤 횃불을 손에서 놓기까지의 시간(초). 팔을 앞으로 휘두르는 순간에 맞춘다")]
        [SerializeField] private float releaseDelay = 0.35f;
        [Tooltip("횃불을 던진 뒤 다시 집을 수 있을 때까지 기다려야 하는 시간(초)")]
        [SerializeField] private float pickupCooldown = 2.5f;
        [Tooltip("던진 횃불이 떨어지는 소리를 들을 수 있는 적의 레이어")]
        [SerializeField] private LayerMask enemyLayer = (1 << 8) | (1 << 9);   // RealEnemy + FakeEnemy

        [Header("○ 손에 든 횃불의 불빛")]
        [Tooltip("불빛의 중심을 플레이어 앞으로 얼마나 내어 둘지(m). 빛은 둥글게 퍼지지만 밝은 곳이 진행 방향으로 쏠린다")]
        [SerializeField] private float heldLightForward = 6f;
        [Tooltip("불빛의 높이(m). 플레이어 오브젝트 위치(발에서 1m 위) 기준")]
        [SerializeField] private float heldLightHeight = 3f;
        [Tooltip("손에 들고 있는 동안 불빛이 닿는 거리(m). 던지면 횃불 프리팹에 정해 둔 거리로 돌아간다")]
        [SerializeField] private float heldLightRange = 22f;

        private Player _player;
        // 지금 들고 있는 횃불. 빈손이면 null.
        private Torch _torch;
        // 들고 있는 횃불의 불빛
        private FireSource _torchFire;
        // 횃불을 쥘 손뼈. 찾지 못하면 null.
        private Transform _hand;
        // 던지는 애니메이션을 재생할 Animator. 던지기 Trigger가 없으면 null.
        private Animator _animator;
        // 던지는 애니메이션이 재생 중이라 아직 손에서 놓지 않은 상태인지
        private bool _throwing;

        // 다시 횃불을 집을 수 있게 되는 시각
        private float _canTakeTime;

        public bool HasTorch => _torch != null;

        /// <summary>
        /// 지금 횃불을 집을 수 있는가. 빈손이고, 던진 뒤 pickupCooldown이 지났어야 한다.
        /// </summary>
        public bool CanTakeTorch => !HasTorch && Time.time >= _canTakeTime;

        private void Awake()
        {
            _player = GetComponent<Player>();
            _hand = FindActiveChild(transform, handBoneName);

            // 꺼 둔 예전 모델의 Animator는 건너뛴다.
            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null && HasTrigger(animator, throwTrigger))
                _animator = animator;
        }

        private void Update()
        {
            // 일시정지 중에는 던지지 않는다.
            if (Time.timeScale == 0f)
                return;

            if (!HasTorch || _throwing || !Input.GetKeyDown(throwKey))
                return;

            if (_animator == null)
            {
                ThrowTorch();
                return;
            }

            _animator.SetTrigger(throwTrigger);
            StartCoroutine(ThrowAfterDelay());
        }

        // 팔을 휘두르는 순간까지 횃불을 손에 쥔 채로 기다렸다가 놓는다.
        private IEnumerator ThrowAfterDelay()
        {
            _throwing = true;
            yield return new WaitForSeconds(releaseDelay);
            _throwing = false;

            if (HasTorch)
                ThrowTorch();
        }

        private static bool HasTrigger(Animator animator, string parameterName)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == parameterName)
                    return true;

            return false;
        }

        /// <summary>
        /// 횃불을 하나 만들어 손에 든다. 이미 들고 있으면 아무 일도 하지 않는다.
        /// </summary>
        public void TakeTorch()
        {
            if (!CanTakeTorch || torchPrefab == null)
                return;

            if (_hand == null)
            {
                _torch = Instantiate(torchPrefab, transform);
                _torch.transform.localPosition = holdPosition;
                _torch.transform.localRotation = Quaternion.Euler(holdRotation);
            }
            else
            {
                // 손뼈는 모델을 들여올 때의 크기 배율을 갖고 있을 수 있다.
                // 월드 기준 크기를 그대로 두고 붙여야 횃불이 갑자기 커지거나 작아지지 않는다.
                _torch = Instantiate(torchPrefab);
                _torch.transform.SetParent(_hand, true);
                PlaceInHand();
            }

            _torchFire = _torch.GetComponent<FireSource>();
            PlaceHeldLight();
        }

        // 손에 든 횃불의 자리와 불빛을 맞춘다. 값을 바꾸면 플레이 중에도 바로 보이도록 매 프레임 맞춘다.
        // (애니메이션이 손을 움직인 뒤에 맞춰야 하므로 LateUpdate에서 한다)
        private void LateUpdate()
        {
            if (!HasTorch)
                return;

            if (_hand != null && _torch.transform.parent == _hand)
                PlaceInHand();
            PlaceHeldLight();
        }

        // 손에 든 횃불의 불빛을 플레이어 앞쪽에 둔다.
        // 빛은 여전히 둥글게 퍼지지만, 밝은 곳이 진행 방향으로 쏠려서 앞을 비추는 도구가 된다.
        // (적에게 드러나는 범위는 FireSource가 횃불 자체의 자리로 재므로 바뀌지 않는다)
        private void PlaceHeldLight()
        {
            if (_torchFire == null)
                return;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            _torchFire.PlaceLight(
                transform.position + forward * heldLightForward + Vector3.up * heldLightHeight, heldLightRange);
        }

        // 위치는 미터 단위로 다루기 위해, 손뼈의 크기 배율을 빼고 방향만 써서 계산한다.
        // 대신 모델을 키우거나 줄인 만큼은 따라가도록, 모델의 Scale을 곱한다.
        // 횃불의 기준점(막대 한가운데)이 아니라 손잡이의 gripPoint가 주먹 안쪽에 오도록 옮긴다.
        private void PlaceInHand()
        {
            Transform torch = _torch.transform;
            Vector3 fist = _hand.position + _hand.rotation * (handOffset * ModelScale());
            torch.rotation = _hand.rotation * Quaternion.Euler(handRotation);
            torch.position = fist - torch.TransformVector(gripPoint);
        }

        // 손뼈가 들어 있는 모델(플레이어 바로 아래 자식)의 Scale.
        private float ModelScale()
        {
            Transform model = _hand;
            while (model.parent != null && model.parent != transform)
                model = model.parent;

            return model.localScale.x;
        }

        // root 아래에서 이름이 같은 오브젝트를 찾는다. 꺼 둔 모델(예전 모델 등)의 뼈는 건너뛴다.
        private static Transform FindActiveChild(Transform root, string childName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>())
                if (child.name == childName)
                    return child;

            return null;
        }

        private void ThrowTorch()
        {
            Torch torch = _torch;
            _torch = null;
            _canTakeTime = Time.time + pickupCooldown;

            // 손을 떠난 횃불은 다시 자기 자리에서 사방을 비춘다. 앞을 비추던 불빛도 함께 잃는다.
            if (_torchFire != null)
                _torchFire.ResetLight();
            _torchFire = null;

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
