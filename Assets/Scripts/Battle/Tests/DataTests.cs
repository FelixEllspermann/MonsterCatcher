using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class DataTests
    {
        [Test] public void SettingsDefaults()
        {
            var s = TestFactory.Settings();
            Assert.AreEqual(6, s.MaxPartySize);
            Assert.AreEqual(0.125, s.PoisonFraction, 1e-6);
        }

        [Test] public void SpeciesBaseStatLookup()
        {
            var s = TestFactory.Species("Testmon", ElementType.Fire, 50, 60, 40, 70, 50, 80);
            Assert.AreEqual(70, s.BaseStat(Stat.SpAttack));
        }
    }
}
