using System.Collections.Generic;
using NUnit.Framework;

namespace MonsterCatcher.Map.Tests
{
    public class MapModelTests
    {
        [Test]
        public void LookupAndRowFilter()
        {
            var nodes = new List<MapNode>
            {
                new MapNode(0, 0, 0.5f, NodeType.Start),
                new MapNode(1, 1, 0.25f, NodeType.Battle),
                new MapNode(2, 1, 0.75f, NodeType.Battle),
            };
            var m = new MapModel(nodes, 0, 2, 3);
            Assert.AreEqual(NodeType.Start, m.Get(0).Type);
            Assert.AreEqual(2, new List<MapNode>(m.NodesInRow(1)).Count);
        }
    }
}
