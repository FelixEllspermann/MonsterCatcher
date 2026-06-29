using NUnit.Framework;
using UnityEngine;

namespace MonsterCatcher.Battle.Tests
{
    public class ElectricLineTests
    {
        private static SpeciesData Load(string n) => Resources.Load<SpeciesData>("Species/" + n);

        [Test] public void AllThreeLoadAsElectric()
        {
            foreach (var n in new[] { "Voltwig", "Stormbark", "Tempestag" })
            {
                var s = Load(n);
                Assert.IsNotNull(s, n);
                Assert.AreEqual(ElementType.Electric, s.Type1, n + " type");
            }
        }

        [Test] public void EvolutionChainAndLevels()
        {
            Assert.AreSame(Load("Stormbark"), Load("Voltwig").EvolvesInto);
            Assert.AreSame(Load("Tempestag"), Load("Stormbark").EvolvesInto);
            Assert.IsNull(Load("Tempestag").EvolvesInto);
            Assert.AreEqual(16, Load("Voltwig").EvolveLevel);
            Assert.AreEqual(34, Load("Stormbark").EvolveLevel);
            Assert.AreEqual(0, Load("Tempestag").EvolveLevel);
        }

        [Test] public void PhysicalSweeperProfile()
        {
            var t = Load("Tempestag");
            // physical sweeper: Attack + Speed are the strong stats, SpAttack lags
            Assert.Greater(t.BaseAttack, t.BaseSpAttack);
            Assert.Greater(t.BaseSpeed, t.BaseSpAttack);
            Assert.Greater(t.BaseAttack, t.BaseDefense);
        }

        [Test] public void SpritesAndLearnsetWired()
        {
            foreach (var n in new[] { "Voltwig", "Stormbark", "Tempestag" })
            {
                var s = Load(n);
                Assert.IsNotNull(s.FrontSprite, n + " front");
                Assert.IsNotNull(s.BackSprite, n + " back");
                Assert.GreaterOrEqual(s.LevelUpLearnset.Count, 5, n + " learnset");
            }
            var early = Load("Voltwig").MovesAtLevel(1);
            Assert.IsTrue(early.Exists(m => m.Type == ElementType.Electric), "Voltwig has an Electric move at L1");
        }
    }
}
