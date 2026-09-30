using UnityEngine;

/// <summary>
/// 디버그용. 플레이어가 벽처럼 부딪힌 Collider를 Console에 출력한다.
/// 눈에 안 보이는 벽에 막힐 때, 무엇이 막고 있는지 찾는 데 쓴다.
///
/// Console의 로그를 클릭하면 부딪힌 오브젝트가 Hierarchy에서 강조된다.
///
/// 씬 구성:
///   CharacterController가 있는 플레이어 오브젝트에 붙인다. 확인이 끝나면 떼어낸다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CollisionLogger : MonoBehaviour
{
    [Tooltip("이 값보다 수직에 가까운 면에 부딪혔을 때만 출력한다 (0: 벽, 1: 평평한 바닥)")]
    [Range(0f, 1f)]
    [SerializeField] private float maxNormalY = 0.7f;

    // 같은 Collider에 계속 부딪히는 동안 로그가 매 프레임 쌓이지 않도록 기억해 둔다.
    private Collider _lastHit;

    // CharacterController가 Move 중에 무언가에 닿을 때마다 호출된다.
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 발밑의 바닥은 무시하고, 앞을 가로막는 면만 본다.
        if (hit.normal.y > maxNormalY || hit.collider == _lastHit)
            return;

        _lastHit = hit.collider;
        Debug.Log(
            $"[막힘] {GetPath(hit.transform)}  " +
            $"(Collider: {hit.collider.GetType().Name}, 면 기울기 normal.y: {hit.normal.y:0.00})",
            hit.gameObject);
    }

    // 이름이 겹치는 오브젝트가 많으므로 부모까지 이어 붙여서 보여준다.
    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
