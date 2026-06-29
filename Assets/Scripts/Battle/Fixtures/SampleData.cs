using System.Collections.Generic;
using UnityEngine;

namespace MonsterCatcher.Battle
{
    public static class SampleData
    {
        public static BattleSettings CreateSettings()
        {
            return ScriptableObject.CreateInstance<BattleSettings>();
        }

        private static MoveData Move(string name, ElementType type, MoveCategory cat,
            int power, int accuracy = 100, int priority = 0)
        {
            var m = ScriptableObject.CreateInstance<MoveData>();
            m.DisplayName = name; m.Type = type; m.Category = cat;
            m.Power = power; m.Accuracy = accuracy; m.Priority = priority; m.MaxPp = 25;
            return m;
        }

        private static SpeciesData Species(string name, ElementType t1, int hp, int atk,
            int def, int spa, int spd, int spe)
        {
            var s = ScriptableObject.CreateInstance<SpeciesData>();
            s.DisplayName = name; s.Type1 = t1; s.HasSecondType = false; s.Type2 = t1;
            s.BaseHp = hp; s.BaseAttack = atk; s.BaseDefense = def;
            s.BaseSpAttack = spa; s.BaseSpDefense = spd; s.BaseSpeed = spe;
            return s;
        }

        private static SpeciesData LoadSpecies(string name)
        {
            var s = Resources.Load<SpeciesData>("Species/" + name);
            return s != null ? s : Species(name, ElementType.Grass, 60, 50, 60, 70, 70, 70);
        }

        public static List<MoveData> PlaceholderGrassMoves()
        {
            return new List<MoveData>
            {
                Move("Vine Whip", ElementType.Grass, MoveCategory.Physical, 45),
                Move("Mega Drain", ElementType.Grass, MoveCategory.Special, 40),
            };
        }

        public static Party CreatePlayerParty(BattleSettings settings)
        {
            var p1 = new Pokemon(LoadSpecies("Mossprig"), 50, PlaceholderGrassMoves());
            var p2 = new Pokemon(LoadSpecies("Briarstag"), 50, PlaceholderGrassMoves());
            return new Party(BattleSide.Player, new List<Pokemon> { p1, p2 }, settings.MaxPartySize);
        }

        public static Party CreateEnemyParty(BattleSettings settings)
        {
            var e1 = new Pokemon(LoadSpecies("Elderthorn"), 50, PlaceholderGrassMoves());
            return new Party(BattleSide.Enemy, new List<Pokemon> { e1 }, settings.MaxPartySize);
        }
    }
}
