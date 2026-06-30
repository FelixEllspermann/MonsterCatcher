using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using MonsterCatcher.Map;

namespace MonsterCatcher.Battle.Tests
{
    public class AbilityTempoTests
    {
        private static Pokemon Make(string ability, List<MoveData> moves)
        {
            var species = TestFactory.Species("A", ElementType.Normal, 100, 80, 80, 80, 80, 80);
            return new Pokemon(species, 50, moves, new[] { ability });
        }

        [Test]
        public void Trickster_GivesPriorityToStatusMoves_NotToAttacks()
        {
            var status = TestFactory.Move("Boost", ElementType.Normal, MoveCategory.Status, 0);
            var attack = TestFactory.Move("Hit", ElementType.Normal, MoveCategory.Physical, 60);
            var mon = Make("Trickster", new List<MoveData> { status, attack });

            Assert.AreEqual(1, AbilityApplier.PriorityBonus(mon, status));
            Assert.AreEqual(0, AbilityApplier.PriorityBonus(mon, attack));
        }

        [Test]
        public void FirstTurnAbilities_OnlyApplyOnFirstTurnOut()
        {
            var move = TestFactory.Move("Hit", ElementType.Normal, MoveCategory.Physical, 60);
            var phantom = Make("PhantomStep", new List<MoveData> { move });
            var warp = Make("TimeWarp", new List<MoveData> { move });

            Assert.AreEqual(1, AbilityApplier.PriorityBonus(phantom, move));
            Assert.IsTrue(AbilityApplier.ForcesFirst(warp));

            phantom.AbilityState.TurnsOut = 1;
            warp.AbilityState.TurnsOut = 1;
            Assert.AreEqual(0, AbilityApplier.PriorityBonus(phantom, move));
            Assert.IsFalse(AbilityApplier.ForcesFirst(warp));
        }

        [Test]
        public void Reversal_ForcesLast_And_Adrenaline_SpeedsUpWhenLowHp()
        {
            var move = TestFactory.Move("Hit", ElementType.Normal, MoveCategory.Physical, 60);
            var reversal = Make("Reversal", new List<MoveData> { move });
            Assert.IsTrue(AbilityApplier.ForcesLast(reversal));

            var adrenaline = Make("Adrenaline", new List<MoveData> { move });
            Assert.AreEqual(1.0, AbilityApplier.SpeedFactor(adrenaline), 1e-9);
            adrenaline.SetCurrentHp(adrenaline.MaxHp / 3);
            Assert.AreEqual(1.5, AbilityApplier.SpeedFactor(adrenaline), 1e-9);
        }
    }
}
