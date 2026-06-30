using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using MonsterCatcher.Map;

namespace MonsterCatcher.Battle.Tests
{
    public class AbilityStatusTests
    {
        private static Pokemon Make(string ability)
        {
            var species = TestFactory.Species("A", ElementType.Normal, 60, 60, 60, 60, 60, 60);
            return new Pokemon(species, 50, TestFactory.OneMove(), new[] { ability });
        }

        [Test]
        public void Limber_IsImmuneToParalysisOnly()
        {
            var p = Make("Limber");
            Assert.IsTrue(AbilityApplier.ImmuneToStatus(p, StatusCondition.Paralysis));
            Assert.IsFalse(AbilityApplier.ImmuneToStatus(p, StatusCondition.Burn));
        }

        [Test]
        public void LuckyCharm_MultipliesSecondaryChance()
        {
            var lucky = Make("LuckyCharm");
            var plain = Make("Limber");
            Assert.AreEqual(1.5, AbilityApplier.SecondaryChanceMult(lucky), 1e-6);
            Assert.AreEqual(1.0, AbilityApplier.SecondaryChanceMult(plain), 1e-6);
        }

        [Test]
        public void Venomtouch_PoisonsDefenderWhenRollSucceeds()
        {
            var attacker = Make("Venomtouch");
            var defender = Make("Limber");
            var events = new List<BattleEvent>();

            var rng = new FakeRng().Enqueue(true);
            AbilityApplier.OnHitInflict(attacker, defender, rng, events);

            Assert.AreEqual(StatusCondition.Poison, defender.Status);
            Assert.AreEqual(StatusCondition.None, attacker.Status);
            Assert.AreEqual(1, events.OfType<StatusInflictedEvent>()
                .Count(e => e.Target == defender && e.Status == StatusCondition.Poison));
        }
    }
}
