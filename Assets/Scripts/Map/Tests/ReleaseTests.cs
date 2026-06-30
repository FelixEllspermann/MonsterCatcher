using NUnit.Framework;

namespace MonsterCatcher.Map.Tests
{
    public class ReleaseTests
    {
        [Test] public void ReleaseRemovesWhenMoreThanOne()
        {
            RunState.NewRun(3);
            RunState.PlayerRoster.Add(new MonsterSave("Briarstag", 5));
            int before = RunState.PlayerRoster.Count;
            Assert.IsTrue(RunState.ReleaseMonster(1));
            Assert.AreEqual(before - 1, RunState.PlayerRoster.Count);
        }

        [Test] public void ReleaseRefusedWhenOnlyOne()
        {
            RunState.NewRun(3);
            Assert.AreEqual(1, RunState.PlayerRoster.Count);
            Assert.IsFalse(RunState.ReleaseMonster(0));
            Assert.AreEqual(1, RunState.PlayerRoster.Count);
        }
    }
}
