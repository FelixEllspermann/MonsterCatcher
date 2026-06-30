using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using MonsterCatcher.Map;

namespace MonsterCatcher.Battle.Tests
{
    public class AbilityEntryTests
    {
        private static Pokemon Mon(int hp, int atk, int def, int spa, int spd, int spe, string ability)
        {
            var species = TestFactory.Species("A", ElementType.Normal, hp, atk, def, spa, spd, spe);
            var moves = TestFactory.OneMove();
            return new Pokemon(species, 50, moves, ability != null ? new[] { ability } : null);
        }

        [Test]
        public void Intimidate_LowersFoeAttackOnEntry()
        {
            var entering = Mon(100, 80, 80, 80, 80, 80, "Intimidate");
            var foe = Mon(100, 80, 80, 80, 80, 80, null);

            var events = new List<BattleEvent>();
            AbilityApplier.OnEntry(entering, foe, events);

            Assert.AreEqual(-1, foe.GetStage(Stat.Attack));
            Assert.AreEqual(0, entering.GetStage(Stat.Attack));
            var ev = events.OfType<StatChangedEvent>().Single();
            Assert.AreSame(foe, ev.Target);
            Assert.AreEqual(Stat.Attack, ev.Stat);
            Assert.AreEqual(-1, ev.DeltaStages);
        }

        [Test]
        public void BattleCry_RaisesUserAttackOnEntry()
        {
            var entering = Mon(100, 80, 80, 80, 80, 80, "BattleCry");
            var foe = Mon(100, 80, 80, 80, 80, 80, null);

            var events = new List<BattleEvent>();
            AbilityApplier.OnEntry(entering, foe, events);

            Assert.AreEqual(1, entering.GetStage(Stat.Attack));
            Assert.AreEqual(0, foe.GetStage(Stat.Attack));
            var ev = events.OfType<StatChangedEvent>().Single();
            Assert.AreSame(entering, ev.Target);
            Assert.AreEqual(Stat.Attack, ev.Stat);
            Assert.AreEqual(1, ev.DeltaStages);
        }

        [Test]
        public void Download_RaisesHigherOffenseStat()
        {
            var atkBiased = Mon(100, 120, 80, 60, 80, 80, "Download");
            AbilityApplier.OnEntry(atkBiased, Mon(100, 80, 80, 80, 80, 80, null), null);
            Assert.AreEqual(1, atkBiased.GetStage(Stat.Attack));
            Assert.AreEqual(0, atkBiased.GetStage(Stat.SpAttack));

            var spaBiased = Mon(100, 60, 80, 120, 80, 80, "Download");
            AbilityApplier.OnEntry(spaBiased, Mon(100, 80, 80, 80, 80, 80, null), null);
            Assert.AreEqual(1, spaBiased.GetStage(Stat.SpAttack));
            Assert.AreEqual(0, spaBiased.GetStage(Stat.Attack));
        }
    }
}
