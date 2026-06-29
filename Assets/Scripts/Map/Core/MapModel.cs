using System.Collections.Generic;

namespace MonsterCatcher.Map
{
    public sealed class MapModel
    {
        private readonly List<MapNode> _nodes;
        private readonly Dictionary<int, MapNode> _byId = new Dictionary<int, MapNode>();

        public int StartId { get; }
        public int BossId { get; }
        public int RowCount { get; }

        public MapModel(List<MapNode> nodes, int startId, int bossId, int rowCount)
        {
            _nodes = nodes;
            foreach (var n in nodes) _byId[n.Id] = n;
            StartId = startId; BossId = bossId; RowCount = rowCount;
        }

        public IReadOnlyList<MapNode> Nodes => _nodes;
        public MapNode Get(int id) => _byId[id];

        public IEnumerable<MapNode> NodesInRow(int row)
        {
            foreach (var n in _nodes) if (n.Row == row) yield return n;
        }
    }
}
