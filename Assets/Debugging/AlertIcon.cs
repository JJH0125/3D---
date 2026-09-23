using UnityEngine;
using TMPro;

namespace Squad
{
    /// <summary>
    /// 적의 머리 위에 현재 판단 상태를 기호로 띄운다.
    ///   소리 조사 중(InvestigateSound) → ?
    ///   추격 중(CatchPlayer)           → !
    ///   배회 중(Wander)               → 표시 없음
    ///
    /// 블랙보드의 경계 수준이 아니라 "이 적의 현재 목표"를 기준으로 한다.
    /// 경계 수준은 모든 적이 공유하므로, 그걸 쓰면 한 적이 발견했을 때
    /// 모든 적(다른 차원의 적까지) 머리 위에 !가 뜨게 된다.
    ///
    /// 씬 구성:
    ///   적 오브젝트 아래에 GameObject → 3D Object → Text - TextMeshPro 를 만들고
    ///   (UI용이 아니라 3D용) 머리 위 높이에 둔 뒤, 이 스크립트를 그 텍스트에 붙인다.
    ///   Body Renderer에는 DimensionMember가 켜고 끄는 것과 같은 렌더러를 연결한다.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class AlertIcon : MonoBehaviour
    {
        [Header("■ 필수 연결 — 비워두면 자동으로 찾음")]
        [Tooltip("판단 상태를 읽어올 적. 비우면 부모에서 찾는다")]
        [SerializeField] private HorrorChaserAgent agent;
        [Tooltip("적의 몸 렌더러. 이 렌더러가 꺼지면(다른 차원) 기호도 숨긴다.\nDimensionMember가 제어하는 렌더러와 같은 것을 연결할 것")]
        [SerializeField] private Renderer bodyRenderer;

        [Header("○ 튜닝 값 — 자유롭게 조절")]
        [SerializeField] private string investigateSymbol = "?";
        [SerializeField] private string chaseSymbol = "!";
        [SerializeField] private Color investigateColor = new Color(0.91f, 0.64f, 0.24f);   // 주황
        [SerializeField] private Color chaseColor = new Color(0.78f, 0.28f, 0.31f);         // 빨강
        [Tooltip("기호가 바뀌는 순간 커졌다가 돌아오는 배율")]
        [SerializeField] private float popScale = 1.8f;
        [Tooltip("튀는 효과가 원래 크기로 돌아오는 시간(초)")]
        [SerializeField] private float popDuration = 0.25f;

        private TMP_Text _label;
        private Renderer _labelRenderer;
        private Transform _camera;
        private Vector3 _baseScale;

        private string _lastGoal;     // 직전 프레임의 목표 (바뀔 때만 갱신하기 위함)
        private float _popTimer;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
            _labelRenderer = GetComponent<Renderer>();
            _baseScale = transform.localScale;

            if (agent == null)
                agent = GetComponentInParent<HorrorChaserAgent>();

            if (bodyRenderer == null && agent != null)
                bodyRenderer = FindBodyRenderer();

            if (agent == null)
                Debug.LogError($"[{name}] HorrorChaserAgent를 찾지 못했습니다", this);

            _label.text = "";
        }

        private void Start()
        {
            if (Camera.main != null)
                _camera = Camera.main.transform;
        }

        private void Update()
        {
            if (agent == null) return;

            string goal = agent.CurrentGoalName;
            if (goal == _lastGoal) return;     // 바뀌지 않았으면 할 일 없음
            _lastGoal = goal;

            switch (goal)
            {
                case "InvestigateSound":
                    _label.text = investigateSymbol;
                    _label.color = investigateColor;
                    _popTimer = popDuration;
                    break;

                case "CatchPlayer":
                    _label.text = chaseSymbol;
                    _label.color = chaseColor;
                    _popTimer = popDuration;
                    break;

                default:   // Wander 등
                    _label.text = "";
                    break;
            }
        }

        // 적이 회전과 이동을 모두 마친 뒤에 방향과 크기를 맞춘다.
        private void LateUpdate()
        {
            // 쿼터뷰 카메라는 회전하지 않으므로, 카메라와 같은 방향을 보면
            // 적이 어느 쪽을 향하든 글자가 항상 정면으로 보인다.
            if (_camera != null)
                transform.rotation = _camera.rotation;

            // 기호가 바뀐 순간 크게 튀었다가 원래 크기로 돌아온다.
            if (_popTimer > 0f)
            {
                _popTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(_popTimer / popDuration);   // 0 → 1
                float s = Mathf.Lerp(popScale, 1f, t);
                transform.localScale = _baseScale * s;
            }
            else
            {
                transform.localScale = _baseScale;
            }

            // 다른 차원에 있어 몸이 안 보이면 기호도 숨긴다.
            // (그대로 두면 보이지 않는 적의 위치가 기호로 드러난다)
            if (_labelRenderer != null && bodyRenderer != null)
                _labelRenderer.enabled = bodyRenderer.enabled;
        }

        // 연결이 비어 있을 때, 적 아래에서 자기 자신이 아닌 첫 번째 몸 렌더러를 찾는다.
        private Renderer FindBodyRenderer()
        {
            foreach (Renderer r in agent.GetComponentsInChildren<Renderer>(true))
            {
                if (r.gameObject == gameObject) continue;
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                    return r;
            }
            return null;
        }
    }
}