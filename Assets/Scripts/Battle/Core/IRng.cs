using System;

namespace MonsterCatcher.Battle
{
    public interface IRng
    {
        bool Roll(double probability);
        int IntInclusive(int minInclusive, int maxInclusive);
    }

    public sealed class DefaultRng : IRng
    {
        private readonly Random _random;

        public DefaultRng() { _random = new Random(); }
        public DefaultRng(int seed) { _random = new Random(seed); }

        public bool Roll(double probability)
        {
            if (probability >= 1.0) return true;
            if (probability <= 0.0) return false;
            return _random.NextDouble() < probability;
        }

        public int IntInclusive(int minInclusive, int maxInclusive)
        {
            return _random.Next(minInclusive, maxInclusive + 1);
        }
    }
}
