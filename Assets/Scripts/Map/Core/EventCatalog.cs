using System.Collections.Generic;

namespace MonsterCatcher.Map
{
    public enum EventCondition { None, TeamAboveOne, HasItems, HasEvolvableNow, HasAnyEvolution }

    public sealed class EventInfo
    {
        public readonly string Id, Name, Description;
        public readonly bool NeedsMonsterTarget;
        public readonly EventCondition Condition;
        public EventInfo(string id, string name, string description, bool needsTarget, EventCondition condition)
        { Id = id; Name = name; Description = description; NeedsMonsterTarget = needsTarget; Condition = condition; }
    }

    public static class EventCatalog
    {
        private static readonly List<EventInfo> _all = new List<EventInfo>
        {
            // --- Pure boons ---
            new EventInfo("MindExpansion", "Mind Expansion", "Open a slot for one more monster on your team.", false, EventCondition.None),
            new EventInfo("AncientAwakening", "Ancient Awakening", "A chosen monster gains a new random ability.", true, EventCondition.None),
            new EventInfo("TrainingGrounds", "Training Grounds", "A chosen monster gains +4 levels.", true, EventCondition.None),
            new EventInfo("WarDrums", "War Drums", "Every monster on your team gains +2 levels.", false, EventCondition.None),
            new EventInfo("SacredSpring", "Sacred Spring", "Fully heal your whole team.", false, EventCondition.None),
            new EventInfo("HiddenCache", "Hidden Cache", "Find 75 gold.", false, EventCondition.None),
            new EventInfo("SupplyDrop", "Supply Drop", "Receive 2 Potions, a Monster Catcher and a Revive.", false, EventCondition.None),
            new EventInfo("EvolutionCatalyst", "Evolution Catalyst", "Evolve a monster that is ready to evolve.", true, EventCondition.HasEvolvableNow),
            new EventInfo("LuckyVein", "Lucky Vein", "Strike it rich and gain 130 gold.", false, EventCondition.None),
            new EventInfo("MentorsGift", "Mentor's Gift", "A chosen monster gains +2 levels and a new random ability.", true, EventCondition.None),
            new EventInfo("TwinDrills", "Twin Drills", "Two random monsters each gain +3 levels.", false, EventCondition.None),
            new EventInfo("Quartermaster", "Quartermaster", "Receive 4 random items.", false, EventCondition.None),

            // --- Trade-offs (good AND bad) ---
            new EventInfo("BloodPact", "Blood Pact", "Your team gains +3 levels, but everyone drops to 50% HP.", false, EventCondition.None),
            new EventInfo("CursedRiches", "Cursed Riches", "Gain 150 gold, but a random monster loses 2 levels.", false, EventCondition.None),
            new EventInfo("ForbiddenTome", "Forbidden Tome", "A chosen monster gains two random abilities, but loses 3 levels.", true, EventCondition.None),
            new EventInfo("SacrificialRite", "Sacrificial Rite", "Release a chosen monster; the survivors each gain +5 levels.", true, EventCondition.TeamAboveOne),
            new EventInfo("DevilsBargain", "Devil's Bargain", "Open a team slot and gain 100 gold, but every monster loses 1 level.", false, EventCondition.None),
            new EventInfo("RecklessEvolution", "Reckless Evolution", "Force-evolve a chosen monster, but it loses 2 levels.", true, EventCondition.HasAnyEvolution),
            new EventInfo("GlassCannonBrew", "Glass Cannon Brew", "A chosen monster gains +6 levels, but is left at 1 HP.", true, EventCondition.None),
            new EventInfo("SoulTax", "Soul Tax", "A chosen monster gains a random ability, but the whole team loses 1 level.", true, EventCondition.None),
            new EventInfo("PawnEverything", "Pawn Everything", "Lose all of one random item type, but gain 100 gold.", false, EventCondition.HasItems),
            new EventInfo("PhoenixRite", "Phoenix Rite", "Fully heal your team, but pay up to 80 gold.", false, EventCondition.None),

            // --- Gambles ---
            new EventInfo("GamblersDice", "Gambler's Dice", "60% chance to win 200 gold, otherwise lose up to 60 gold.", false, EventCondition.None),
            new EventInfo("MysteryBox", "Mystery Box", "50% chance a chosen monster gains two abilities, otherwise it loses 4 levels.", true, EventCondition.None),
        };
        private static readonly Dictionary<string, EventInfo> _byId = Index();

        public static IReadOnlyList<EventInfo> All => _all;
        public static EventInfo ById(string id) => id != null && _byId.TryGetValue(id, out var e) ? e : null;

        // A deterministic distinct pick from the supplied applicable ids (Fisher-Yates shuffle, take count).
        public static List<string> RandomOffer(int seed, int count, IReadOnlyList<string> applicableIds)
        {
            var ids = new List<string>();
            if (applicableIds == null || applicableIds.Count == 0) return ids;
            for (int i = 0; i < applicableIds.Count; i++) ids.Add(applicableIds[i]);
            var rng = new System.Random(seed);
            for (int k = ids.Count - 1; k > 0; k--)
            {
                int j = rng.Next(k + 1);
                var tmp = ids[k]; ids[k] = ids[j]; ids[j] = tmp;
            }
            if (count > ids.Count) count = ids.Count;
            if (count < 0) count = 0;
            return ids.GetRange(0, count);
        }

        private static Dictionary<string, EventInfo> Index()
        {
            var d = new Dictionary<string, EventInfo>();
            foreach (var e in _all) d[e.Id] = e;
            return d;
        }
    }
}
