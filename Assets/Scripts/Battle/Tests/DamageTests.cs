using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class DamageTests
    {
        private static BattleSettings Settings() => TestFactory.Settings();

        private static Pokemon Attacker(ElementType type)
        {
            var s = TestFactory.Species("A", type, 100, 120, 100, 120, 100, 100);
            return TestFactory.Mon(s);
        }

        [Test] public void MissReturnsNoDamage()
        {
            var move = TestFactory.Move("Shaky", ElementType.Normal, MoveCategory.Physical, 50, accuracy: 50);
            var rng = new FakeRng(); // Roll(0.5) -> DefaultRoll false -> miss
            var res = DamageCalculator.Calculate(Attacker(ElementType.Normal),
                Attacker(ElementType.Normal), move, Settings(), rng);
            Assert.IsFalse(res.Hit);
            Assert.AreEqual(0, res.Damage);
        }

        [Test] public void ImmunityDealsZero()
        {
            var move = TestFactory.Move("Tackle", ElementType.Normal, MoveCategory.Physical, 50);
            var defender = TestFactory.Mon(TestFactory.Species("Ghost", ElementType.Ghost, 100, 80, 80, 80, 80, 80));
            var res = DamageCalculator.Calculate(Attacker(ElementType.Normal), defender, move, Settings(), new FakeRng());
            Assert.IsTrue(res.Hit);
            Assert.AreEqual(0, res.Damage);
            Assert.AreEqual(0.0, res.Effectiveness, 1e-6);
        }

        [Test] public void DeterministicDamageMaxRollNoCrit()
        {
            var move = TestFactory.Move("Ember", ElementType.Fire, MoveCategory.Special, 90);
            var defender = TestFactory.Mon(TestFactory.Species("Leaf", ElementType.Grass, 100, 80, 80, 80, 80, 80));
            var rng = new FakeRng(); // no crit, factor 100
            var res = DamageCalculator.Calculate(Attacker(ElementType.Fire), defender, move, Settings(), rng);
            // STAB 1.5 (Fire attacker, Fire move) * type 2.0 (Fire vs Grass), max roll, no crit
            Assert.Greater(res.Damage, 0);
            Assert.AreEqual(2.0, res.Effectiveness, 1e-6);
            Assert.IsFalse(res.WasCritical);
        }

        [Test] public void CritUsesMultiplier()
        {
            var move = TestFactory.Move("Ember", ElementType.Fire, MoveCategory.Special, 90);
            var defender = TestFactory.Mon(TestFactory.Species("Leaf", ElementType.Grass, 100, 80, 80, 80, 80, 80));
            var noCrit = DamageCalculator.Calculate(Attacker(ElementType.Fire), defender, move, Settings(), new FakeRng());
            // Accuracy 100 -> Roll(1.0) returns true without dequeuing, so the first queued bool is the crit roll.
            var crit = DamageCalculator.Calculate(Attacker(ElementType.Fire), defender, move, Settings(),
                new FakeRng().Enqueue(true));
            Assert.Greater(crit.Damage, noCrit.Damage);
        }

        [Test] public void BurnHalvesPhysical()
        {
            var move = TestFactory.Move("Tackle", ElementType.Normal, MoveCategory.Physical, 80);
            var attacker = Attacker(ElementType.Normal);
            attacker.TryApplyStatus(StatusCondition.Burn);
            var defender = TestFactory.Mon(TestFactory.Species("D", ElementType.Water, 100, 80, 80, 80, 80, 80));
            var healthy = Attacker(ElementType.Normal);
            var burned = DamageCalculator.Calculate(attacker, defender, move, Settings(), new FakeRng());
            var normal = DamageCalculator.Calculate(healthy, defender, move, Settings(), new FakeRng());
            Assert.Less(burned.Damage, normal.Damage);
        }
    }
}
