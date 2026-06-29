using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class StatStagesTests
    {
        [Test] public void Neutral() => Assert.AreEqual(1.0, StatStages.Multiplier(0), 1e-6);
        [Test] public void PlusOne() => Assert.AreEqual(1.5, StatStages.Multiplier(1), 1e-6);
        [Test] public void PlusSix() => Assert.AreEqual(4.0, StatStages.Multiplier(6), 1e-6);
        [Test] public void MinusOne() => Assert.AreEqual(2.0 / 3.0, StatStages.Multiplier(-1), 1e-6);
        [Test] public void MinusSix() => Assert.AreEqual(0.25, StatStages.Multiplier(-6), 1e-6);
        [Test] public void ClampsBeyondRange() => Assert.AreEqual(4.0, StatStages.Multiplier(99), 1e-6);
    }
}
