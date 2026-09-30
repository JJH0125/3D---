using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 외부 에셋의 풀, 덤불처럼 작은 오브젝트의 MeshCollider를 한꺼번에 끄는 에디터 도구.
/// 메뉴: Tools > Plant Collider Tool
///
/// 오브젝트 이름으로는 풀인지 알 수 없으므로 크기로 구분한다.
///   풀     가로, 세로, 높이가 모두 작다
///   나무   높다
///   바닥   넓다
/// 그래서 "가장 긴 변이 기준값보다 작은" 오브젝트만 고르면 풀만 걸린다.
///
/// 이름 필터를 채우면 그 문자열이 이름에 들어간 오브젝트만 대상으로 한다.
/// (예: "carve.photos"로 풀 카드를, "Plane"과 큰 기준 크기로 바닥을 뺀 나무 잎 카드를 고른다)
///
/// Collider를 지우지 않고 끄기만 하므로, 언제든 다시 켤 수 있다. (Ctrl+Z도 가능)
/// </summary>
public class PlantColliderTool : EditorWindow
{
    private GameObject _root;
    private float _maxSize = 3f;
    private string _nameFilter = "";
    private string _message = "";

    [MenuItem("Tools/Plant Collider Tool")]
    private static void Open() => GetWindow<PlantColliderTool>("Plant Colliders");

    private void OnSelectionChange() => Repaint();

    private void OnGUI()
    {
        _root = (GameObject)EditorGUILayout.ObjectField(
            "대상 (ForestRoad)", _root, typeof(GameObject), true);
        _maxSize = EditorGUILayout.FloatField(
            new GUIContent("기준 크기", "가장 긴 변이 이 값보다 작은 오브젝트를 풀로 본다 (월드 단위)"),
            _maxSize);
        _nameFilter = EditorGUILayout.TextField(
            new GUIContent("이름 필터", "비워두면 이름과 상관없이 크기로만 고른다"),
            _nameFilter);

        // 기준값을 정할 수 있도록, Scene에서 클릭한 오브젝트의 크기를 보여준다.
        EditorGUILayout.Space();
        GameObject selected = Selection.activeGameObject;
        Renderer selectedRenderer = selected != null ? selected.GetComponent<Renderer>() : null;
        if (selectedRenderer != null)
            EditorGUILayout.HelpBox(
                $"선택한 오브젝트: {selected.name}\n가장 긴 변: {LongestSide(selectedRenderer):0.00}",
                MessageType.None);
        else
            EditorGUILayout.HelpBox(
                "Scene에서 풀이나 나무를 클릭하면 크기가 여기 표시된다.\n" +
                "(모델의 자식을 고르려면 같은 곳을 한 번 더 클릭)",
                MessageType.None);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(_root == null))
        {
            if (GUILayout.Button("1. 기준보다 작은 오브젝트 선택 (미리보기)"))
            {
                List<MeshCollider> small = FindSmallColliders();
                var objects = new List<Object>();
                foreach (MeshCollider c in small)
                    objects.Add(c.gameObject);
                Selection.objects = objects.ToArray();
                _message = $"{small.Count}개 선택됨. Scene에서 풀만 선택됐는지 확인할 것.";
            }

            if (GUILayout.Button("2. 선택된 조건의 Collider 끄기"))
                SetColliders(FindSmallColliders(), false, "끔");

            if (GUILayout.Button("모든 Collider 다시 켜기"))
                SetColliders(new List<MeshCollider>(_root.GetComponentsInChildren<MeshCollider>(true)), true, "켬");
        }

        if (_message != "")
            EditorGUILayout.HelpBox(_message, MessageType.Info);
    }

    // 대상 아래의 MeshCollider 중, 이름 필터에 맞고
    // 같은 오브젝트의 Renderer 크기가 기준보다 작은 것만 모은다.
    private List<MeshCollider> FindSmallColliders()
    {
        var result = new List<MeshCollider>();
        foreach (MeshCollider collider in _root.GetComponentsInChildren<MeshCollider>(true))
        {
            if (_nameFilter != "" && !collider.name.Contains(_nameFilter))
                continue;

            Renderer renderer = collider.GetComponent<Renderer>();
            if (renderer != null && LongestSide(renderer) < _maxSize)
                result.Add(collider);
        }
        return result;
    }

    private void SetColliders(List<MeshCollider> colliders, bool enabled, string verb)
    {
        // Ctrl+Z로 되돌릴 수 있도록 기록한다.
        Undo.RecordObjects(colliders.ToArray(), $"Collider {verb}");
        foreach (MeshCollider collider in colliders)
            collider.enabled = enabled;
        _message = $"Collider {colliders.Count}개를 {verb}. 씬을 저장(Ctrl+S)해야 유지된다.";
    }

    private static float LongestSide(Renderer renderer)
    {
        Vector3 size = renderer.bounds.size;
        return Mathf.Max(size.x, Mathf.Max(size.y, size.z));
    }
}
