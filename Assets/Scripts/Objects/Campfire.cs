using UnityEngine;

namespace Squad
{
    /// <summary>
    /// 모닥불. 플레이어가 범위에 들어와 E 키를 누르면 횃불을 집어 든다.
    /// 모닥불은 꺼지지 않고, 횃불은 몇 번이든 다시 집을 수 있다.
    /// (횃불을 이미 들고 있으면 집을 수 없으므로 안내도 띄우지 않는다)
    ///
    /// 불빛은 같은 오브젝트의 FireSource가 맡는다. 이 스크립트는 상호작용만 한다.
    ///
    /// 씬 구성:
    ///   - 이 오브젝트에 Collider를 두고 Is Trigger를 켠다 (상호작용 범위)
    ///   - 같은 오브젝트에 FireSource를 붙인다
    ///   - 모닥불 모델은 자식으로 둔다
    ///   - 플레이어에게 TorchHolder가 붙어 있어야 한다
    /// </summary>
    public class Campfire : MonoBehaviour
    {
        [Header("○ 튜닝 값 — 자유롭게 조절")]
        [Tooltip("상호작용할 수 있는 플레이어 대상 레이어")]
        [SerializeField] private LayerMask playerLayer;
        [Tooltip("범위 안에서 화면에 띄울 안내 문구")]
        [SerializeField] private string promptMessage = "[E] 횃불 집기";
        [Tooltip("횃불을 집는 키")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        // 범위 안에 들어와 있는 플레이어의 횃불 관리자. 범위 밖이면 null.
        private TorchHolder _holder;
        // 지금 안내 문구를 띄워 둔 상태인지
        private bool _promptShown;

        private void Update()
        {
            // 일시정지 중에는 상호작용하지 않는다.
            if (Time.timeScale == 0f)
                return;

            // 범위 안에 있고 빈손일 때만 집을 수 있다. (던진 직후 잠깐은 빈손이어도 집을 수 없다)
            bool canTake = _holder != null && _holder.CanTakeTorch;

            if (canTake && Input.GetKeyDown(interactKey))
            {
                _holder.TakeTorch();
                canTake = false;
            }

            // 범위 안에서 횃불을 던져 빈손이 되면 안내가 다시 떠야 하므로,
            // 들어오고 나갈 때만이 아니라 매 프레임 상태를 맞춘다.
            SetPrompt(canTake);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsInLayerMask(other.gameObject.layer, playerLayer))
                return;

            _holder = other.GetComponentInParent<TorchHolder>();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsInLayerMask(other.gameObject.layer, playerLayer))
                return;

            _holder = null;
            SetPrompt(false);
        }

        // Stage가 파괴될 때는 OnTriggerExit이 불리지 않으므로 여기서도 안내를 내린다.
        private void OnDisable() => SetPrompt(false);

        private void SetPrompt(bool show)
        {
            if (show == _promptShown || InteractionPrompt.Instance == null)
                return;

            _promptShown = show;
            if (show)
                InteractionPrompt.Instance.Show(this, promptMessage);
            else
                InteractionPrompt.Instance.Hide(this);
        }

        // LayerMask는 비트로 레이어를 표시한다. 해당 레이어 비트가 켜져 있는지 확인.
        private static bool IsInLayerMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }
    }
}
