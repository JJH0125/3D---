using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>카메라에 붙일 스크립트. 플레이어가 생성되면 쿼터뷰 형태로 플레이어를 비춘다.</summary>
public class Follow : MonoBehaviour
{
    [Header("○ 튜닝 값 — 자유롭게 조절")]
    [Tooltip("플레이어 기준 카메라의 위치")]
    [SerializeField] private Vector3 offset;

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
        transform.position = target.position + offset;
    }

    public void SetTarget(Transform player) => target = player;
}
