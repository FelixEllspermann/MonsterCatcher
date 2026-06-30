using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using MonsterCatcher.Map;

namespace MonsterCatcher.Battle.Tests
{
    public class AbilityCritTests
    {
        private static SpeciesData Sp() =>
            TestFactory.Species("A", ElementType.Normal, 100, 80, 60, 60, 60, 60);

        private static Pokemon Mon(string ability) =>
            new Pokemon(Sp(), 50, TestFactory.OneMove(),
                ability == null ? new string[0] : new[] { ability });

        [Test] public void CritChanceScalesAndClamps()
        {
            var plain = Mon(null);
            var keen = Mon("Keen");
            var lucky = Mon("LuckyStrike");

            Assert.AreEqual(0.0625, AbilityApplier.CritChance(plain, 0.0625), 1e-9);
            Assert.AreEqual(0.1875, AbilityApplier.CritChance(keen, 0.0625), 1e-9);
            Assert.AreEqual(0.125, AbilityApplier.CritChance(lucky, 0.0625), 1e-9);
            Assert.AreEqual(1.0, AbilityApplier.CritChance(keen, 0.5), 1e-9);
        }

        [Test] public void AvengerForcesCritOnlyAtLowHp()
        {
            var avenger = Mon("Avenger");
            var plain = Mon("Avenger");

            avenger.SetCurrentHp(avenger.MaxHp / 4);
            plain.SetCurrentHp(plain.MaxHp);

            Assert.IsTrue(AbilityApplier.ForceCrit(avenger));
            Assert.IsFalse(AbilityApplier.ForceCrit(plain));
            Assert.IsFalse(AbilityApplier.ForceCrit(Mon(null)));
        }

        [Test] public void CritImmuneDamageAccuracyAndNeverMiss()
        {
            Assert.IsTrue(AbilityApplier.CritImmune(Mon("Unbreakable")));
            Assert.IsFalse(AbilityApplier.CritImmune(Mon(null)));

            Assert.AreEqual(1.5, AbilityApplier.CritDamageMult(Mon("Sniper")), 1e-9);
            Assert.AreEqual(1.0, AbilityApplier.CritDamageMult(Mon(null)), 1e-9);

            Assert.AreEqual(1.20, AbilityApplier.AccuracyFactor(Mon("HawkEye")), 1e-6);
            Assert.AreEqual(1.10, AbilityApplier.AccuracyFactor(Mon("Nimble")), 1e-6);
            Assert.AreEqual(1.0, AbilityApplier.AccuracyFactor(Mon(null)), 1e-6);

            Assert.IsTrue(AbilityApplier.NeverMisses(Mon("Deadeye")));
            Assert.IsFalse(AbilityApplier.NeverMisses(Mon("HawkEye")));

            Assert.AreEqual(1.10f, AbilityApplier.StatMult(Mon("Nimble"), Stat.Speed), 1e-6f);
        }
    }
}
