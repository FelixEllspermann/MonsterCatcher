using System.Collections.Generic;
using NUnit.Framework;

namespace MonsterCatcher.Map.Tests
{
    public class LevelingTests
    {
        [SetUp] public void Reset() => RunState.NewRun(3);

        [Test] public void NewRunRoster()
        {
            Assert.AreEqual(2, RunState.PlayerRoster.Count);
            Assert.AreEqual("Mossprig", RunState.PlayerRoster[0].SpeciesName);
            Assert.AreEqual("Briarstag", RunState.PlayerRoster[1].SpeciesName);
            Assert.AreEqual(1, RunState.PlayerRoster[0].Level);
            Assert.AreEqual(int.MaxValue, RunState.PlayerRoster[0].CurrentHp);
        }

        [Test] public void ParticipantLevelsBenchGetsShare()
        {
            RunState.ApplyWin(new[] { true, false });
            Assert.AreEqual(2, RunState.PlayerRoster[0].Level);          // participant +1
            Assert.AreEqual(1, RunState.PlayerRoster[1].Level);          // bench still 1
            Assert.AreEqual(0.5f, RunState.PlayerRoster[1].LevelProgress, 1e-5);

            RunState.ApplyWin(new[] { true, false });
            Assert.AreEqual(3, RunState.PlayerRoster[0].Level);
            Assert.AreEqual(2, RunState.PlayerRoster[1].Level);          // bench leveled after 2nd win
            Assert.AreEqual(0f, RunState.PlayerRoster[1].LevelProgress, 1e-5);
        }

        [Test] public void EnemyLevelIsRowAndBoss()
        {
            var floor1 = new List<MapNode>(RunState.Map.NodesInRow(1));
            RunState.PendingNodeId = floor1[0].Id;
            Assert.AreEqual(1, RunState.PendingEnemyLevel());

            RunState.PendingNodeId = RunState.Map.BossId;
            Assert.AreEqual(RunState.BossLevel, RunState.PendingEnemyLevel());
        }

        [Test] public void EnemySpeciesByFloor()
        {
            var f2 = new List<MapNode>(RunState.Map.NodesInRow(2));
            RunState.PendingNodeId = f2[0].Id;
            Assert.AreEqual("Mossprig", RunState.PendingEnemySpecies());

            var f5 = new List<MapNode>(RunState.Map.NodesInRow(5));
            RunState.PendingNodeId = f5[0].Id;
            Assert.AreEqual("Briarstag", RunState.PendingEnemySpecies());

            var f8 = new List<MapNode>(RunState.Map.NodesInRow(8));
            RunState.PendingNodeId = f8[0].Id;
            Assert.AreEqual("Elderthorn", RunState.PendingEnemySpecies());

            RunState.PendingNodeId = RunState.Map.BossId;
            Assert.AreEqual("Elderthorn", RunState.PendingEnemySpecies());
        }
    }
}
