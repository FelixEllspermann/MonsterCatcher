using System.Collections.Generic;
using UnityEngine;

namespace MonsterCatcher.Battle.Tests
{
    public static class TestFactory
    {
        public static BattleSettings Settings()
        {
            return ScriptableObject.CreateInstance<BattleSettings>();
        }

        public static MoveData Move(string name, ElementType type, MoveCategory cat,
            int power, int accuracy = 100, int priority = 0, int maxPp = 35)
        {
            var m = ScriptableObject.CreateInstance<MoveData>();
            m.DisplayName = name;
            m.Type = type;
            m.Category = cat;
            m.Power = power;
            m.Accuracy = accuracy;
            m.Priority = priority;
            m.MaxPp = maxPp;
            return m;
        }

        public static SpeciesData Species(string name, ElementType t1, int hp, int atk,
            int def, int spa, int spd, int spe, ElementType? t2 = null,
            List<MoveData> moves = null)
        {
            var s = ScriptableObject.CreateInstance<SpeciesData>();
            s.DisplayName = name;
            s.Type1 = t1;
            s.HasSecondType = t2.HasValue;
            s.Type2 = t2 ?? t1;
            s.BaseHp = hp; s.BaseAttack = atk; s.BaseDefense = def;
            s.BaseSpAttack = spa; s.BaseSpDefense = spd; s.BaseSpeed = spe;
            s.LearnableMoves = moves ?? new List<MoveData>();
            return s;
        }

        public static Pokemon Mon(SpeciesData species, params MoveData[] moves)
        {
            return new Pokemon(species, 50, new List<MoveData>(moves));
        }

        public static List<MoveData> OneMove() =>
            new List<MoveData> { Move("Tackle", ElementType.Normal, MoveCategory.Physical, 40) };
    }
}
