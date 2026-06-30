using System.Collections.Generic;
using NUnit.Framework;

namespace MonsterCatcher.Map.Tests
{
    public class BossTests
    {
        [Test] public void BossPartyGrowsWithTierUpToSix()
        {
            RunState.NewRun(3);                              // tier 1
            Assert.AreEqual(3, RunState.BossPartySize());
            RunState.NextTier(1);                            // tier 2
            Assert.AreEqual(4, RunState.BossPartySize());
            RunState.NextTier(1); RunState.NextTier(1);      // tier 4
            Assert.AreEqual(6, RunState.BossPartySize());
            RunState.NextTier(1);                            // tier 5
            Assert.AreEqual(6, RunState.BossPartySize());    // capped at 6
        }

        [Test] public void BossEnemiesAreFinalStages()
        {
            var finals = new HashSet<string> { "Elderthorn", "Vulcarion", "Tempestag", "Eclipseon" };
            for (int i = 0; i < 6; i++)
                Assert.IsTrue(finals.Contains(RunState.BossEnemySpecies(i)), "index " + i);
        }
    }
}
