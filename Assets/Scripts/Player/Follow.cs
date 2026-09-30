using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>
/// 카메라에 붙일 스크립트. 플레이어가 생성되면 쿼터뷰 형태로 플레이어를 비춘다.
///
/// 카메라의 회전과 위치를 각도·거리 값으로부터 함께 계산하므로,
/// 각도를 바꿔도 플레이어가 항상 화면 정중앙에 온다.
/// (회전과 offset을 따로 맞추면 둘이 어긋나 플레이어가 중앙에서 벗어나기 쉽다)
///</summary>
public class Follow : MonoBehaviour
{
    [Header("○ 튜닝 값 — 자유롭게 조절")]
    [Tooltip("내려다보는 각도(도). 클수록 위에서 수직으로 내려다본다")]
    [Range(10f, 90f)]
    [SerializeField] private float pitch = 35.4f;
    [Tooltip("바라보는 방향(도). 45면 대각선 방향에서 비춘다")]
    [SerializeField] private float yaw = 45f;
    [Tooltip("플레이어와 카메라 사이의 거리(m)")]
    [SerializeField] private float distance = 26.73f;

    private Transform target;

    /// <summary>
    /// Update에서 바뀐 target의 position을
    /// LateUpdate에서 안전하게 참조하여
    /// 카메라의 위치를 바꾼다.
    /// 이를 통해 카메라가 떨리는 것을 방지할 수 있다.
    /// </summary>
    void LateUpdate()
    {
        if (target == null)
            return;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = rotation;
        // 카메라가 바라보는 방향(rotation * forward)의 반대쪽으로 distance만큼 물러난 곳에 둔다.
        transform.position = target.position - rotation * Vector3.forward * distance;
    }

    public void SetTarget(Transform player) => target = player;
}
