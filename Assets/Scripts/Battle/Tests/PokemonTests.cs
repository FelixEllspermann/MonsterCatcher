using System.Collections.Generic;
using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class PokemonTests
    {
        private static Pokemon Make(int hp, int atk, int def, int spa, int spd, int spe)
        {
            var species = TestFactory.Species("Mon", ElementType.Normal, hp, atk, def, spa, spd, spe);
            return new Pokemon(species, 50, new List<MoveData>());
        }

        [Test] public void MaxHpFormula()
        {
            // (2*45*50)/100 + 50 + 10 = 45 + 60 = 105
            Assert.AreEqual(105, Make(45, 49, 49, 65, 65, 45).MaxHp);
        }

        [Test] public void RawStatFormula()
        {
            // (2*49*50)/100 + 5 = 49 + 5 = 54
            Assert.AreEqual(54, Make(45, 49, 49, 65, 65, 45).GetRawStat(Stat.Attack));
        }

        [Test] public void StageRaisesEffectiveStat()
        {
            var p = Make(45, 100, 49, 65, 65, 45); // raw atk = 105
            p.ChangeStage(Stat.Attack, 1);          // x1.5
            Assert.AreEqual(157, p.EffectiveStat(Stat.Attack)); // floor(105*1.5)=157
        }

        [Test] public void CritIgnoresNegativeAttackerStage()
        {
            var p = Make(45, 100, 49, 65, 65, 45); // raw atk = 105
            p.ChangeStage(Stat.Attack, -2);         // x0.5 normally
            Assert.AreEqual(105, p.EffectiveStat(Stat.Attack, crit: true, ignoreNegative: true));
        }

        [Test] public void TakeDamageAndFaint()
        {
            var p = Make(45, 49, 49, 65, 65, 45);
            p.TakeDamage(1000);
            Assert.IsTrue(p.IsFainted);
            Assert.AreEqual(0, p.CurrentHp);
        }

        [Test] public void StatusAppliesOnlyWhenHealthy()
        {
            var p = Make(45, 49, 49, 65, 65, 45);
            Assert.IsTrue(p.TryApplyStatus(StatusCondition.Poison));
            Assert.IsFalse(p.TryApplyStatus(StatusCondition.Burn)); // already statused
            Assert.AreEqual(StatusCondition.Poison, p.Status);
        }
    }
}
