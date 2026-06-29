using System.Collections.Generic;
using NUnit.Framework;

namespace MonsterCatcher.Map.Tests
{
    public class TierTests
    {
        [Test]
        public void NextTierAdvancesAndKeepsRoster()
        {
            RunState.NewRun(3);
            RunState.PlayerRoster[0].Level = 8;
            var rosterRef = RunState.PlayerRoster;

            RunState.NextTier(7);

            Assert.AreEqual(2, RunState.Tier);
            Assert.AreSame(rosterRef, RunState.PlayerRoster);
            Assert.AreEqual(8, RunState.PlayerRoster[0].Level);
            Assert.AreEqual(RunState.Map.StartId, RunState.CurrentNodeId);
            Assert.IsFalse(RunState.RunWon);
        }

        [Test]
        public void EnemyLevelScalesWithTier()
        {
            RunState.NewRun(3);
            var f1 = new List<MapNode>(RunState.Map.NodesInRow(1));
            RunState.PendingNodeId = f1[0].Id;
            int t1 = RunState.PendingEnemyLevel();

            RunState.NextTier(3); // same seed -> same structure
            var f1b = new List<MapNode>(RunState.Map.NodesInRow(1));
            RunState.PendingNodeId = f1b[0].Id;
            int t2 = RunState.PendingEnemyLevel();

            Assert.AreEqual(t1 + RunState.BossLevel, t2);
        }
    }
}
