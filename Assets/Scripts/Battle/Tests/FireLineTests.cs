using NUnit.Framework;
using UnityEngine;

namespace MonsterCatcher.Battle.Tests
{
    public class FireLineTests
    {
        private static SpeciesData Load(string n) => Resources.Load<SpeciesData>("Species/" + n);

        [Test] public void AllThreeLoadAsFire()
        {
            foreach (var n in new[] { "Cindrop", "Magmelt", "Vulcarion" })
            {
                var s = Load(n);
                Assert.IsNotNull(s, n);
                Assert.AreEqual(ElementType.Fire, s.Type1, n + " type");
            }
        }

        [Test] public void EvolutionChainAndLevels()
        {
            Assert.AreSame(Load("Magmelt"), Load("Cindrop").EvolvesInto);
            Assert.AreSame(Load("Vulcarion"), Load("Magmelt").EvolvesInto);
            Assert.IsNull(Load("Vulcarion").EvolvesInto);
            Assert.AreEqual(16, Load("Cindrop").EvolveLevel);
            Assert.AreEqual(34, Load("Magmelt").EvolveLevel);
            Assert.AreEqual(0, Load("Vulcarion").EvolveLevel);
        }

        [Test] public void GlassCannonProfile()
        {
            var v = Load("Vulcarion");
            // special attacker: SpAttack + Speed are the strong stats, defenses lag behind
            Assert.Greater(v.BaseSpAttack, v.BaseAttack);
            Assert.Greater(v.BaseSpAttack, v.BaseSpDefense);
            Assert.Greater(v.BaseSpeed, v.BaseDefense);
        }

        [Test] public void SpritesAndLearnsetWired()
        {
            foreach (var n in new[] { "Cindrop", "Magmelt", "Vulcarion" })
            {
                var s = Load(n);
                Assert.IsNotNull(s.FrontSprite, n + " front");
                Assert.IsNotNull(s.BackSprite, n + " back");
                Assert.GreaterOrEqual(s.LevelUpLearnset.Count, 5, n + " learnset");
            }
            var early = Load("Cindrop").MovesAtLevel(1);
            Assert.IsTrue(early.Exists(m => m.Type == ElementType.Fire), "Cindrop has a Fire move at L1");
        }
    }
}
