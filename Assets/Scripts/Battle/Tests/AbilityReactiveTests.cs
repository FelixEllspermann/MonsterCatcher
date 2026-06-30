using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using MonsterCatcher.Map;

namespace MonsterCatcher.Battle.Tests
{
    public class AbilityReactiveTests
    {
        private static List<MoveData> Hit60() =>
            new List<MoveData> { TestFactory.Move("Hit", ElementType.Normal, MoveCategory.Physical, 60) };

        [Test]
        public void LastStand_SurvivesLethalHitWith1Hp()
        {
            var attackerSpecies = TestFactory.Species("Bruiser", ElementType.Normal, 60, 200, 50, 50, 50, 50);
            var defenderSpecies = TestFactory.Species("Glass", ElementType.Normal, 20, 50, 5, 50, 5, 40);

            var attacker = new Pokemon(attackerSpecies, 50, Hit60());
            var defender = new Pokemon(defenderSpecies, 50, Hit60(), new[] { "LastStand" });

            var settings = TestFactory.Settings();
            var rng = new FakeRng();

            var engine = new BattleEngine(
                new Party(BattleSide.Player, new List<Pokemon> { attacker }, 6),
                new Party(BattleSide.Enemy, new List<Pokemon> { defender }, 6),
                settings, rng);

            engine.ExecuteTurn(BattleAction.UseMove(0), BattleAction.UseMove(0));

            Assert.IsFalse(defender.IsFainted);
            Assert.AreEqual(1, defender.CurrentHp);
            Assert.IsTrue(defender.AbilityState.LastStandUsed);
        }

        [Test]
        public void LastStand_OnlyTriggersOnce()
        {
            var species = TestFactory.Species("Glass", ElementType.Normal, 20, 50, 5, 50, 5, 40);
            var p = new Pokemon(species, 50, Hit60(), new[] { "LastStand" });

            p.TakeDamage(p.MaxHp);
            Assert.AreEqual(1, p.CurrentHp);

            p.TakeDamage(p.MaxHp);
            Assert.IsFalse(AbilityApplier.TrySurviveLethal(p));
        }

        [Test]
        public void Thorns_ChipsAttacker_AndMoxie_RaisesAttackOnKo()
        {
            var attackerSpecies = TestFactory.Species("A", ElementType.Normal, 100, 80, 60, 60, 60, 60);
            var defenderSpecies = TestFactory.Species("D", ElementType.Normal, 100, 80, 60, 60, 60, 60);

            var thornsAttacker = new Pokemon(attackerSpecies, 50, Hit60());
            var thornsDefender = new Pokemon(defenderSpecies, 50, Hit60(), new[] { "Thorns" });

            var events = new List<BattleEvent>();
            int expectedThorns = System.Math.Max(1, (int)(thornsAttacker.MaxHp * 0.125f));
            AbilityApplier.OnDealtDamage(thornsAttacker, thornsDefender, events);

            Assert.AreEqual(thornsAttacker.MaxHp - expectedThorns, thornsAttacker.CurrentHp);
            Assert.IsTrue(events.OfType<StatusDamageEvent>().Any(e => e.Amount == expectedThorns));

            var moxieMon = new Pokemon(attackerSpecies, 50, Hit60(), new[] { "Moxie" });
            var koEvents = new List<BattleEvent>();
            AbilityApplier.OnKo(moxieMon, koEvents);

            Assert.AreEqual(1, moxieMon.GetStage(Stat.Attack));
            Assert.IsTrue(koEvents.OfType<StatChangedEvent>().Any(e => e.Stat == Stat.Attack && e.DeltaStages == 1));
        }
    }
}
