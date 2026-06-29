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
    }
}
