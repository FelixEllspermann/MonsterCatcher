using System.Collections.Generic;

namespace MonsterCatcher.Map
{
    public sealed class MapNode
    {
        public int Id;
        public int Row;
        public float X;            // normalized [0..1] horizontal position
        public NodeType Type;
        public readonly List<int> Next = new List<int>();

        public MapNode(int id, int row, float x, NodeType type)
        {
            Id = id; Row = row; X = x; Type = type;
        }
    }
}
