using NUnit.Framework;
using UnityEngine;

namespace MonsterCatcher.Battle.Tests
{
    public class SpeciesAssetTests
    {
        private static SpeciesData Load(string n) => Resources.Load<SpeciesData>("Species/" + n);

        [Test] public void AllThreeLoad()
        {
            Assert.IsNotNull(Load("Mossprig"));
            Assert.IsNotNull(Load("Briarstag"));
            Assert.IsNotNull(Load("Elderthorn"));
        }

        [Test] public void AllAreGrass()
        {
            Assert.AreEqual(ElementType.Grass, Load("Mossprig").Type1);
            Assert.AreEqual(ElementType.Grass, Load("Briarstag").Type1);
            Assert.AreEqual(ElementType.Grass, Load("Elderthorn").Type1);
        }

        [Test] public void EvolutionChainLinks()
        {
            Assert.AreSame(Load("Briarstag"), Load("Mossprig").EvolvesInto);
            Assert.AreSame(Load("Elderthorn"), Load("Briarstag").EvolvesInto);
            Assert.IsNull(Load("Elderthorn").EvolvesInto);
        }

        [Test] public void EvolveLevels()
        {
            Assert.AreEqual(16, Load("Mossprig").EvolveLevel);
            Assert.AreEqual(34, Load("Briarstag").EvolveLevel);
            Assert.AreEqual(0, Load("Elderthorn").EvolveLevel);
        }

        [Test] public void SpritesAssigned()
        {
            foreach (var n in new[] { "Mossprig", "Briarstag", "Elderthorn" })
            {
                var s = Load(n);
                Assert.IsNotNull(s.FrontSprite, n + " front");
                Assert.IsNotNull(s.BackSprite, n + " back");
            }
        }

        [Test] public void StatSpotCheck()
        {
            Assert.AreEqual(120, Load("Elderthorn").BaseSpAttack);
            Assert.AreEqual(40, Load("Mossprig").BaseAttack);
        }

        [Test] public void GrassLearnsetRedistributed()
        {
            var moves = Load("Mossprig").MovesAtLevel(10);   // special-tank kit
            Assert.IsTrue(moves.Exists(m => m.DisplayName == "Mega Drain"), "drain");
            Assert.IsTrue(moves.Exists(m => m.DisplayName == "Sleep Spore"), "sleep");
        }
    }
}
