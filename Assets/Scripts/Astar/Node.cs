using UnityEngine;

namespace Astar3D
{
    public class Node
    {
        public readonly bool Walkable;
        public readonly Vector3 WorldPosition;   // 3D — on the floor (XZ), fixed Y
        public readonly int GridX;  // 가로로 몇 번째 노드
        public readonly int GridY;  // 세로로 몇 번째 노드

        public int GCost;
        public int HCost;
        public Node Parent;
        public int HeapIndex;

        public int FCost => GCost + HCost;

        public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY)
        {
            Walkable = walkable;
            WorldPosition = worldPosition;
            GridX = gridX;
            GridY = gridY;
        }
    }
}
