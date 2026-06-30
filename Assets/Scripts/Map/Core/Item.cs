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
            new ItemInfo("Remedy", "Remedy", "Cure the active monster's status condition.", 15, ItemTarget.ActiveMonster),
            new ItemInfo("Revive", "Revive", "Revive a fainted team member with 50% HP.", 40, ItemTarget.FaintedAlly),
            new ItemInfo("XAttack", "X-Attack", "Raise the active monster's Attack by one stage.", 18, ItemTarget.ActiveMonster),
        };
        private static readonly Dictionary<string, ItemInfo> _byId = Index();

        public static IReadOnlyList<ItemInfo> All => _all;
        public static ItemInfo ById(string id) => id != null && _byId.TryGetValue(id, out var i) ? i : null;

        private static Dictionary<string, ItemInfo> Index()
        {
            var d = new Dictionary<string, ItemInfo>();
            foreach (var i in _all) d[i.Id] = i;
            return d;
        }
    }
}
