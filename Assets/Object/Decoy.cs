using UnityEngine;

namespace Squad
{
    public class Decoy : MonoBehaviour
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
    }
}