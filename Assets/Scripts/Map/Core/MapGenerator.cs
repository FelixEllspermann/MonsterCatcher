using System;
using System.Collections.Generic;

namespace MonsterCatcher.Map
{
    public static class MapGenerator
    {
        public const int Floors = 20;
        public const int MinPerFloor = 2;
        public const int MaxPerFloor = 5;
        public const double BranchChance = 0.4;
        public const double HealChance = 0.1;
        public const double ShopChance = 0.1;
        public const double EventChance = 0.30;

        public static MapModel Generate(int seed)
        {
            var rng = new Random(seed);
            var nodes = new List<MapNode>();
            int nextId = 0;

            int startId = nextId++;
            var start = new MapNode(startId, 0, 0.5f, NodeType.Start);
            nodes.Add(start);

            var rows = new List<List<MapNode>> { new List<MapNode> { start } };

            for (int f = 1; f <= Floors; f++)
            {
                int count = rng.Next(MinPerFloor, MaxPerFloor + 1);
                var rowNodes = new List<MapNode>();
                int eventIndex = (f >= 2 && rng.NextDouble() < EventChance) ? rng.Next(count) : -1;
                for (int i = 0; i < count; i++)
                {
                    float x = (i + 0.5f) / count;
                    var type = NodeType.Battle;
                    if (i == eventIndex)
                    {
                        type = NodeType.Event;
                    }
                    else if (f >= 2)
                    {
                        double r = rng.NextDouble();
                        if (r < HealChance) type = NodeType.Heal;
                        else if (r < HealChance + ShopChance) type = NodeType.Shop;
                    }
                    var node = new MapNode(nextId++, f, x, type);
                    nodes.Add(node);
                    rowNodes.Add(node);
                }
                rows.Add(rowNodes);
            }

            int bossId = nextId++;
            var boss = new MapNode(bossId, Floors + 1, 0.5f, NodeType.Boss);
            nodes.Add(boss);
            rows.Add(new List<MapNode> { boss });

            // START -> all of floor 1
            foreach (var n in rows[1]) start.Next.Add(n.Id);

            // floor f -> f+1 for f in 1..Floors-1
            for (int f = 1; f < Floors; f++) ConnectRows(rows[f], rows[f + 1], rng);

            // floor Floors -> BOSS
            foreach (var n in rows[Floors]) n.Next.Add(bossId);

            return new MapModel(nodes, startId, bossId, Floors + 2);
        }

        private static void ConnectRows(List<MapNode> lower, List<MapNode> upper, Random rng)
        {
            var hasIncoming = new HashSet<int>();
            foreach (var a in lower)
            {
                int n1 = NearestIndex(upper, a.X, -1);
                AddEdge(a, upper[n1], hasIncoming);
                if (rng.NextDouble() < BranchChance)
                {
                    int n2 = NearestIndex(upper, a.X, n1);
                    if (n2 >= 0) AddEdge(a, upper[n2], hasIncoming);
                }
            }
            for (int j = 0; j < upper.Count; j++)
            {
                if (hasIncoming.Contains(upper[j].Id)) continue;
                int li = NearestIndex(lower, upper[j].X, -1);
                AddEdge(lower[li], upper[j], hasIncoming);
            }
        }

        private static void AddEdge(MapNode from, MapNode to, HashSet<int> hasIncoming)
        {
            if (!from.Next.Contains(to.Id)) from.Next.Add(to.Id);
            hasIncoming.Add(to.Id);
        }

        private static int NearestIndex(List<MapNode> nodes, float x, int exclude)
        {
            int best = -1; float bestD = float.MaxValue;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (i == exclude) continue;
                float d = Math.Abs(nodes[i].X - x);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }
    }
}
