using System.Collections.Generic;

namespace MonsterCatcher.Map
{
    public enum ItemTarget { ActiveMonster, FaintedAlly, WildEnemy, Self }

    public sealed class ItemInfo
    {
        public readonly string Id, Name, Description;
        public readonly int Price;
        public readonly ItemTarget Target;
        public ItemInfo(string id, string name, string description, int price, ItemTarget target)
        { Id = id; Name = name; Description = description; Price = price; Target = target; }
    }

    public static class ItemCatalog
    {
        private static readonly List<ItemInfo> _all = new List<ItemInfo>
        {
            new ItemInfo("MonsterCatcher", "Monster Catcher", "Catch a wild (non-boss) monster.", 30, ItemTarget.WildEnemy),
            new ItemInfo("Potion", "Potion", "Restore 50% of the active monster's max HP.", 20, ItemTarget.ActiveMonster),
            new ItemInfo("Antidote", "Antidote", "Cure the active monster of Poison.", 12, ItemTarget.ActiveMonster),
            new ItemInfo("BurnHeal", "Burn Heal", "Cure the active monster of Burn.", 12, ItemTarget.ActiveMonster),
            new ItemInfo("ParalyzeHeal", "Paralyze Heal", "Cure the active monster of Paralysis.", 12, ItemTarget.ActiveMonster),
            new ItemInfo("Awakening", "Awakening", "Wake the active monster from Sleep.", 12, ItemTarget.ActiveMonster),
            new ItemInfo("Revive", "Revive", "Revive a fainted team member with 50% HP.", 40, ItemTarget.FaintedAlly),
            new ItemInfo("XAttack", "X-Attack", "Raise the active monster's Attack by one stage.", 18, ItemTarget.ActiveMonster),
        };
        private static readonly Dictionary<string, ItemInfo> _byId = Index();

        public static IReadOnlyList<ItemInfo> All => _all;
        public static ItemInfo ById(string id) => id != null && _byId.TryGetValue(id, out var i) ? i : null;

        // A deterministic random selection of distinct item ids (e.g. a shop's 3 wares).
        public static List<string> RandomOffer(int seed, int count)
        {
            var ids = new List<string>();
            foreach (var i in _all) ids.Add(i.Id);
            var rng = new System.Random(seed);
            for (int k = ids.Count - 1; k > 0; k--)
            {
                int j = rng.Next(k + 1);
                var tmp = ids[k]; ids[k] = ids[j]; ids[j] = tmp;
            }
            if (count > ids.Count) count = ids.Count;
            return ids.GetRange(0, count);
        }

        private static Dictionary<string, ItemInfo> Index()
        {
            var d = new Dictionary<string, ItemInfo>();
            foreach (var i in _all) d[i.Id] = i;
            return d;
        }
    }
}
