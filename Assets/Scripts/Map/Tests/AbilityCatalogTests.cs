using System.Collections.Generic;
using NUnit.Framework;

namespace MonsterCatcher.Map.Tests
{
    public class AbilityCatalogTests
    {
        [Test] public void RollReturnsACatalogId()
        {
            Assert.IsNotNull(AbilityCatalog.ById(AbilityCatalog.RollId(123)));
        }

        [Test] public void RollIsDeterministicPerSeed()
        {
            Assert.AreEqual(AbilityCatalog.RollId(7), AbilityCatalog.RollId(7));
        }

        [Test] public void NewRunGivesStarterExactlyOneAbility()
        {
            RunState.NewRun(3);
            Assert.AreEqual(1, RunState.PlayerRoster[0].AbilityIds.Count);
            Assert.IsNotNull(AbilityCatalog.ById(RunState.PlayerRoster[0].AbilityIds[0]));
        }

        [Test] public void IdsAreUnique()
        {
            var seen = new HashSet<string>();
            foreach (var a in AbilityCatalog.All) Assert.IsTrue(seen.Add(a.Id), "duplicate id " + a.Id);
        }

        [Test] public void CatalogHas110With70Buffs40Defining()
        {
            Assert.AreEqual(110, AbilityCatalog.All.Count);
            int buffs = 0, defining = 0;
            foreach (var a in AbilityCatalog.All)
                if (a.Category == AbilityCategory.Buff) buffs++; else defining++;
            Assert.AreEqual(70, buffs, "buffs");
            Assert.AreEqual(40, defining, "defining");
        }
    }
}
