using NUnit.Framework;
using UnityEngine;

namespace MonsterCatcher.Battle.Tests
{
    public class MoveLibraryTests
    {
        private static MoveData Load(string name) => Resources.Load<MoveData>("Moves/" + name);

        [Test]
        public void LibraryHasAtLeast76Moves()
        {
            var all = Resources.LoadAll<MoveData>("Moves");
            Assert.GreaterOrEqual(all.Length, 76,
                "Expected the move library (>=76) under Resources/Moves.");
        }

        [Test]
        public void StatusMovesInflictTheirStatusReliably()
        {
            AssertStatusMove("VenomSpray", StatusCondition.Poison);
            AssertStatusMove("SearingHeat", StatusCondition.Burn);
            AssertStatusMove("StaticShock", StatusCondition.Paralysis);
            AssertStatusMove("SleepSpore", StatusCondition.Sleep);
        }

        private static void AssertStatusMove(string name, StatusCondition expected)
        {
            var m = Load(name);
            Assert.IsNotNull(m, name + " should exist.");
            Assert.AreEqual(MoveCategory.Status, m.Category, name + " is a status move.");
            Assert.AreEqual(expected, m.InflictsStatus, name + " inflicts " + expected);
            Assert.AreEqual(100, m.StatusChance, name + " inflicts at 100%.");
        }

        [Test]
        public void RepresentativeEffectsAreWired()
        {
            Assert.AreEqual(33, Load("FlareBlitz").RecoilPercent, "Flare Blitz recoils.");
            Assert.AreEqual(50, Load("MegaDrain").DrainPercent, "Mega Drain drains.");
            Assert.IsTrue(Load("LeafBlade").HighCrit, "Leaf Blade has high crit.");
            Assert.AreEqual(1, Load("AquaJet").Priority, "Aqua Jet has +1 priority.");
            Assert.IsTrue(Load("SolarRay").ChargesUp, "Solar Ray charges up.");
        }
    }
}
