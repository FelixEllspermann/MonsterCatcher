using NUnit.Framework;
using UnityEngine;

namespace MonsterCatcher.Battle.Tests
{
    public class LoreTests
    {
        [Test] public void EverySpeciesHasLore()
        {
            foreach (var n in new[]
            {
                "Mossprig", "Briarstag", "Elderthorn", "Cindrop", "Magmelt", "Vulcarion",
                "Voltwig", "Stormbark", "Tempestag", "Lunakit", "Moonlynx", "Eclipseon"
            })
            {
                var s = Resources.Load<SpeciesData>("Species/" + n);
                Assert.IsNotNull(s, n);
                Assert.IsFalse(string.IsNullOrWhiteSpace(s.LoreText), n + " has lore");
            }
        }
    }
}
