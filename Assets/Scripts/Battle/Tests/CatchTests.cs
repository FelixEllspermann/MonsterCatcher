using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class CatchTests
    {
        private static Pokemon Wild(int baseHp)
        {
            var sp = TestFactory.Species("W", ElementType.Normal, baseHp, 60, 60, 60, 60, 60);
            return new Pokemon(sp, 50, TestFactory.OneMove());   // starts at full HP
        }

        [Test] public void FullHpChanceIsBase()
        {
            Assert.AreEqual(0.30, CatchCalculator.Chance(Wild(200)), 1e-9);
        }

        [Test] public void WeakerMonsterIsEasierToCatch()
        {
            var full = Wild(200);
            var weak = Wild(200); weak.SetCurrentHp(weak.MaxHp / 10);
            Assert.Less(CatchCalculator.Chance(full), CatchCalculator.Chance(weak));
        }

        [Test] public void StatusAddsACatchBonus()
        {
            var plain = Wild(200); plain.SetCurrentHp(plain.MaxHp / 2);
            var asleep = Wild(200); asleep.SetCurrentHp(asleep.MaxHp / 2);
            asleep.TryApplyStatus(StatusCondition.Sleep);
            Assert.Greater(CatchCalculator.Chance(asleep), CatchCalculator.Chance(plain));
        }

        [Test] public void ChanceIsClampedToRange()
        {
            var nearDead = Wild(200); nearDead.SetCurrentHp(1); nearDead.TryApplyStatus(StatusCondition.Sleep);
            double c = CatchCalculator.Chance(nearDead);
            Assert.LessOrEqual(c, 0.95);
            Assert.GreaterOrEqual(CatchCalculator.Chance(Wild(200)), 0.10);
        }
    }
}
