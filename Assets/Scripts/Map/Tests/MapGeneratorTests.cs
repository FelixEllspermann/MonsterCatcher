using System.Collections.Generic;
using NUnit.Framework;

namespace MonsterCatcher.Map.Tests
{
    public class MapGeneratorTests
    {
        private static Dictionary<int, List<int>> Incoming(MapModel m)
        {
            var inc = new Dictionary<int, List<int>>();
            foreach (var n in m.Nodes) inc[n.Id] = new List<int>();
            foreach (var n in m.Nodes)
                foreach (var t in n.Next) inc[t].Add(n.Id);
            return inc;
        }

        private static HashSet<int> Reachable(MapModel m, int from, bool forward)
        {
            var adj = new Dictionary<int, List<int>>();
            foreach (var n in m.Nodes) adj[n.Id] = new List<int>();
            foreach (var n in m.Nodes)
                foreach (var t in n.Next)
                {
                    if (forward) adj[n.Id].Add(t);
                    else adj[t].Add(n.Id);
                }
            var seen = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(from); seen.Add(from);
            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                foreach (var nx in adj[cur]) if (seen.Add(nx)) stack.Push(nx);
            }
            return seen;
        }

        [Test]
        public void InvariantsHoldAcrossSeeds()
        {
            for (int seed = 0; seed <= 50; seed++)
            {
                var m = MapGenerator.Generate(seed);
                Assert.AreEqual(10, m.RowCount, "seed " + seed);

                int starts = 0, bosses = 0;
                foreach (var n in m.Nodes)
                {
                    if (n.Type == NodeType.Start) starts++;
                    if (n.Type == NodeType.Boss) bosses++;
                }
                Assert.AreEqual(1, starts, "seed " + seed);
                Assert.AreEqual(1, bosses, "seed " + seed);
                Assert.AreEqual(0, m.Get(m.StartId).Row);
                Assert.AreEqual(9, m.Get(m.BossId).Row);

                for (int f = 1; f <= 8; f++)
                {
                    int count = new List<MapNode>(m.NodesInRow(f)).Count;
                    Assert.IsTrue(count >= 2 && count <= 5, "floor " + f + " seed " + seed + " count " + count);
                }

                var inc = Incoming(m);
                foreach (var n in m.Nodes)
                {
                    if (n.Type != NodeType.Boss) Assert.IsTrue(n.Next.Count >= 1, "no out: " + n.Id + " seed " + seed);
                    if (n.Type != NodeType.Start) Assert.IsTrue(inc[n.Id].Count >= 1, "no in: " + n.Id + " seed " + seed);
                    foreach (var t in n.Next)
                        Assert.AreEqual(n.Row + 1, m.Get(t).Row, "non-adjacent edge seed " + seed);
                }

                var fromStart = Reachable(m, m.StartId, true);
                var toBoss = Reachable(m, m.BossId, false);
                foreach (var n in m.Nodes)
                {
                    Assert.IsTrue(fromStart.Contains(n.Id), "unreachable " + n.Id + " seed " + seed);
                    Assert.IsTrue(toBoss.Contains(n.Id), "cannot reach boss " + n.Id + " seed " + seed);
                }
            }
        }

        [Test]
        public void DeterministicForSeed()
        {
            var a = MapGenerator.Generate(7);
            var b = MapGenerator.Generate(7);
            Assert.AreEqual(a.Nodes.Count, b.Nodes.Count);
            for (int i = 0; i < a.Nodes.Count; i++)
            {
                Assert.AreEqual(a.Nodes[i].Row, b.Nodes[i].Row);
                Assert.AreEqual(a.Nodes[i].Next.Count, b.Nodes[i].Next.Count);
            }
        }
    }
}
