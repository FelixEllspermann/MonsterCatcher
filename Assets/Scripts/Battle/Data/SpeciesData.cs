using System.Collections.Generic;
using UnityEngine;

namespace MonsterCatcher.Battle
{
    [CreateAssetMenu(menuName = "MonsterCatcher/Species", fileName = "Species")]
    public class SpeciesData : ScriptableObject
    {
        public string DisplayName = "New Species";
        public ElementType Type1 = ElementType.Normal;
        public ElementType Type2 = ElementType.Normal;
        public bool HasSecondType = false;

        [Min(1)] public int BaseHp = 45;
        [Min(1)] public int BaseAttack = 49;
        [Min(1)] public int BaseDefense = 49;
        [Min(1)] public int BaseSpAttack = 65;
        [Min(1)] public int BaseSpDefense = 65;
        [Min(1)] public int BaseSpeed = 45;

        [Header("Sprites")]
        public Sprite FrontSprite;
        public Sprite BackSprite;

        [Header("Lore")]
        [TextArea] public string LoreText;

        [Header("Evolution")]
        public SpeciesData EvolvesInto;
        [Min(0)] public int EvolveLevel;

        public List<MoveData> LearnableMoves = new List<MoveData>();
        public List<LearnsetEntry> LevelUpLearnset = new List<LearnsetEntry>();

        public List<MoveData> MovesAtLevel(int level)
        {
            var eligible = new List<MoveData>();
            foreach (var e in LevelUpLearnset)
                if (e != null && e.Move != null && e.Level <= level) eligible.Add(e.Move);
            if (eligible.Count <= 4) return eligible;
            return eligible.GetRange(eligible.Count - 4, 4);
        }

        public bool CanEvolveAt(int level)
        {
            return EvolvesInto != null && EvolveLevel > 0 && level >= EvolveLevel;
        }

        public int BaseStat(Stat stat)
        {
            switch (stat)
            {
                case Stat.Hp: return BaseHp;
                case Stat.Attack: return BaseAttack;
                case Stat.Defense: return BaseDefense;
                case Stat.SpAttack: return BaseSpAttack;
                case Stat.SpDefense: return BaseSpDefense;
                case Stat.Speed: return BaseSpeed;
                default: return 1;
            }
        }
    }

    [System.Serializable]
    public sealed class LearnsetEntry
    {
        public int Level;
        public MoveData Move;
    }
}
