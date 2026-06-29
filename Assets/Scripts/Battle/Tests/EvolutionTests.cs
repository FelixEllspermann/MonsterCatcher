using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class EvolutionTests
    {
        [Test]
        public void CanEvolveAtRespectsLevelAndTarget()
        {
            var to = TestFactory.Species("Evolved", ElementType.Grass, 70, 50, 70, 90, 95, 80);
            var from = TestFactory.Species("Base", ElementType.Grass, 50, 40, 55, 65, 70, 60);
            from.EvolvesInto = to;
            from.EvolveLevel = 5;

            Assert.IsFalse(from.CanEvolveAt(4));
            Assert.IsTrue(from.CanEvolveAt(5));
            Assert.IsTrue(from.CanEvolveAt(6));
            Assert.IsFalse(to.CanEvolveAt(99));   // no EvolvesInto set
        }
    }
}
