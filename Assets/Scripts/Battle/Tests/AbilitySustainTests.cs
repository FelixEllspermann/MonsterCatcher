using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using MonsterCatcher.Map;

namespace MonsterCatcher.Battle.Tests
{
    public class AbilitySustainTests
    {
        private static Pokemon Make(string ability)
        {
            var species = TestFactory.Species("A", ElementType.Normal, 100, 80, 70, 80, 70, 75);
            var moves = TestFactory.OneMove();
            return ability == null
                ? new Pokemon(species, 50, moves)
                : new Pokemon(species, 50, moves, new[] { ability });
        }

        [Test]
        public void Regrowth_HealsFractionOfMaxHpPerTurn()
        {
            var p = Make("Regrowth");
            int expected = (int)(p.MaxHp * .0625f);
            Assert.AreEqual(expected, AbilityApplier.EndOfTurnHeal(p));
            Assert.AreEqual(0, AbilityApplier.EndOfTurnHeal(Make(null)));
        }

        [Test]
        public void Siphon_DrainsFractionOfDamageDealt()
        {
            var p = Make("Siphon");
            Assert.AreEqual(15, AbilityApplier.DrainAmount(p, 100));
            Assert.AreEqual(0, AbilityApplier.DrainAmount(Make(null), 100));
        }

        [Test]
        public void Reckless_IsRecoilImmuneAndBoostsRecoilMoves()
        {
            var p = Make("Reckless");
            Assert.IsTrue(AbilityApplier.RecoilImmune(p));
            Assert.AreEqual(1.20f, AbilityApplier.RecoilBonus(p), 1e-4f);

            var plain = Make(null);
            Assert.IsFalse(AbilityApplier.RecoilImmune(plain));
            Assert.AreEqual(1f, AbilityApplier.RecoilBonus(plain), 1e-4f);
        }

        [Test]
        public void SecondWind_HealsOnceWhenBelowHalf()
        {
            var p = Make("SecondWind");
            p.SetCurrentHp(p.MaxHp);
            Assert.AreEqual(0, AbilityApplier.OneTimeHeal(p));
            Assert.IsFalse(p.AbilityState.SecondWindUsed);

            p.SetCurrentHp(p.MaxHp / 2 - 1);
            int expected = (int)(p.MaxHp * .10f);
            Assert.AreEqual(expected, AbilityApplier.OneTimeHeal(p));
            Assert.IsTrue(p.AbilityState.SecondWindUsed);

            Assert.AreEqual(0, AbilityApplier.OneTimeHeal(p));
        }
    }
}
