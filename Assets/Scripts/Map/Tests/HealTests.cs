using NUnit.Framework;

namespace MonsterCatcher.Map.Tests
{
    public class HealTests
    {
        [Test]
        public void VisitHealRestoresHpAndAdvances()
        {
            RunState.NewRun(3);
            RunState.PlayerRoster.Add(new MonsterSave("Briarstag", 1));   // ensure a 2nd mon to revive
            RunState.PlayerRoster[0].CurrentHp = 1;   // damaged
            RunState.PlayerRoster[1].CurrentHp = 0;   // fainted
            int node = RunState.Available()[0];

            Assert.IsTrue(RunState.VisitHeal(node));
            Assert.AreEqual(int.MaxValue, RunState.PlayerRoster[0].CurrentHp);
            Assert.AreEqual(int.MaxValue, RunState.PlayerRoster[1].CurrentHp); // revived
            Assert.AreEqual(node, RunState.CurrentNodeId);
            Assert.IsTrue(RunState.Cleared.Contains(node));
        }

        [Test]
        public void VisitHealRejectsUnavailableNode()
        {
            RunState.NewRun(3);
            Assert.IsFalse(RunState.VisitHeal(RunState.Map.BossId));
        }
    }
}
