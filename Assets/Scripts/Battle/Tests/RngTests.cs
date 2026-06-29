using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class RngTests
    {
        [Test] public void Roll_Certain_IsAlwaysTrue()
        {
            Assert.IsTrue(new DefaultRng(1).Roll(1.0));
            Assert.IsFalse(new DefaultRng(1).Roll(0.0));
        }

        [Test] public void IntInclusive_IsDeterministicForSeed()
        {
            var a = new DefaultRng(42);
            var b = new DefaultRng(42);
            Assert.AreEqual(a.IntInclusive(85, 100), b.IntInclusive(85, 100));
        }
    }
}
