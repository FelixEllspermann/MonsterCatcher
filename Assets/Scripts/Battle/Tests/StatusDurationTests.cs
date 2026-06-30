using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class StatusDurationTests
    {
        private static Party P(BattleSide side, params Pokemon[] mons) =>
            new Party(side, mons.ToList(), 6);

        [Test]
        public void DefaultDurationsAreSet()
        {
            Assert.AreEqual(0, Pokemon.DefaultStatusDuration(StatusCondition.None));
            Assert.Greater(Pokemon.DefaultStatusDuration(StatusCondition.Poison), 0);
            Assert.Greater(Pokemon.DefaultStatusDuration(StatusCondition.Burn), 0);
            Assert.Greater(Pokemon.DefaultStatusDuration(StatusCondition.Paralysis), 0);
            Assert.Greater(Pokemon.DefaultStatusDuration(StatusCondition.Sleep), 0);
        }

        [Test]
        public void ParalysisWearsOffAfterItsDuration()
        {
            var settings = TestFactory.Settings();
            var noop = TestFactory.Move("NoOp", ElementType.Normal, MoveCategory.Status, 0, accuracy: 0);
            var a = TestFactory.Mon(TestFactory.Species("A", ElementType.Normal, 300, 60, 200, 60, 200, 50), noop);
            var b = TestFactory.Mon(TestFactory.Species("B", ElementType.Normal, 300, 60, 200, 60, 200, 40), noop);
            a.TryApplyStatus(StatusCondition.Paralysis);
            int dur = Pokemon.DefaultStatusDuration(StatusCondition.Paralysis);

            var engine = new BattleEngine(P(BattleSide.Player, a), P(BattleSide.Enemy, b), settings, new FakeRng());
            IReadOnlyList<BattleEvent> last = null;
            for (int i = 0; i < dur; i++)
            {
                Assert.AreEqual(StatusCondition.Paralysis, a.Status);   // still paralyzed up to the final tick
                last = engine.ExecuteTurn(BattleAction.UseMove(0), BattleAction.UseMove(0));
            }
            Assert.AreEqual(StatusCondition.None, a.Status);            // worn off
            Assert.IsTrue(last.OfType<StatusEndedEvent>().Any(e => e.Target == a && e.Status == StatusCondition.Paralysis));
        }
    }
}
