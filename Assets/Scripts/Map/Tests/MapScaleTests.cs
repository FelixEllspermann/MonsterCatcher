using System.Collections.Generic;
using NUnit.Framework;

namespace MonsterCatcher.Map.Tests
{
    public class MapScaleTests
    {
        // ---- map size ------------------------------------------------------------

        [Test] public void FloorsIsTwentyAndMapHasBossAndStart()
        {
            Assert.AreEqual(20, MapGenerator.Floors);
            var m = MapGenerator.Generate(3);
            Assert.AreEqual(22, m.RowCount);

            int starts = 0, bosses = 0;
            foreach (var n in m.Nodes)
            {
                if (n.Type == NodeType.Start) starts++;
                if (n.Type == NodeType.Boss) bosses++;
            }
            Assert.AreEqual(1, starts);
            Assert.AreEqual(1, bosses);
        }

        // ---- event placement -----------------------------------------------------

        [Test] public void EventNodesAppearAcrossSeeds()
        {
            bool seen = false;
            for (int s = 0; s < 80 && !seen; s++)
            {
                var map = MapGenerator.Generate(s);
                foreach (var n in map.Nodes) if (n.Type == NodeType.Event) { seen = true; break; }
            }
            Assert.IsTrue(seen, "event nodes should appear across seeds");
        }

        [Test] public void AtMostOneEventPerRow()
        {
            for (int s = 0; s < 40; s++)
            {
                var map = MapGenerator.Generate(s);
                var perRow = new Dictionary<int, int>();
                foreach (var n in map.Nodes)
                    if (n.Type == NodeType.Event)
                        perRow[n.Row] = (perRow.TryGetValue(n.Row, out var c) ? c : 0) + 1;
                foreach (var kv in perRow)
                    Assert.LessOrEqual(kv.Value, 1, "more than one Event on row " + kv.Key + " seed " + s);
            }
        }

        // ---- enemy leveling ------------------------------------------------------

        [Test] public void BossLevelIsTwentyTwo()
        {
            Assert.AreEqual(22, RunState.BossLevel);
        }

        [Test] public void EnemyLevelForBossAndBattleAndTier()
        {
            Assert.AreEqual(22, RunState.EnemyLevelFor(NodeType.Boss, 5, 1));
            Assert.AreEqual(20, RunState.EnemyLevelFor(NodeType.Battle, 20, 1));
            Assert.Less(RunState.EnemyLevelFor(NodeType.Battle, 20, 1),
                        RunState.EnemyLevelFor(NodeType.Boss, 5, 1), "a row-20 battle is easier than the boss");
            Assert.Greater(RunState.EnemyLevelFor(NodeType.Battle, 1, 2),
                           RunState.EnemyLevelFor(NodeType.Boss, 0, 1), "tier 2 floor 1 outscales the tier 1 boss");
        }

        // ---- stage bands ---------------------------------------------------------

        [Test] public void StageForRowBands()
        {
            Assert.AreEqual(0, RunState.StageForRow(NodeType.Battle, 6));
            Assert.AreEqual(1, RunState.StageForRow(NodeType.Battle, 7));
            Assert.AreEqual(1, RunState.StageForRow(NodeType.Battle, 13));
            Assert.AreEqual(2, RunState.StageForRow(NodeType.Battle, 14));
            Assert.AreEqual(2, RunState.StageForRow(NodeType.Boss, 1));
        }
    }
}
