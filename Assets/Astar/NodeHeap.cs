namespace Astar3D
{
    /// <summary>
    /// Binary min-heap for the A* open set. Identical to the 2D version —
    /// it only deals with Nodes and their FCost/HCost, so nothing about it
    /// changes between 2D and 3D. Ordered by FCost, tie-broken by HCost.
    /// </summary>
    public class NodeHeap
    {
        private readonly Node[] _items;
        private int _count;

        public int Count => _count;

        public NodeHeap(int maxSize) => _items = new Node[maxSize];

        public void Add(Node node)
        {
            node.HeapIndex = _count;
            _items[_count] = node;
            SortUp(node);
            _count++;
        }

        public Node RemoveFirst()
        {
            Node first = _items[0];
            _count--;
            _items[0] = _items[_count];
            _items[0].HeapIndex = 0;
            SortDown(_items[0]);
            return first;
        }

        public void UpdateItem(Node node) => SortUp(node);

        public bool Contains(Node node) =>
            node.HeapIndex < _count && _items[node.HeapIndex] == node;

        public void Clear() => _count = 0;

        private void SortUp(Node node)
        {
            int parentIndex;
            while (true)    
            {
                parentIndex = (node.HeapIndex - 1) / 2;
                Node parentNode = _items[parentIndex];
                if (Compare(node, parentNode) > 0)
                    Swap(node, parentNode);
                else
                    break;                
            }
        }

        private void SortDown(Node node)
        {
            while (true)
            {
                int childLeft = node.HeapIndex * 2 + 1;
                int childRight = node.HeapIndex * 2 + 2;
                if (childLeft < _count)
                {
                    int swapIndex = childLeft;
                    if (childRight < _count &&
                        Compare(_items[childRight], _items[childLeft]) > 0)
                        swapIndex = childRight;
                    if (Compare(_items[swapIndex], node) > 0)
                        Swap(node, _items[swapIndex]);
                    else
                        break;
                }
                else
                    break;
            }
        }

        private static int Compare(Node a, Node b)
        {
            int compare = b.FCost.CompareTo(a.FCost);
            if (compare == 0)
                compare = b.HCost.CompareTo(a.HCost);
            return compare;
        }

        private void Swap(Node a, Node b)
        {
            _items[a.HeapIndex] = b;
            _items[b.HeapIndex] = a;
            (a.HeapIndex, b.HeapIndex) = (b.HeapIndex, a.HeapIndex);
        }
    }
}
