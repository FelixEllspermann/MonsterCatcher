using NUnit.Framework;
using UnityEngine;

namespace MonsterCatcher.Battle.Tests
{
    public class MoonLineTests
    {
        private static SpeciesData Load(string n) => Resources.Load<SpeciesData>("Species/" + n);

        [Test] public void AllThreeAreFairyDark()
        {
            foreach (var n in new[] { "Lunakit", "Moonlynx", "Eclipseon" })
            {
                var s = Load(n);
                Assert.IsNotNull(s, n);
                Assert.IsTrue(s.HasSecondType, n + " is dual-type");
                Assert.AreEqual(ElementType.Fairy, s.Type1, n + " primary");
                Assert.AreEqual(ElementType.Dark, s.Type2, n + " secondary");
            }
        }

        [Test] public void EvolutionChainAndLevels()
        {
            Assert.AreSame(Load("Moonlynx"), Load("Lunakit").EvolvesInto);
            Assert.AreSame(Load("Eclipseon"), Load("Moonlynx").EvolvesInto);
            Assert.IsNull(Load("Eclipseon").EvolvesInto);
            Assert.AreEqual(16, Load("Lunakit").EvolveLevel);
            Assert.AreEqual(34, Load("Moonlynx").EvolveLevel);
            Assert.AreEqual(0, Load("Eclipseon").EvolveLevel);
        }

        [Test] public void MixedAttackerProfile()
        {
            var e = Load("Eclipseon");
            Assert.AreEqual(e.BaseAttack, e.BaseSpAttack);   // balanced mixed offense (can hit from both sides)
        }

        [Test] public void DualTypeDefenseStacksBothTypes()
        {
            var s = Load("Lunakit");
            // Poison: 2x vs Fairy, 1x vs Dark -> 2x
            Assert.AreEqual(2.0,
                TypeChart.Effectiveness(ElementType.Poison, s.Type1, s.Type2, s.HasSecondType), 1e-6);
            // Fighting: 0.5x vs Fairy AND 2x vs Dark -> 1.0 (only correct if BOTH types are considered)
            Assert.AreEqual(1.0,
                TypeChart.Effectiveness(ElementType.Fighting, s.Type1, s.Type2, s.HasSecondType), 1e-6);
        }

        [Test] public void SpritesAndDualStabLearnset()
        {
            foreach (var n in new[] { "Lunakit", "Moonlynx", "Eclipseon" })
            {
                var s = Load(n);
                Assert.IsNotNull(s.FrontSprite, n + " front");
                Assert.IsNotNull(s.BackSprite, n + " back");
                Assert.GreaterOrEqual(s.LevelUpLearnset.Count, 5, n + " learnset");
            }
            var moves = Load("Eclipseon").MovesAtLevel(50);   // both STABs in the final kit
            Assert.IsTrue(moves.Exists(m => m.Type == ElementType.Fairy), "has a Fairy STAB");
            Assert.IsTrue(moves.Exists(m => m.Type == ElementType.Dark), "has a Dark STAB");
        }
    }
}
