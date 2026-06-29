using System.Collections.Generic;
using NUnit.Framework;

namespace MonsterCatcher.Map.Tests
{
    public class LevelingTests
    {
        [SetUp] public void Reset() => RunState.NewRun(3);

        [Test] public void NewRunRoster()
        {
            Assert.AreEqual(1, RunState.PlayerRoster.Count);   // one random starter
            var name = RunState.PlayerRoster[0].SpeciesName;
            Assert.That(name, Is.EqualTo("Mossprig").Or.EqualTo("Cindrop"));
            Assert.AreEqual(1, RunState.PlayerRoster[0].Level);
            Assert.AreEqual(int.MaxValue, RunState.PlayerRoster[0].CurrentHp);
        }

        [Test] public void StarterIsRandomFirstStage()
        {
            Assert.AreEqual("Mossprig", RunState.StarterFor(0));
            Assert.AreEqual("Cindrop", RunState.StarterFor(1));
            var seen = new HashSet<string>();
            for (int s = 0; s < 8; s++) seen.Add(RunState.StarterFor(s));
            Assert.AreEqual(2, seen.Count);   // both first-stages occur
        }

        [Test] public void ParticipantLevelsBenchGetsShare()
        {
            RunState.PlayerRoster.Add(new MonsterSave("Briarstag", 1));   // give it a bench mon
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

        [Test] public void EnemyStageByFloor()
        {
            Assert.AreEqual(0, RunState.StageForRow(NodeType.Battle, 2));
            Assert.AreEqual(1, RunState.StageForRow(NodeType.Battle, 5));
            Assert.AreEqual(2, RunState.StageForRow(NodeType.Battle, 8));
            Assert.AreEqual(2, RunState.StageForRow(NodeType.Boss, 1));
        }

        [Test] public void EnemyPoolBothLinesSpawn()
        {
            var early = new HashSet<string>();
            var boss = new HashSet<string>();
            for (int id = 0; id < 40; id++)
            {
                early.Add(RunState.EnemySpeciesFor(NodeType.Battle, 2, id, 1));
                boss.Add(RunState.EnemySpeciesFor(NodeType.Boss, 8, id, 1));
            }
            Assert.IsTrue(early.Contains("Mossprig") && early.Contains("Cindrop"));   // both first stages
            Assert.IsTrue(boss.Contains("Elderthorn") && boss.Contains("Vulcarion")); // both finals
        }
    }
}
