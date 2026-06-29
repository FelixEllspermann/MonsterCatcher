using System.Collections.Generic;

namespace MonsterCatcher.Map
{
    // The 110-ability catalog (identity + data-driven effect). Behavior is applied in the
    // Battle core (AbilityApplier). Entries are grown one hook-group at a time during Phase 1.
    public static class AbilityCatalog
    {
        private static readonly List<AbilityInfo> _all = Build();
        private static readonly Dictionary<string, AbilityInfo> _byId = Index(_all);

        public static IReadOnlyList<AbilityInfo> All => _all;
        public static AbilityInfo ById(string id) =>
            id != null && _byId.TryGetValue(id, out var a) ? a : null;

        // Deterministic pick from a seed (no Math.Random — keeps tests stable).
        public static string RollId(int seed)
        {
            int i = (int)((uint)((uint)seed * 2654435761u) % (uint)_all.Count);
            return _all[i].Id;
        }

        private static Dictionary<string, AbilityInfo> Index(List<AbilityInfo> all)
        {
            var d = new Dictionary<string, AbilityInfo>();
            foreach (var a in all) d[a.Id] = a;
            return d;
        }

        // ---- authoring helpers -------------------------------------------------------
        private static AbilityInfo Buff(string id, string name, string desc, AbilityEffect e) =>
            new AbilityInfo(id, name, desc, AbilityCategory.Buff, e);
        private static AbilityInfo Def(string id, string name, string desc, AbilityEffect e) =>
            new AbilityInfo(id, name, desc, AbilityCategory.Defining, e);

        // Stat-multiplier effect: pass (statIndex, multiplier) pairs.
        private static AbilityEffect SM(params (int stat, float mult)[] mods)
        {
            var e = new AbilityEffect();
            foreach (var (stat, mult) in mods) e.StatMult[stat] = mult;
            return e;
        }

        private static List<AbilityInfo> Build()
        {
            var list = new List<AbilityInfo>();

            // ---- Stat-multiplier buffs (Tasks 1-3) -----------------------------------
            list.Add(Buff("Brawler", "Brawler", "+12% Attack", SM((S.Attack, 1.12f))));
            list.Add(Buff("Mystic", "Mystic", "+12% Sp.Attack", SM((S.SpAttack, 1.12f))));
            list.Add(Buff("Bulwark", "Bulwark", "+15% Defense", SM((S.Defense, 1.15f))));
            list.Add(Buff("Warden", "Warden", "+15% Sp.Defense", SM((S.SpDefense, 1.15f))));
            list.Add(Buff("Fleetfoot", "Fleetfoot", "+15% Speed", SM((S.Speed, 1.15f))));
            list.Add(Buff("Stalwart", "Stalwart", "+10% max HP", SM((S.Hp, 1.10f))));
            list.Add(Buff("Vigor", "Vigor", "+8% Attack & Sp.Attack", SM((S.Attack, 1.08f), (S.SpAttack, 1.08f))));
            list.Add(Buff("Turtle", "Turtle", "+10% Defense & Sp.Defense", SM((S.Defense, 1.10f), (S.SpDefense, 1.10f))));
            list.Add(Buff("Powerhouse", "Powerhouse", "+20% Attack", SM((S.Attack, 1.20f))));
            list.Add(Buff("Archmage", "Archmage", "+20% Sp.Attack", SM((S.SpAttack, 1.20f))));
            list.Add(Buff("Fortress", "Fortress", "+25% Defense", SM((S.Defense, 1.25f))));
            list.Add(Buff("Aegis", "Aegis", "+25% Sp.Defense", SM((S.SpDefense, 1.25f))));
            list.Add(Buff("Sprinter", "Sprinter", "+25% Speed", SM((S.Speed, 1.25f))));
            list.Add(Buff("Giant", "Giant", "+20% max HP", SM((S.Hp, 1.20f))));
            list.Add(Buff("Balanced", "Balanced", "+8% to all stats",
                SM((S.Hp, 1.08f), (S.Attack, 1.08f), (S.Defense, 1.08f), (S.SpAttack, 1.08f), (S.SpDefense, 1.08f), (S.Speed, 1.08f))));
            list.Add(Buff("TwinStrike", "Twin Strike", "+10% Attack & Sp.Attack", SM((S.Attack, 1.10f), (S.SpAttack, 1.10f))));
            list.Add(Buff("Stonewall", "Stonewall", "+12% Defense & Sp.Defense", SM((S.Defense, 1.12f), (S.SpDefense, 1.12f))));
            list.Add(Buff("Duelist", "Duelist", "+15% Speed & +10% Attack", SM((S.Speed, 1.15f), (S.Attack, 1.10f))));

            return list;
        }
    }
}
