using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 가운데에 봉우리가 하나 있는 지형(Terrain)을 만드는 에디터 도구.
/// 메뉴: Tools > Terrain Builder
///
/// 지형의 높이는 격자 모양으로 나열한 숫자(높이맵)로 저장된다.
/// 이 도구는 칸마다 "가운데에서 얼마나 떨어졌는가"로 높이를 계산해 그 숫자를 채운다.
///   봉우리   가운데가 가장 높고, 반경 끝에서 평지와 부드럽게 이어진다
///   굴곡     평지가 밋밋하지 않도록 전체에 얕은 굴곡을 더한다 (0이면 완전히 평평)
///
/// 만든 뒤에는 Terrain의 Inspector에서 붓으로 자유롭게 고칠 수 있다.
/// 다만 이 도구를 다시 실행하면 높이를 처음부터 다시 계산하므로, 붓으로 고친 높이는 사라진다.
/// (칠해 둔 바닥 텍스처는 그대로 남는다)
///
/// 지형의 한가운데가 월드의 원점(0, 0, 0)에 오도록 놓는다.
/// Stage가 원점에 생성되고 PathGrid도 자기 위치를 중심으로 격자를 깔기 때문이다.
/// </summary>
public class TerrainBuilder : EditorWindow
{
    private const string Folder = "Assets/Terrain";
    private const string DataPath = Folder + "/ForestTerrainData.asset";
    private const string LayerPath = Folder + "/ForestGround.terrainlayer";
    private const string TerrainName = "Forest Terrain";

    /// 높이맵의 가로세로 칸 수. Unity 규칙상 (2의 거듭제곱 + 1)이어야 한다.
    /// 513이면 1000m 지형에서 한 칸이 약 2m다.
    private const int Resolution = 513;

    private float _size = 1000f;
    private float _hillHeight = 40f;
    private float _hillRadius = 200f;
    private float _bumpHeight = 1f;
    private float _bumpSize = 40f;
    private Texture2D _groundTexture;
    private Texture2D _groundNormal;
    private float _textureTile = 20f;

    [MenuItem("Tools/Terrain Builder")]
    private static void Open() => GetWindow<TerrainBuilder>("Terrain Builder");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("크기", EditorStyles.boldLabel);
        _size = EditorGUILayout.FloatField(
            new GUIContent("가로세로 길이 (m)", "지형은 정사각형이다"), _size);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("가운데 봉우리", EditorStyles.boldLabel);
        _hillHeight = EditorGUILayout.Slider(
            new GUIContent("높이 (m)", "봉우리 꼭대기의 높이. 0이면 봉우리가 없다"), _hillHeight, 0f, 200f);
        _hillRadius = EditorGUILayout.Slider(
            new GUIContent("반경 (m)", "가운데에서 이 거리까지가 봉우리다. 클수록 완만해진다"),
            _hillRadius, 10f, _size * 0.5f);

        // 높이와 반경을 바꾸면 경사가 어떻게 되는지 바로 보여준다.
        float steepest = SteepestHillAngle();
        EditorGUILayout.HelpBox(
            $"봉우리에서 가장 가파른 곳의 기울기: {steepest:0.0}도\n" +
            "플레이어(CharacterController)는 Slope Limit(지금 45도)보다 가파른 곳을 오르지 못한다.",
            steepest > 45f ? MessageType.Warning : MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("평지의 굴곡", EditorStyles.boldLabel);
        _bumpHeight = EditorGUILayout.Slider(
            new GUIContent("굴곡 높이 (m)", "0이면 완전히 평평하다"), _bumpHeight, 0f, 10f);
        _bumpSize = EditorGUILayout.Slider(
            new GUIContent("굴곡 하나의 너비 (m)", "클수록 넓고 완만한 굴곡이 된다"), _bumpSize, 5f, 200f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("바닥 텍스처 (선택)", EditorStyles.boldLabel);
        _groundTexture = (Texture2D)EditorGUILayout.ObjectField(
            new GUIContent("색상", "반복해서 깔아도 이음매가 안 보이는 텍스처여야 한다"),
            _groundTexture, typeof(Texture2D), false);
        _groundNormal = (Texture2D)EditorGUILayout.ObjectField("노멀맵", _groundNormal, typeof(Texture2D), false);
        _textureTile = EditorGUILayout.FloatField(
            new GUIContent("한 장의 너비 (m)", "텍스처 한 장이 덮는 땅의 너비"), _textureTile);

        EditorGUILayout.Space();
        if (GUILayout.Button("지형 만들기 / 다시 만들기"))
            Build();
    }

    /// <summary>
    /// 가운데에서 distance만큼 떨어진 곳의 봉우리 높이.
    /// 코사인 곡선을 쓰면 꼭대기와 가장자리 양쪽에서 기울기가 0이 되어,
    /// 꼭대기는 둥글고 평지와는 꺾이는 곳 없이 이어진다.
    /// </summary>
    private float HillHeightAt(float distance)
    {
        if (distance >= _hillRadius)
            return 0f;

        return _hillHeight * 0.5f * (1f + Mathf.Cos(Mathf.PI * distance / _hillRadius));
    }

    // 코사인 곡선은 반경의 절반 지점에서 가장 가파르고, 그 기울기는 (높이 × π) ÷ (반경 × 2)다.
    private float SteepestHillAngle()
        => Mathf.Atan(_hillHeight * Mathf.PI / (_hillRadius * 2f)) * Mathf.Rad2Deg;

    private void Build()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets", "Terrain");

        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(DataPath);
        if (data == null)
        {
            data = new TerrainData();
            AssetDatabase.CreateAsset(data, DataPath);
        }
        else if (!EditorUtility.DisplayDialog(
            "지형 다시 만들기",
            "높이를 처음부터 다시 계산합니다.\n붓으로 고친 높이는 사라집니다. 계속할까요?",
            "다시 만들기", "취소"))
        {
            return;
        }
        else
            Undo.RegisterCompleteObjectUndo(data, "지형 다시 만들기");

        // 높이맵에는 0~1 사이의 비율만 들어가고, 실제 높이는 (비율 × 지형의 최대 높이)다.
        // 그래서 가장 높은 곳이 1을 넘지 않도록 최대 높이를 넉넉히 잡는다.
        float maxHeight = _hillHeight + _bumpHeight + 1f;

        // 칸 수를 바꾸면 크기가 초기화되므로 칸 수를 먼저 정한다.
        data.heightmapResolution = Resolution;
        data.size = new Vector3(_size, maxHeight, _size);

        var heights = new float[Resolution, Resolution];
        for (int z = 0; z < Resolution; z++)
        {
            for (int x = 0; x < Resolution; x++)
            {
                // 이 칸이 지형의 한가운데에서 얼마나 떨어져 있는가 (m)
                float worldX = (x / (Resolution - 1f) - 0.5f) * _size;
                float worldZ = (z / (Resolution - 1f) - 0.5f) * _size;
                float distance = Mathf.Sqrt(worldX * worldX + worldZ * worldZ);

                float height = HillHeightAt(distance);
                // PerlinNoise는 가까운 곳끼리 비슷한 값이 나오는 무작위 값(0~1)이라 자연스러운 굴곡이 된다.
                // 음수 좌표에서는 무늬가 대칭으로 나오므로 좌표를 양수 쪽으로 밀어 준다.
                height += Mathf.PerlinNoise(
                    (worldX + _size) / _bumpSize, (worldZ + _size) / _bumpSize) * _bumpHeight;

                // 높이맵은 [z, x] 순서다.
                heights[z, x] = height / maxHeight;
            }
        }
        data.SetHeights(0, 0, heights);

        // 이미 바닥 텍스처를 칠해 두었다면 건드리지 않는다.
        if (_groundTexture != null && data.terrainLayers.Length == 0)
            data.terrainLayers = new[] { GetGroundLayer() };

        GameObject terrainObject = GameObject.Find(TerrainName);
        if (terrainObject == null)
        {
            terrainObject = Terrain.CreateTerrainGameObject(data);
            terrainObject.name = TerrainName;
            Undo.RegisterCreatedObjectUndo(terrainObject, "지형 만들기");
        }
        // Terrain은 한쪽 모서리가 기준점이라, 절반만큼 당겨야 한가운데가 원점에 온다.
        terrainObject.transform.position = new Vector3(-_size * 0.5f, 0f, -_size * 0.5f);

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(terrainObject.scene);
        Selection.activeGameObject = terrainObject;
    }

    // 바닥 전체에 깔 텍스처 한 겹(TerrainLayer)을 만들거나, 이미 있으면 값만 고쳐서 돌려준다.
    private TerrainLayer GetGroundLayer()
    {
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerPath);
        if (layer == null)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, LayerPath);
        }

        layer.diffuseTexture = _groundTexture;
        layer.normalMapTexture = _groundNormal;
        layer.tileSize = new Vector2(_textureTile, _textureTile);
        EditorUtility.SetDirty(layer);
        return layer;
    }
}
