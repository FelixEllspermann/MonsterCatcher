using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using MonsterCatcher.Map;

namespace MonsterCatcher.Battle.Tests
{
    public class AbilitySpecialTests
    {
        private static Pokemon Mon(string ability, int hp = 60)
        {
            var species = TestFactory.Species("A", ElementType.Normal, hp, 50, 50, 50, 50, 50);
            var moves = new List<MoveData> { TestFactory.Move("Hit", ElementType.Normal, MoveCategory.Physical, 60) };
            return new Pokemon(species, 50, moves, new[] { ability });
        }

        private static Pokemon Plain(int hp = 60)
        {
            var species = TestFactory.Species("B", ElementType.Normal, hp, 50, 50, 50, 50, 50);
            return new Pokemon(species, 50, TestFactory.OneMove(), new string[0]);
        }

        [Test]
        public void Titan_DoublesStab_PlainKeepsNormalStab()
        {
            Assert.AreEqual(2.0, AbilityApplier.StabFactor(Mon("Titan")));
            Assert.AreEqual(1.5, AbilityApplier.StabFactor(Plain()));
        }

        [Test]
        public void TintedLens_RaisesResistedHitToNeutral_OnlyWhenResisted()
        {
            var tl = Mon("TintedLens");
            Assert.AreEqual(1.0, AbilityApplier.AdjustEffectiveness(tl, 0.5));
            Assert.AreEqual(2.0, AbilityApplier.AdjustEffectiveness(tl, 2.0));
            Assert.AreEqual(0.0, AbilityApplier.AdjustEffectiveness(tl, 0.0));
            Assert.AreEqual(0.5, AbilityApplier.AdjustEffectiveness(Plain(), 0.5));
        }

        [Test]
        public void Executioner_TriggersAtOrBelowThreshold()
        {
            var attacker = Mon("Executioner");
            var foe = Plain(hp: 100);
            foe.SetCurrentHp((int)(foe.MaxHp * 0.20f));
            Assert.IsTrue(AbilityApplier.ExecutesFoe(attacker, foe));

            foe.SetCurrentHp(foe.MaxHp);
            Assert.IsFalse(AbilityApplier.ExecutesFoe(attacker, foe));

            foe.SetCurrentHp(1);
            Assert.IsFalse(AbilityApplier.ExecutesFoe(Plain(), foe));
        }
    }
}
