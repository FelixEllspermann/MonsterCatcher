using System.Collections.Generic;

namespace MonsterCatcher.Battle
{
    public static class TypeChart
    {
        // _chart[attacker][defender] = multiplier for entries that are not 1.0.
        private static readonly Dictionary<ElementType, Dictionary<ElementType, double>> _chart =
            Build();

        public static double Effectiveness(ElementType attacking, ElementType defending)
        {
            if (_chart.TryGetValue(attacking, out var row) &&
                row.TryGetValue(defending, out var mult))
            {
                return mult;
            }
            return 1.0;
        }

        public static double Effectiveness(ElementType attacking, ElementType defType1,
            ElementType defType2, bool hasSecondType)
        {
            double e = Effectiveness(attacking, defType1);
            if (hasSecondType) e *= Effectiveness(attacking, defType2);
            return e;
        }

        private static Dictionary<ElementType, Dictionary<ElementType, double>> Build()
        {
            var c = new Dictionary<ElementType, Dictionary<ElementType, double>>();

            void Row(ElementType atk, params (ElementType def, double mult)[] entries)
            {
                var d = new Dictionary<ElementType, double>();
                foreach (var (def, mult) in entries) d[def] = mult;
                c[atk] = d;
            }

            const double H = 0.5, X = 2.0, Z = 0.0;
            Row(ElementType.Normal, (ElementType.Rock, H), (ElementType.Ghost, Z), (ElementType.Steel, H));
            Row(ElementType.Fire, (ElementType.Fire, H), (ElementType.Water, H), (ElementType.Grass, X), (ElementType.Ice, X), (ElementType.Bug, X), (ElementType.Rock, H), (ElementType.Dragon, H), (ElementType.Steel, X));
            Row(ElementType.Water, (ElementType.Fire, X), (ElementType.Water, H), (ElementType.Grass, H), (ElementType.Ground, X), (ElementType.Rock, X), (ElementType.Dragon, H));
            Row(ElementType.Electric, (ElementType.Water, X), (ElementType.Electric, H), (ElementType.Grass, H), (ElementType.Ground, Z), (ElementType.Flying, X), (ElementType.Dragon, H));
            Row(ElementType.Grass, (ElementType.Fire, H), (ElementType.Water, X), (ElementType.Grass, H), (ElementType.Poison, H), (ElementType.Ground, X), (ElementType.Flying, H), (ElementType.Bug, H), (ElementType.Rock, X), (ElementType.Dragon, H), (ElementType.Steel, H));
            Row(ElementType.Ice, (ElementType.Fire, H), (ElementType.Water, H), (ElementType.Grass, X), (ElementType.Ice, H), (ElementType.Ground, X), (ElementType.Flying, X), (ElementType.Dragon, X), (ElementType.Steel, H));
            Row(ElementType.Fighting, (ElementType.Normal, X), (ElementType.Ice, X), (ElementType.Poison, H), (ElementType.Flying, H), (ElementType.Psychic, H), (ElementType.Bug, H), (ElementType.Rock, X), (ElementType.Ghost, Z), (ElementType.Dark, X), (ElementType.Steel, X), (ElementType.Fairy, H));
            Row(ElementType.Poison, (ElementType.Grass, X), (ElementType.Poison, H), (ElementType.Ground, H), (ElementType.Rock, H), (ElementType.Ghost, H), (ElementType.Steel, Z), (ElementType.Fairy, X));
            Row(ElementType.Ground, (ElementType.Fire, X), (ElementType.Electric, X), (ElementType.Grass, H), (ElementType.Poison, X), (ElementType.Flying, Z), (ElementType.Bug, H), (ElementType.Rock, X), (ElementType.Steel, X));
            Row(ElementType.Flying, (ElementType.Electric, H), (ElementType.Grass, X), (ElementType.Fighting, X), (ElementType.Bug, X), (ElementType.Rock, H), (ElementType.Steel, H));
            Row(ElementType.Psychic, (ElementType.Fighting, X), (ElementType.Poison, X), (ElementType.Psychic, H), (ElementType.Dark, Z), (ElementType.Steel, H));
            Row(ElementType.Bug, (ElementType.Fire, H), (ElementType.Grass, X), (ElementType.Fighting, H), (ElementType.Poison, H), (ElementType.Flying, H), (ElementType.Psychic, X), (ElementType.Ghost, H), (ElementType.Dark, X), (ElementType.Steel, H), (ElementType.Fairy, H));
            Row(ElementType.Rock, (ElementType.Fire, X), (ElementType.Ice, X), (ElementType.Fighting, H), (ElementType.Ground, H), (ElementType.Flying, X), (ElementType.Bug, X), (ElementType.Steel, H));
            Row(ElementType.Ghost, (ElementType.Normal, Z), (ElementType.Psychic, X), (ElementType.Ghost, X), (ElementType.Dark, H));
            Row(ElementType.Dragon, (ElementType.Dragon, X), (ElementType.Steel, H), (ElementType.Fairy, Z));
            Row(ElementType.Dark, (ElementType.Fighting, H), (ElementType.Psychic, X), (ElementType.Ghost, X), (ElementType.Dark, H), (ElementType.Fairy, H));
            Row(ElementType.Steel, (ElementType.Fire, H), (ElementType.Water, H), (ElementType.Electric, H), (ElementType.Ice, X), (ElementType.Rock, X), (ElementType.Steel, H), (ElementType.Fairy, X));
            Row(ElementType.Fairy, (ElementType.Fire, H), (ElementType.Fighting, X), (ElementType.Poison, H), (ElementType.Dragon, X), (ElementType.Dark, X), (ElementType.Steel, H));

            return c;
        }
    }
}
