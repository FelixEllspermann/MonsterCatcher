using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using MonsterCatcher.Map;

namespace MonsterCatcher.Battle.Tests
{
    public class AbilityIncomingTests
    {
        static MoveData FireHit() => TestFactory.Move("Ember", ElementType.Fire, MoveCategory.Special, 60);
        static MoveData NormalHit() => TestFactory.Move("Hit", ElementType.Normal, MoveCategory.Physical, 60);

        [Test]
        public void Thickhide_ReducesIncomingByTwelvePercent()
        {
            var move = NormalHit();
            var attacker = new Pokemon(
                TestFactory.Species("A", ElementType.Normal, 100, 120, 100, 100, 100, 100),
                50, new List<MoveData> { move }, new string[0]);

            var plainDef = new Pokemon(
                TestFactory.Species("D", ElementType.Normal, 200, 100, 100, 100, 100, 100),
                50, new List<MoveData> { move }, new string[0]);
            var tankDef = new Pokemon(
                TestFactory.Species("D", ElementType.Normal, 200, 100, 100, 100, 100, 100),
                50, new List<MoveData> { move }, new[] { "Thickhide" });

            float plain = AbilityApplier.IncomingMultiplier(plainDef, attacker, move, 1.0);
            float tank = AbilityApplier.IncomingMultiplier(tankDef, attacker, move, 1.0);

            Assert.AreEqual(1f, plain, 1e-4f);
            Assert.AreEqual(0.88f, tank, 1e-4f);
        }

        [Test]
        public void Heatproof_HalvesFireButNotNormal()
        {
            var defender = new Pokemon(
                TestFactory.Species("D", ElementType.Normal, 200, 100, 100, 100, 100, 100),
                50, new List<MoveData> { FireHit() }, new[] { "Heatproof" });
            var attacker = new Pokemon(
                TestFactory.Species("A", ElementType.Normal, 100, 120, 100, 120, 100, 100),
                50, new List<MoveData> { FireHit() }, new string[0]);

            float vsFire = AbilityApplier.IncomingMultiplier(defender, attacker, FireHit(), 1.0);
            float vsNormal = AbilityApplier.IncomingMultiplier(defender, attacker, NormalHit(), 1.0);

            Assert.AreEqual(0.5f, vsFire, 1e-4f);
            Assert.AreEqual(1f, vsNormal, 1e-4f);
        }

        [Test]
        public void Multiscale_OnlyAppliesAtFullHp()
        {
            var move = NormalHit();
            var defender = new Pokemon(
                TestFactory.Species("D", ElementType.Normal, 200, 100, 100, 100, 100, 100),
                50, new List<MoveData> { move }, new[] { "Multiscale" });
            var attacker = new Pokemon(
                TestFactory.Species("A", ElementType.Normal, 100, 120, 100, 100, 100, 100),
                50, new List<MoveData> { move }, new string[0]);

            float atFull = AbilityApplier.IncomingMultiplier(defender, attacker, move, 1.0);
            Assert.AreEqual(0.5f, atFull, 1e-4f);

            defender.TakeDamage(1);
            float afterHit = AbilityApplier.IncomingMultiplier(defender, attacker, move, 1.0);
            Assert.AreEqual(1f, afterHit, 1e-4f);
        }
    }
}
