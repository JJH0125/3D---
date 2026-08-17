using System.Collections.Generic;
using UnityEngine;

namespace Astar3D
{
    /// <summary>
    /// Builds and owns the 3D node grid, laid out on the XZ plane (the floor).
    ///
    /// NAMED "PathGrid", NOT "Grid": Unity has a built-in UnityEngine.Grid
    /// component (used by Tilemap). Naming your class "Grid" makes the editor
    /// try to attach the built-in GridEditor and throw
    /// SerializedObjectNotCreatableException. Avoid built-in names.
    ///
    /// Walkability is sampled with Physics.CheckBox against an "unwalkable"
    /// layer mask, so any 3D collider (walls, props, level geometry) blocks a
    /// cell — no Tilemap required.
    /// </summary>
    public class PathGrid : MonoBehaviour
    {
        [Header("○ 튜닝 값 — 자유롭게 조절")]
        [Tooltip("전체 월드의 크기")]
        [SerializeField] private Vector2 gridWorldSize = new Vector2(40, 40);

        [Tooltip("셀 하나의 반경. 셀의 크기는 이 값의 2배")]
        [SerializeField] private float nodeRadius = 0.5f;

        [Tooltip("장애물로 감지하는 높이")]
        [SerializeField] private float obstacleCheckHeight = 10000f;

        [Tooltip("지정한 레이어의 물체들을 장애물로 인식")]
        [SerializeField] private LayerMask unwalkableMask;

        [Tooltip("대각선 이동 허용 여부. True이면 8방향, False면 4방향")]
        [SerializeField] private bool allowDiagonal = true;

        private Node[,] _grid;
        private float _nodeDiameter;
        private int _gridSizeX;
        private int _gridSizeY;   // count along Z

        public int MaxSize => _gridSizeX * _gridSizeY;
        public bool AllowDiagonal => allowDiagonal;

        private void Awake() => BuildGrid();

        /// <summary>
        /// 게임이 시작되면 호출되는 함수.
        /// 월드 상에서 도달 가능한 모든 지역을 Node로 그려낸다.
        /// </summary>
        public void BuildGrid()
        {
            _nodeDiameter = nodeRadius * 2;
            _gridSizeX = Mathf.RoundToInt(gridWorldSize.x / _nodeDiameter);
            _gridSizeY = Mathf.RoundToInt(gridWorldSize.y / _nodeDiameter);
            _grid = new Node[_gridSizeX, _gridSizeY];

            // Bottom-left corner of the grid on the XZ plane.
            Vector3 worldBottomLeft = transform.position
                                      + Vector3.left * gridWorldSize.x / 2
                                      + Vector3.back * gridWorldSize.y / 2;

            // 각 칸에 벽이 있는지 검사하는 데 쓰이는
            // 검사 박스의 radius 값을 담는 변수.
            // x, z는 노드의 radius 값에 0.9를 곱하여
            // 1.0으로 설정했을 때 바로 옆칸의 벽까지
            // 판정되는 일이 없도록 함.
            Vector3 boxHalf = new Vector3(nodeRadius * 0.9f,
                                        obstacleCheckHeight * 0.5f,
                                        nodeRadius * 0.9f);

            for (int x = 0; x < _gridSizeX; x++)
            {
                for (int y = 0; y < _gridSizeY; y++)
                {
                    // x, y번째 노드의 중심 좌표 (xz 평면 상에서)
                    Vector3 worldPoint = worldBottomLeft
                        + Vector3.right * (x * _nodeDiameter + nodeRadius)
                        + Vector3.forward * (y * _nodeDiameter + nodeRadius);

                    // x, y번째 노드의 중심 좌표 (장애물 높이 감지 위해 연직 위 좌표 추가))
                    Vector3 boxCenter = worldPoint + Vector3.up * (obstacleCheckHeight * 0.5f);
                    
                    // center로부터 half 반경의 공간으로 이루어진
                    // 눈에 보이지 않는 충돌 판정 측정 박스가
                    // unwalkableMask에 지정된 레이어의 오브젝트와 겹치는지 판정.
                    bool walkable = !Physics.CheckBox(
                        boxCenter, boxHalf, Quaternion.identity, unwalkableMask);

                    _grid[x, y] = new Node(walkable, worldPoint, x, y);
                }
            }
        }

        /// <summary>
        /// 실제 유니티 상의 좌표를 받아서, 그 좌표가 있는 노드의 번호를 반환하는 함수.
        /// </summary>
        public Node NodeFromWorldPoint(Vector3 worldPosition)
        {
            // 맵의 왼쪽 아래 끝 기준으로 벡터값을 변환
            Vector3 local = worldPosition - transform.position
                            + new Vector3(gridWorldSize.x / 2, 0, gridWorldSize.y / 2);

            // 맵 크기 대비 좌표의 비율(0~1 사이의 값)을 환산
            float percentX = Mathf.Clamp01(local.x / gridWorldSize.x);
            float percentY = Mathf.Clamp01(local.z / gridWorldSize.y);  // Z maps to grid Y

            int x = Mathf.Clamp(Mathf.RoundToInt((_gridSizeX - 1) * percentX), 0, _gridSizeX - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt((_gridSizeY - 1) * percentY), 0, _gridSizeY - 1);
            return _grid[x, y];
        }

        public IEnumerable<Node> GetNeighbors(Node node)
        {
            // 노드 자신과 그 주변의 8개, 총 9개의 노드 중
            // 곧장 도달할 수 있는 노드를 검사.
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    // 제자리일 경우 continue
                    if (dx == 0 && dy == 0)
                        continue;
                    // 대각선이 허용되지 않았는데 대각선일 경우도
                    // continue
                    if (!allowDiagonal && dx != 0 && dy != 0)
                        continue;

                    // 해당 노드의 번호
                    int checkX = node.GridX + dx;
                    int checkY = node.GridY + dy;
                    
                    // 경계 out 보호
                    if (checkX >= 0 && checkX < _gridSizeX && checkY >= 0 && checkY < _gridSizeY)
                    {
                        // 대각선의 노드일 경우, 
                        // A D
                        // S B (S에서 D로 가는 상황을 검사)
                        // 같은 상황일 때 A와 B 둘 중 하나라도 벽이라면,
                        // S에서 D로 대각선으로 곧장 이동할 수 없기에
                        // 그런 노드는 continue.
                        if (allowDiagonal && dx != 0 && dy != 0)
                        {
                            if (!_grid[node.GridX + dx, node.GridY].Walkable ||
                                !_grid[node.GridX, node.GridY + dy].Walkable)
                                continue;
                        }
                        yield return _grid[checkX, checkY];
                    }
                }
            }
        }

        // 적이 나를 향해 오는 경로의 노드를 담는 List
        public List<Node> DebugPath;

        // 유효한 목표 지점만 뽑도록 하는 방어 코드.
        public bool IsWalkable(Vector3 worldPos) => NodeFromWorldPoint(worldPos).Walkable;

        /// <summary>
        /// A*가 제대로 동작하는지를,
        /// Scene 화면에서 노드가 색칠되는 것을 통해
        /// 시각적으로 알 수 있게 하는 함수
        /// </summary>
        private void OnDrawGizmos()
        {
            // 월드의 경계를 흰선으로 나타냄
            Gizmos.DrawWireCube(transform.position,
                new Vector3(gridWorldSize.x, 0.1f, gridWorldSize.y));
            
            // grid가 아직 생성되지 않았다면
            // 즉 게임이 실행되지 않았다면
            // 아무것도 하지 않음
            if (_grid == null)
                return;

            foreach (Node n in _grid)
            {
                /// Walkable 하면 흰색, 그렇지 않으면 빨간색
                /// 4번째
                Gizmos.color = n.Walkable ? new Color(1, 1, 1, 0.5f)    // 투명도 0.5
                                          : new Color(1, 0, 0, 0.8f);   // 투명도 0.8
                // 큐브의 한가운데 좌표와 큐브의 full size를 받아
                // 바닥의 격자 무늬를 그림
                Gizmos.DrawCube(n.WorldPosition,
                new Vector3(_nodeDiameter * 0.9f, 0.05f, _nodeDiameter * 0.9f));
            }

            if (DebugPath != null)
            {
                Gizmos.color = Color.cyan;
                // 적이 오는 경로에 해당하는 노드들은
                // 격자무늬의 사각형 안에 더 작은 시안색의 사각형으로 표시
                foreach (Node n in DebugPath)
                    Gizmos.DrawCube(n.WorldPosition, new Vector3(
                        _nodeDiameter * 0.5f, 0.1f, _nodeDiameter * 0.5f));
            }
        }
    }
}
