using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class MovesTests
    {
        private static Party P(BattleSide side, params Pokemon[] mons) =>
            new Party(side, mons.ToList(), 6);

        [Test]
        public void ChargeTakesTwoTurns()
        {
            var settings = TestFactory.Settings();
            var charge = TestFactory.Move("Charge", ElementType.Grass, MoveCategory.Special, 120);
            charge.ChargesUp = true;
            var tackle = TestFactory.Move("Tackle", ElementType.Normal, MoveCategory.Physical, 40);

            var atk = TestFactory.Mon(TestFactory.Species("A", ElementType.Grass, 100, 60, 60, 130, 60, 120), charge, tackle);
            var def = TestFactory.Mon(TestFactory.Species("D", ElementType.Normal, 250, 60, 250, 60, 250, 5), tackle, charge);
            var engine = new BattleEngine(P(BattleSide.Player, atk), P(BattleSide.Enemy, def), settings, new FakeRng());

            // Turn 1: attacker uses Charge (index 0) -> charges, no damage to defender.
            var t1 = engine.ExecuteTurn(BattleAction.UseMove(0), BattleAction.UseMove(0));
            Assert.IsTrue(t1.OfType<ChargingEvent>().Any());
            Assert.AreEqual(def.MaxHp, def.CurrentHp);
            Assert.AreEqual(0, t1.OfType<DamageEvent>().Count(d => ReferenceEquals(d.Target, def)));

            // Turn 2: the chosen action is ignored; the engine forces the charge release.
            var t2 = engine.ExecuteTurn(BattleAction.UseMove(1), BattleAction.UseMove(0));
            Assert.Less(def.CurrentHp, def.MaxHp);
        }

        [Test]
        public void MovesAtLevelKeepsRecentFour()
        {
            var t = TestFactory.Move("T", ElementType.Normal, MoveCategory.Physical, 40);
            var pw = TestFactory.Move("PW", ElementType.Grass, MoveCategory.Special, 50);
            var g = TestFactory.Move("G", ElementType.Normal, MoveCategory.Status, 0);
            var s = TestFactory.Move("S", ElementType.Normal, MoveCategory.Physical, 55);
            var gl = TestFactory.Move("GL", ElementType.Normal, MoveCategory.Status, 0);
            var sp = TestFactory.Species("X", ElementType.Grass, 50, 40, 55, 65, 70, 60);
            sp.LevelUpLearnset = new List<LearnsetEntry>
            {
                new LearnsetEntry { Level = 1, Move = t },
                new LearnsetEntry { Level = 2, Move = pw },
                new LearnsetEntry { Level = 4, Move = g },
                new LearnsetEntry { Level = 7, Move = s },
                new LearnsetEntry { Level = 10, Move = gl },
            };
            Assert.AreEqual(1, sp.MovesAtLevel(1).Count);
            Assert.AreEqual(4, sp.MovesAtLevel(7).Count);
            var l10 = sp.MovesAtLevel(10);
            Assert.AreEqual(4, l10.Count);
            Assert.IsFalse(l10.Contains(t));   // oldest dropped
            Assert.IsTrue(l10.Contains(gl));
        }

        [Test]
        public void GrowlRaisesUserAttack()
        {
            var settings = TestFactory.Settings();
            var growl = TestFactory.Move("Growl", ElementType.Normal, MoveCategory.Status, 0, accuracy: 0);
            growl.StatToChange = Stat.Attack;
            growl.StatStageDelta = 1;
            growl.StatChangeTargetsSelf = true;
            growl.StatChangeChance = 100;
            var tackle = TestFactory.Move("Tackle", ElementType.Normal, MoveCategory.Physical, 40);

            var user = TestFactory.Mon(TestFactory.Species("U", ElementType.Normal, 100, 80, 80, 80, 80, 120), growl, tackle);
            var foe = TestFactory.Mon(TestFactory.Species("F", ElementType.Normal, 200, 80, 200, 80, 200, 5), tackle, growl);
            var engine = new BattleEngine(P(BattleSide.Player, user), P(BattleSide.Enemy, foe), settings, new FakeRng());

            Assert.AreEqual(0, user.GetStage(Stat.Attack));
            engine.ExecuteTurn(BattleAction.UseMove(0), BattleAction.UseMove(0));
            Assert.AreEqual(1, user.GetStage(Stat.Attack));
        }
    }
}
