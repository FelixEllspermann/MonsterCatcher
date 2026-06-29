using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class TypeChartTests
    {
        [Test] public void SuperEffective() =>
            Assert.AreEqual(2.0, TypeChart.Effectiveness(ElementType.Water, ElementType.Fire), 1e-6);

        [Test] public void NotVeryEffective() =>
            Assert.AreEqual(0.5, TypeChart.Effectiveness(ElementType.Fire, ElementType.Water), 1e-6);

        [Test] public void Immune() =>
            Assert.AreEqual(0.0, TypeChart.Effectiveness(ElementType.Normal, ElementType.Ghost), 1e-6);

        [Test] public void Neutral() =>
            Assert.AreEqual(1.0, TypeChart.Effectiveness(ElementType.Normal, ElementType.Water), 1e-6);

        [Test] public void DualTypeStacks() =>
            // Rock attacking Fire/Flying = 2 * 2 = 4
            Assert.AreEqual(4.0, TypeChart.Effectiveness(ElementType.Rock, ElementType.Fire, ElementType.Flying, true), 1e-6);

        [Test] public void DualTypeImmunityWins() =>
            // Ground 0 vs Flying, even if the other type is weak to Ground
            Assert.AreEqual(0.0, TypeChart.Effectiveness(ElementType.Ground, ElementType.Flying, ElementType.Fire, true), 1e-6);
    }
}
