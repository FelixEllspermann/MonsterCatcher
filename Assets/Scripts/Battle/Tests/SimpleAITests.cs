using System.Linq;
using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class SimpleAITests
    {
        private static Party P(BattleSide side, params Pokemon[] mons) =>
            new Party(side, mons.ToList(), 6);

        [Test] public void PicksStrongestEffectiveMove()
        {
            var ember = TestFactory.Move("Ember", ElementType.Fire, MoveCategory.Special, 40);
            var watergun = TestFactory.Move("Water Gun", ElementType.Water, MoveCategory.Special, 40);
            var self = TestFactory.Mon(TestFactory.Species("Mix", ElementType.Normal, 60, 60, 60, 60, 60, 60), ember, watergun);
            var foe = TestFactory.Mon(TestFactory.Species("Rock", ElementType.Rock, 60, 60, 60, 60, 60, 60)); // Water 2x, Fire 0.5x
            var ai = new SimpleAI();
            var action = ai.ChooseAction(P(BattleSide.Enemy, self), P(BattleSide.Player, foe));
            Assert.AreEqual(ActionKind.Move, action.Kind);
            Assert.AreEqual(1, action.Index); // Water Gun
        }

        [Test] public void SwitchesWhenOutmatchedAndLowHp()
        {
            var tackle = TestFactory.Move("Tackle", ElementType.Normal, MoveCategory.Physical, 40);
            // Active is Grass, opponent is Fire -> Fire 2x vs Grass (danger).
            var active = TestFactory.Mon(TestFactory.Species("Grass", ElementType.Grass, 60, 60, 60, 60, 60, 60), tackle);
            active.TakeDamage(active.MaxHp - 1); // very low HP
            // Bench is Water -> Fire 0.5x vs Water (safer).
            var bench = TestFactory.Mon(TestFactory.Species("Water", ElementType.Water, 60, 60, 60, 60, 60, 60), tackle);
            var foe = TestFactory.Mon(TestFactory.Species("Fire", ElementType.Fire, 60, 60, 60, 60, 60, 60), tackle);
            var ai = new SimpleAI();
            var action = ai.ChooseAction(P(BattleSide.Enemy, active, bench), P(BattleSide.Player, foe));
            Assert.AreEqual(ActionKind.Switch, action.Kind);
            Assert.AreEqual(1, action.Index);
        }

        [Test] public void DoesNotSwitchWhenHealthy()
        {
            var tackle = TestFactory.Move("Tackle", ElementType.Normal, MoveCategory.Physical, 40);
            var active = TestFactory.Mon(TestFactory.Species("Grass", ElementType.Grass, 60, 60, 60, 60, 60, 60), tackle);
            var bench = TestFactory.Mon(TestFactory.Species("Water", ElementType.Water, 60, 60, 60, 60, 60, 60), tackle);
            var foe = TestFactory.Mon(TestFactory.Species("Fire", ElementType.Fire, 60, 60, 60, 60, 60, 60), tackle);
            var ai = new SimpleAI();
            var action = ai.ChooseAction(P(BattleSide.Enemy, active, bench), P(BattleSide.Player, foe));
            Assert.AreEqual(ActionKind.Move, action.Kind);
        }
    }
}
