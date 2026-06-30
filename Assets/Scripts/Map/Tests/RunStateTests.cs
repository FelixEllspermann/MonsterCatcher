using System.Collections.Generic;
using NUnit.Framework;

namespace MonsterCatcher.Map.Tests
{
    public class RunStateTests
    {
        [SetUp] public void Reset() => RunState.NewRun(3);

        [Test] public void NewRunStartsAtStart()
        {
            Assert.AreEqual(RunState.Map.StartId, RunState.CurrentNodeId);
            Assert.AreEqual(NodeStatus.Current, RunState.StatusOf(RunState.Map.StartId));
            Assert.IsTrue(RunState.InRun);
        }

        [Test] public void AvailableIsFloorOne()
        {
            var avail = new List<int>(RunState.Available());
            CollectionAssert.AreEquivalent(RunState.Map.Get(RunState.Map.StartId).Next, avail);
            foreach (var id in avail) Assert.AreEqual(NodeStatus.Available, RunState.StatusOf(id));
        }

        [Test] public void SelectOnlyAvailable()
        {
            int avail = RunState.Available()[0];
            Assert.IsTrue(RunState.CanSelect(avail));
            Assert.IsFalse(RunState.CanSelect(RunState.Map.BossId));
        }

        [Test] public void WinAdvancesCurrent()
        {
            int node = RunState.Available()[0];
            RunState.Select(node);
            RunState.ReportBattleResult(true);
            Assert.AreEqual(node, RunState.CurrentNodeId);
            Assert.AreEqual(NodeStatus.Current, RunState.StatusOf(node));
        }

        [Test] public void LossSetsRunLost()
        {
            int node = RunState.Available()[0];
            RunState.Select(node);
            RunState.ReportBattleResult(false);
            Assert.IsTrue(RunState.RunLost);
        }

        [Test] public void BeatingBossWinsRun()
        {
            int lastFloor = -1;
            foreach (var n in RunState.Map.NodesInRow(MapGenerator.Floors))
                if (n.Next.Contains(RunState.Map.BossId)) { lastFloor = n.Id; break; }
            Assert.AreNotEqual(-1, lastFloor);
            RunState.CurrentNodeId = lastFloor;
            RunState.Cleared.Add(lastFloor);
            RunState.Select(RunState.Map.BossId);
            RunState.ReportBattleResult(true);
            Assert.IsTrue(RunState.RunWon);
        }
    }
}
