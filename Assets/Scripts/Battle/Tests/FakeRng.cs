using System.Collections.Generic;

namespace MonsterCatcher.Battle.Tests
{
    /// <summary>Deterministic IRng for tests.
    /// Roll() returns true for p&gt;=1 and false for p&lt;=0; otherwise it dequeues a
    /// scripted result, falling back to <see cref="DefaultRoll"/>.
    /// IntInclusive() returns <see cref="IntResult"/> clamped into range
    /// (default int.MaxValue -&gt; the max, i.e. damage factor 100).</summary>
    public sealed class FakeRng : IRng
    {
        public bool DefaultRoll = false;
        public int IntResult = int.MaxValue;
        private readonly Queue<bool> _rolls = new Queue<bool>();

        public FakeRng Enqueue(params bool[] results)
        {
            foreach (var r in results) _rolls.Enqueue(r);
            return this;
        }

        public bool Roll(double probability)
        {
            if (probability >= 1.0) return true;
            if (probability <= 0.0) return false;
            return _rolls.Count > 0 ? _rolls.Dequeue() : DefaultRoll;
        }

        public int IntInclusive(int minInclusive, int maxInclusive)
        {
            int v = IntResult;
            if (v < minInclusive) v = minInclusive;
            if (v > maxInclusive) v = maxInclusive;
            return v;
        }
    }
}
