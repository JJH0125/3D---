using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follow : MonoBehaviour
{
    [Header("■ 필수 연결 — 비워두면 에러")]
    [Tooltip("따라다닐 플레이어")]
    [SerializeField] private Transform target;

    [Header("○ 튜닝 값 — 자유롭게 조절")]
    [Tooltip("플레이어 기준 카메라의 위치")]
    [SerializeField] private Vector3 offset;

    /// <summary>
    /// Update에서 바뀐 target의 position을
    /// LateUpdate에서 안전하게 참조하여
    /// 카메라의 위치를 바꾼다.
    /// 이를 통해 카메라가 떨리는 것을 방지할 수 있다.
    /// </summary>
    void LateUpdate()
    {
        transform.position = target.position + offset;
    }
}
