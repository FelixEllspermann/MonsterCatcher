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

            // ---- Outgoing-damage abilities -------------------------------------------
            list.Add(Buff("Bruiser", "Bruiser", "+10% damage dealt", new AbilityEffect { OutgoingMult = 1.10f }));
            list.Add(Def("Berserk", "Berserk", "+50% damage at low HP", new AbilityEffect { LowHpDamageMult = 1.5f }));
            list.Add(Buff("Vanguard", "Vanguard", "+10% damage at full HP", new AbilityEffect { FullHpDamageMult = 1.10f }));
            list.Add(Buff("EarlyBird", "Early Bird", "+20% damage on its first turn out", new AbilityEffect { FirstTurnDamageMult = 1.20f }));
            list.Add(Buff("Opportunist", "Opportunist", "+15% damage vs foes at or below half HP", new AbilityEffect { FoeLowHpDamageMult = 1.15f, FoeLowHpThreshold = .5f }));
            list.Add(Buff("Finisher", "Finisher", "+25% damage vs foes at or below quarter HP", new AbilityEffect { FoeLowHpDamageMult = 1.25f, FoeLowHpThreshold = .25f }));
            list.Add(Def("Bully", "Bully", "+30% damage vs statused foes", new AbilityEffect { VsStatusedDamageMult = 1.30f }));
            list.Add(Buff("Counterpunch", "Counterpunch", "+15% damage when moving after the foe", new AbilityEffect { AfterFoeDamageMult = 1.15f }));
            list.Add(Def("Tyrant", "Tyrant", "+50% super-effective bonus damage", new AbilityEffect { SuperEffectiveBonusMult = 1.5f }));
            list.Add(Def("Comeback", "Comeback", "+15% damage per fainted ally", new AbilityEffect { PerFaintedAllyMult = .15f }));
            list.Add(Def("Momentum", "Momentum", "+10% damage each turn out (up to +40%)", new AbilityEffect { RampPerTurn = .10f, RampMax = .40f }));
            list.Add(Def("GlassCannon", "Glass Cannon", "+30% damage dealt but +15% damage taken", new AbilityEffect { OutgoingMult = 1.30f, IncomingMult = 1.15f }));
            list.Add(Buff("Pyromaniac", "Pyromaniac", "+20% Fire move damage", new AbilityEffect { BoostMoveType = T.Fire, BoostMoveTypeMult = 1.20f }));
            list.Add(Buff("Galvanize", "Galvanize", "+20% Electric move damage", new AbilityEffect { BoostMoveType = T.Electric, BoostMoveTypeMult = 1.20f }));
            list.Add(Buff("Naturalist", "Naturalist", "+20% Grass move damage", new AbilityEffect { BoostMoveType = T.Grass, BoostMoveTypeMult = 1.20f }));
            list.Add(Buff("Moonblessed", "Moonblessed", "+20% Fairy move damage", new AbilityEffect { BoostMoveType = T.Fairy, BoostMoveTypeMult = 1.20f }));
            list.Add(Buff("Scrappy", "Scrappy", "+20% Normal move damage", new AbilityEffect { BoostMoveType = T.Normal, BoostMoveTypeMult = 1.20f }));
            list.Add(Buff("Aquatic", "Aquatic", "+20% Water move damage", new AbilityEffect { BoostMoveType = T.Water, BoostMoveTypeMult = 1.20f }));
            list.Add(Buff("Frostbite", "Frostbite", "+20% Ice move damage", new AbilityEffect { BoostMoveType = T.Ice, BoostMoveTypeMult = 1.20f }));
            list.Add(Buff("Brawl", "Brawl", "+20% Fighting move damage", new AbilityEffect { BoostMoveType = T.Fighting, BoostMoveTypeMult = 1.20f }));
            list.Add(Buff("Venomous", "Venomous", "+20% Poison move damage", new AbilityEffect { BoostMoveType = T.Poison, BoostMoveTypeMult = 1.20f }));
            list.Add(Buff("Nightfall", "Nightfall", "+20% Dark move damage", new AbilityEffect { BoostMoveType = T.Dark, BoostMoveTypeMult = 1.20f }));
            list.Add(Buff("IronFist", "Iron Fist", "+12% Physical move damage", new AbilityEffect { BoostCategory = C.Physical, BoostCategoryMult = 1.12f }));
            list.Add(Buff("MysticSurge", "Mystic Surge", "+12% Special move damage", new AbilityEffect { BoostCategory = C.Special, BoostCategoryMult = 1.12f }));

            // ---- Incoming-damage abilities -------------------------------------------
            list.Add(Buff("Thickhide", "Thickhide", "Takes 12% less damage", new AbilityEffect { IncomingMult = 0.88f }));
            list.Add(Buff("Resolute", "Resolute", "Takes 10% less damage", new AbilityEffect { IncomingMult = 0.90f }));
            list.Add(Buff("Heatproof", "Heatproof", "Halves damage from Fire moves", new AbilityEffect { ResistType = T.Fire, ResistTypeMult = 0.5f }));
            list.Add(Buff("Waterproof", "Waterproof", "Halves damage from Water moves", new AbilityEffect { ResistType = T.Water, ResistTypeMult = 0.5f }));
            list.Add(Buff("Grounded", "Grounded", "Halves damage from Electric moves", new AbilityEffect { ResistType = T.Electric, ResistTypeMult = 0.5f }));
            list.Add(Buff("Frostward", "Frostward", "Halves damage from Ice moves", new AbilityEffect { ResistType = T.Ice, ResistTypeMult = 0.5f }));
            list.Add(Buff("Shade", "Shade", "Halves damage from Dark moves", new AbilityEffect { ResistType = T.Dark, ResistTypeMult = 0.5f }));
            list.Add(Buff("Plated", "Plated", "Takes 15% less from physical moves", new AbilityEffect { ResistCategory = C.Physical, ResistCategoryMult = 0.85f }));
            list.Add(Buff("Veiled", "Veiled", "Takes 15% less from special moves", new AbilityEffect { ResistCategory = C.Special, ResistCategoryMult = 0.85f }));
            list.Add(Def("Anvil", "Anvil", "Takes 30% less from physical moves", new AbilityEffect { ResistCategory = C.Physical, ResistCategoryMult = 0.70f }));
            list.Add(Def("Cushion", "Cushion", "Takes 30% less from special moves", new AbilityEffect { ResistCategory = C.Special, ResistCategoryMult = 0.70f }));
            list.Add(Buff("Scales", "Scales", "Takes 10% less damage", new AbilityEffect { IncomingMult = 0.90f }));
            list.Add(Def("Shellguard", "Shellguard", "Takes 25% less from super-effective hits", new AbilityEffect { SuperEffTakenMult = 0.75f }));
            list.Add(Def("Multiscale", "Multiscale", "Halves damage taken at full HP", new AbilityEffect { FullHpTakenMult = 0.5f }));
            list.Add(Buff("Toughness", "Toughness", "Takes 10% less damage at full HP", new AbilityEffect { FullHpTakenMult = 0.90f }));
            list.Add(Buff("Featherfall", "Featherfall", "Takes 20% less from the first hit", new AbilityEffect { FirstHitTakenMult = 0.80f }));
            list.Add(Def("Fortified", "Fortified", "Takes 40% less damage for its first two turns", new AbilityEffect { EarlyTurnsWindow = 2, EarlyTurnsTakenMult = 0.60f }));

            // ---- Crit / accuracy abilities -------------------------------------------
            list.Add(Buff("Keen", "Keen", "Triples critical-hit chance", new AbilityEffect { CritChanceMult = 3f }));
            list.Add(Buff("LuckyStrike", "Lucky Strike", "Doubles critical-hit chance", new AbilityEffect { CritChanceMult = 2f }));
            list.Add(Def("Sniper", "Sniper", "+50% critical-hit damage", new AbilityEffect { CritDamageMult = 1.5f }));
            list.Add(Def("Unbreakable", "Unbreakable", "Cannot be hit by critical hits", new AbilityEffect { CritImmune = true }));
            list.Add(Def("Avenger", "Avenger", "Always lands critical hits at low HP", new AbilityEffect { LowHpAlwaysCrit = true }));
            list.Add(Buff("HawkEye", "Hawk Eye", "+20% accuracy", new AbilityEffect { AccuracyMult = 1.20f }));
            list.Add(Buff("FocusedAim", "Focused Aim", "+15% accuracy", new AbilityEffect { AccuracyMult = 1.15f }));
            list.Add(Buff("Nimble", "Nimble", "+10% accuracy & Speed", new AbilityEffect { AccuracyMult = 1.10f, StatMult = new[] { 1f, 1f, 1f, 1f, 1f, 1.10f } }));
            list.Add(Def("Deadeye", "Deadeye", "Moves never miss", new AbilityEffect { NeverMiss = true }));

            // ---- Status abilities ----------------------------------------------------
            list.Add(Buff("Limber", "Limber", "Immune to paralysis", new AbilityEffect { ImmuneStatus = Sc.Paralysis }));
            list.Add(Buff("Stoic", "Stoic", "Immune to burn", new AbilityEffect { ImmuneStatus = Sc.Burn }));
            list.Add(Buff("WideAwake", "Wide Awake", "Immune to sleep", new AbilityEffect { ImmuneStatus = Sc.Sleep }));
            list.Add(Buff("Antibody", "Antibody", "Immune to poison", new AbilityEffect { ImmuneStatus = Sc.Poison }));
            list.Add(Buff("Bloom", "Bloom", "Immune to poison and heals 4% max HP each turn", new AbilityEffect { ImmuneStatus = Sc.Poison, HealPerTurnFraction = 0.04f }));
            list.Add(Buff("Cozy", "Cozy", "Immune to burn and takes 25% less Fire damage", new AbilityEffect { ImmuneStatus = Sc.Burn, ResistType = T.Fire, ResistTypeMult = 0.75f }));
            list.Add(Buff("FeverWard", "Fever Ward", "Takes no burn chip damage", new AbilityEffect { BurnNoChip = true }));
            list.Add(Buff("ShakeItOff", "Shake It Off", "Paralysis does not cut Speed", new AbilityEffect { ParalysisNoSpeedCut = true }));
            list.Add(Buff("HardyMind", "Hardy Mind", "Immune to stat drops", new AbilityEffect { ImmuneStatDrops = true }));
            list.Add(Def("Venomtouch", "Venomtouch", "30% chance to poison the foe on hit", new AbilityEffect { OnHitInflict = Sc.Poison, OnHitInflictChance = 30 }));
            list.Add(Def("StaticBody", "Static Body", "30% chance to paralyze the attacker when hit", new AbilityEffect { OnHitInflict = Sc.Paralysis, OnHitInflictChance = 30, OnHitInflictTargetsAttacker = true }));
            list.Add(Buff("LuckyCharm", "Lucky Charm", "+50% secondary-effect chance", new AbilityEffect { SecondaryChanceMult = 1.5f }));

            // ---- Sustain abilities ---------------------------------------------------
            list.Add(Buff("Regrowth", "Regrowth", "Heals 6.25% max HP each turn", new AbilityEffect { HealPerTurnFraction = .0625f }));
            list.Add(Buff("Mending", "Mending", "Heals 4% max HP each turn", new AbilityEffect { HealPerTurnFraction = .04f }));
            list.Add(Def("Siphon", "Siphon", "Drains 15% of damage dealt as HP", new AbilityEffect { DrainAllFraction = .15f }));
            list.Add(Buff("Vampiric", "Vampiric", "Drains 8% of damage dealt as HP", new AbilityEffect { DrainAllFraction = .08f }));
            list.Add(Def("Reckless", "Reckless", "Immune to recoil; +20% recoil-move damage", new AbilityEffect { RecoilImmune = true, RecoilMoveBonusMult = 1.20f }));
            list.Add(Buff("SecondWind", "Second Wind", "Once per battle, heals 10% max HP when first dropping below half HP", new AbilityEffect { OneTimeHealBelowHalf = .10f }));

            // ---- Tempo abilities -----------------------------------------------------
            list.Add(Def("Trickster", "Trickster", "Status moves gain +1 priority", new AbilityEffect { StatusMovePriority = true }));
            list.Add(Def("PhantomStep", "Phantom Step", "All moves gain +1 priority on the first turn out", new AbilityEffect { FirstTurnPriority = true }));
            list.Add(Def("TimeWarp", "Time Warp", "Guaranteed to move first on the first turn out", new AbilityEffect { GuaranteedFirstTurn1 = true }));
            list.Add(Def("Reversal", "Reversal", "Always moves last, but deals +40% damage", new AbilityEffect { AlwaysMoveLast = true, OutgoingMult = 1.40f }));
            list.Add(Def("Adrenaline", "Adrenaline", "+50% Speed when at or below 1/3 HP", new AbilityEffect { LowHpSpeedMult = 1.5f }));

            // ---- On-entry abilities --------------------------------------------------
            list.Add(Def("Intimidate", "Intimidate", "On entry, lowers the foe's Attack", new AbilityEffect { EntryFoeStat = S.Attack, EntryFoeStages = -1 }));
            list.Add(Def("BattleCry", "Battle Cry", "On entry, raises the user's Attack", new AbilityEffect { EntrySelfStat = S.Attack, EntrySelfStages = 1 }));
            list.Add(Def("Overclock", "Overclock", "On entry, raises the user's Sp.Attack", new AbilityEffect { EntrySelfStat = S.SpAttack, EntrySelfStages = 1 }));
            list.Add(Buff("WarmUp", "Warm Up", "On entry, raises the user's Speed", new AbilityEffect { EntrySelfStat = S.Speed, EntrySelfStages = 1 }));
            list.Add(Buff("GuardUp", "Guard Up", "On entry, raises the user's Defense", new AbilityEffect { EntrySelfStat = S.Defense, EntrySelfStages = 1 }));
            list.Add(Buff("Brace", "Brace", "On entry, raises the user's Sp.Defense", new AbilityEffect { EntrySelfStat = S.SpDefense, EntrySelfStages = 1 }));
            list.Add(Def("Download", "Download", "On entry, raises the user's higher offense by one stage", new AbilityEffect { EntryDownloadHigherOffense = true }));

            // ---- Reactive abilities --------------------------------------------------
            list.Add(Def("Moxie", "Moxie", "On KO, raises Attack", new AbilityEffect { OnKoSelfStat = S.Attack, OnKoSelfStages = 1 }));
            list.Add(Def("Steadfast", "Steadfast", "When hit, raises Speed", new AbilityEffect { OnHitTakenSelfStat = S.Speed, OnHitTakenSelfStages = 1 }));
            list.Add(Def("Rage", "Rage", "When hit, raises Attack", new AbilityEffect { OnHitTakenSelfStat = S.Attack, OnHitTakenSelfStages = 1 }));
            list.Add(Def("Thorns", "Thorns", "Attackers take 1/8 of their max HP when they hit this Pokemon", new AbilityEffect { ThornsFraction = .125f }));
            list.Add(Def("Aftermath", "Aftermath", "On fainting, deals 1/4 of the foe's max HP", new AbilityEffect { AftermathFraction = .25f }));
            list.Add(Def("LastStand", "Last Stand", "Survives one otherwise-lethal hit with 1 HP", new AbilityEffect { SurviveLethalOnce = true }));
            list.Add(Def("Phoenix", "Phoenix", "Once per battle, revives with 33% HP after fainting", new AbilityEffect { ReviveOnce = true, ReviveFraction = .33f }));

            // ---- Offensive-special abilities -----------------------------------------
            list.Add(Def("Adaptability", "Adaptability", "Every move gets the STAB bonus", new AbilityEffect { AllMovesStab = true }));
            list.Add(Def("TintedLens", "Tinted Lens", "\"Not very effective\" hits land at neutral power", new AbilityEffect { TintedLens = true }));
            list.Add(Def("Titan", "Titan", "Doubles the STAB bonus (x2 instead of x1.5)", new AbilityEffect { DoubleStab = true }));
            list.Add(Def("Executioner", "Executioner", "Can finish foes at or below 20% HP", new AbilityEffect { ExecuteThreshold = 0.20f }));
            list.Add(Def("Disruptor", "Disruptor", "Ignores the foe's stat boosts", new AbilityEffect { SuppressFoeBoosts = true }));

            return list;
        }
    }
}
