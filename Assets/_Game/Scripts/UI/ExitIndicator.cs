using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Squad
{
    /// <summary>
    /// 출구가 화면 밖에 있을 때, 화면 가장자리에 출구 쪽을 가리키는 화살표와 남은 거리를 표시한다.
    /// 출구가 화면 안에 보이면 숨긴다.
    ///
    /// 방향을 구하는 방법:
    ///   출구의 위치를 "화면의 어느 지점에 찍히는가"로 바꾼 뒤(화면 밖이면 화면 범위를 벗어난 값이 나온다),
    ///   화면 한가운데에서 그 지점으로 향하는 방향을 따라 화면 가장자리까지 간 곳에 화살표를 놓는다.
    ///
    /// 화살표와 거리 글자는 이 스크립트가 시작할 때 스스로 만든다.
    /// 화살표 그림을 따로 연결하지 않으면 흰 삼각형을 직접 그려서 쓴다.
    ///
    /// 씬 구성:
    ///   Canvas 아래의 GameUI처럼 화면 전체를 덮는 UI 오브젝트에 붙인다.
    ///   GameUI에 붙이면 플레이 중에만 켜지므로 다른 화면에서는 보이지 않는다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ExitIndicator : MonoBehaviour
    {
        [Header("□ 선택 연결 — 비워두면 자동 처리")]
        [Tooltip("화살표 그림. 위쪽을 가리키는 그림이어야 한다. 비워두면 흰 삼각형을 그려서 쓴다")]
        [SerializeField] private Sprite arrowSprite;
        [Tooltip("거리 글자의 글꼴. 비워두면 TextMeshPro 기본 글꼴을 쓴다")]
        [SerializeField] private TMP_FontAsset font;

        [Header("○ 튜닝 값 — 자유롭게 조절")]
        [Tooltip("화살표와 글자의 색")]
        [SerializeField] private Color color = new Color(1f, 0.85f, 0.3f);
        [Tooltip("화살표의 크기 (가로, 세로)")]
        [SerializeField] private Vector2 arrowSize = new Vector2(40f, 48f);
        [Tooltip("화면 가장자리에서 화살표까지의 여백")]
        [SerializeField] private float edgeMargin = 70f;
        [Tooltip("거리 글자의 크기")]
        [SerializeField] private float fontSize = 28f;
        [Tooltip("화살표에서 화면 안쪽으로 글자를 얼마나 띄울지")]
        [SerializeField] private float labelOffset = 50f;
        [Tooltip("거리 표시 형식. {0}=남은 거리(m)")]
        [SerializeField] private string distanceFormat = "{0:0}m";

        private RectTransform _area;
        private RectTransform _arrow;
        private Image _arrowImage;
        private TextMeshProUGUI _label;
        private Camera _camera;
        // 플레이어는 라운드마다 새로 생성되므로, 사라지면 다시 찾는다.
        private Player _player;

        private void Awake()
        {
            _area = GetComponent<RectTransform>();
            CreateArrow();
            CreateLabel();
            SetVisible(false);
        }

        // 카메라가 플레이어를 따라 움직인 뒤에 계산해야 화살표가 한 프레임 늦지 않으므로 LateUpdate에서 한다.
        private void LateUpdate()
        {
            Exit exit = GameManager.Instance != null ? GameManager.Instance.CurrentExit : null;
            if (_camera == null)
                _camera = Camera.main;

            if (exit == null || _camera == null)
            {
                SetVisible(false);
                return;
            }

            // 출구가 화면의 어디에 찍히는가. x, y는 화면 왼쪽 아래가 (0, 0), 오른쪽 위가 (1, 1)이고,
            // z는 카메라 앞쪽이면 양수, 뒤쪽이면 음수다.
            Vector3 viewport = _camera.WorldToViewportPoint(exit.transform.position);
            bool onScreen = viewport.z > 0f
                            && viewport.x >= 0f && viewport.x <= 1f
                            && viewport.y >= 0f && viewport.y <= 1f;
            if (onScreen)
            {
                SetVisible(false);
                return;
            }

            // 화면 한가운데에서 출구가 찍히는 지점으로 향하는 방향 (이 UI의 크기 기준)
            Vector2 areaSize = _area.rect.size;
            Vector2 direction = new Vector2(
                (viewport.x - 0.5f) * areaSize.x,
                (viewport.y - 0.5f) * areaSize.y);
            // 카메라 뒤쪽에 있는 점은 화면에 뒤집혀 찍히므로 방향을 다시 뒤집는다.
            if (viewport.z < 0f)
                direction = -direction;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.down;

            // 그 방향으로 뻗어 나가다가 (여백을 뺀) 화면 테두리에 처음 닿는 곳에 화살표를 놓는다.
            // 가로와 세로 중 먼저 닿는 쪽이 기준이 된다.
            Vector2 half = areaSize * 0.5f - Vector2.one * edgeMargin;
            float scaleX = Mathf.Abs(direction.x) > 0.0001f ? half.x / Mathf.Abs(direction.x) : float.MaxValue;
            float scaleY = Mathf.Abs(direction.y) > 0.0001f ? half.y / Mathf.Abs(direction.y) : float.MaxValue;
            Vector2 arrowPosition = direction * Mathf.Min(scaleX, scaleY);

            _arrow.anchoredPosition = arrowPosition;
            // 화살표 그림은 위쪽(각도 90도)을 가리키므로, 구한 각도에서 90도를 뺀 만큼 돌린다.
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            _arrow.localRotation = Quaternion.Euler(0f, 0f, angle);

            // 글자는 돌리지 않고, 화살표보다 화면 안쪽에 둔다.
            _label.rectTransform.anchoredPosition = arrowPosition - direction.normalized * labelOffset;
            _label.text = string.Format(distanceFormat, DistanceToExit(exit));

            SetVisible(true);
        }

        // 플레이어에서 출구까지, 높이를 뺀 거리. 플레이어를 못 찾으면 0.
        private float DistanceToExit(Exit exit)
        {
            if (_player == null)
                _player = FindObjectOfType<Player>();
            if (_player == null)
                return 0f;

            Vector3 offset = exit.transform.position - _player.transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        private void SetVisible(bool visible)
        {
            _arrowImage.enabled = visible;
            _label.enabled = visible;
        }

        // ── 화살표와 글자 만들기 ─────────────────────────────────────

        private void CreateArrow()
        {
            var arrowObject = new GameObject("Exit Arrow", typeof(RectTransform));
            _arrow = arrowObject.GetComponent<RectTransform>();
            _arrow.SetParent(transform, false);
            _arrow.sizeDelta = arrowSize;

            _arrowImage = arrowObject.AddComponent<Image>();
            _arrowImage.sprite = arrowSprite != null ? arrowSprite : CreateTriangleSprite();
            _arrowImage.color = color;
            // 표시만 하는 그림이라 클릭을 가로채지 않게 한다.
            _arrowImage.raycastTarget = false;
        }

        private void CreateLabel()
        {
            var labelObject = new GameObject("Exit Distance", typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);

            _label = labelObject.AddComponent<TextMeshProUGUI>();
            _label.rectTransform.sizeDelta = new Vector2(200f, 50f);
            if (font != null)
                _label.font = font;
            _label.fontSize = fontSize;
            _label.color = color;
            _label.alignment = TextAlignmentOptions.Center;
            _label.enableWordWrapping = false;
            _label.raycastTarget = false;
        }

        // 위쪽을 가리키는 흰 삼각형 그림을 직접 만든다. (화살표 그림을 연결하지 않았을 때 쓴다)
        private static Sprite CreateTriangleSprite()
        {
            const int Size = 64;
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var pixels = new Color32[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                // 아래쪽 줄일수록 넓고, 맨 위에서는 한 점으로 모인다.
                float halfWidth = (1f - y / (Size - 1f)) * Size * 0.5f;
                for (int x = 0; x < Size; x++)
                {
                    bool inside = Mathf.Abs(x + 0.5f - Size * 0.5f) <= halfWidth;
                    pixels[y * Size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f));
        }
    }
}
