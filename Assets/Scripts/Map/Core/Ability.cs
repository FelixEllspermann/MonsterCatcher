namespace MonsterCatcher.Map
{
    public enum AbilityCategory { Buff, Defining }

    // Int mirrors of the Battle-core enums so this pure-C# (engine-free) catalog can
    // reference types/stats/categories/statuses without depending on the Battle assembly.
    // A Battle.Tests sync test asserts these match the real enums.
    public static class T // ElementType
    {
        public const int Normal = 0, Fire = 1, Water = 2, Electric = 3, Grass = 4, Ice = 5,
            Fighting = 6, Poison = 7, Ground = 8, Flying = 9, Psychic = 10, Bug = 11,
            Rock = 12, Ghost = 13, Dragon = 14, Dark = 15, Steel = 16, Fairy = 17;
    }
    public static class S // Stat
    {
        public const int Hp = 0, Attack = 1, Defense = 2, SpAttack = 3, SpDefense = 4, Speed = 5;
    }
    public static class Sc // StatusCondition
    {
        public const int Poison = 1, Burn = 2, Paralysis = 3, Sleep = 4;
    }
    public static class C // MoveCategory
    {
        public const int Physical = 0, Special = 1, Status = 2;
    }

    // Data-driven passive-ability effect palette. Every field defaults to a no-op; the
    // Battle engine reads the relevant fields at its hook points (see AbilityApplier).
    public sealed class AbilityEffect
    {
        // Stat multipliers, index 0=Hp,1=Atk,2=Def,3=SpAtk,4=SpDef,5=Speed (1.0 = unchanged).
        public float[] StatMult = { 1f, 1f, 1f, 1f, 1f, 1f };

        // Outgoing damage
        public float OutgoingMult = 1f;
        public int BoostMoveType = -1; public float BoostMoveTypeMult = 1f;
        public int BoostCategory = -1; public float BoostCategoryMult = 1f;
        public float LowHpDamageMult = 1f;      // user below 1/3 HP
        public float FullHpDamageMult = 1f;     // user at full HP
        public float FirstTurnDamageMult = 1f;  // user's first turn out
        public float FoeLowHpDamageMult = 1f; public float FoeLowHpThreshold = 0f;
        public float VsStatusedDamageMult = 1f; // foe has a status condition
        public float AfterFoeDamageMult = 1f;   // user moved after the foe
        public float SuperEffectiveBonusMult = 1f;
        public float PerFaintedAllyMult = 0f;
        public float RampPerTurn = 0f; public float RampMax = 0f;

        // Incoming damage
        public float IncomingMult = 1f;
        public int ResistType = -1; public float ResistTypeMult = 1f;
        public int ResistCategory = -1; public float ResistCategoryMult = 1f;
        public float SuperEffTakenMult = 1f;
        public float FullHpTakenMult = 1f;
        public float FirstHitTakenMult = 1f;
        public int EarlyTurnsWindow = 0; public float EarlyTurnsTakenMult = 1f;

        // Crit / accuracy
        public float CritChanceMult = 1f;
        public bool LowHpAlwaysCrit = false;
        public float CritDamageMult = 1f;
        public bool CritImmune = false;
        public float AccuracyMult = 1f;
        public bool NeverMiss = false;

        // Status
        public int ImmuneStatus = -1;
        public bool ImmuneAllStatus = false;
        public bool BurnNoChip = false, PoisonNoChip = false;
        public bool ParalysisNoSpeedCut = false;
        public bool ImmuneStatDrops = false;
        public int OnHitInflict = -1; public int OnHitInflictChance = 0;
        public bool OnHitInflictTargetsAttacker = false;

        // Sustain
        public float HealPerTurnFraction = 0f;
        public float DrainAllFraction = 0f;
        public bool RecoilImmune = false; public float RecoilMoveBonusMult = 1f;
        public float OneTimeHealBelowHalf = 0f;

        // Tempo
        public bool StatusMovePriority = false;
        public bool FirstTurnPriority = false;
        public bool GuaranteedFirstTurn1 = false;
        public bool AlwaysMoveLast = false;
        public float LowHpSpeedMult = 1f;

        // On-entry
        public int EntrySelfStat = -1; public int EntrySelfStages = 0;
        public int EntryFoeStat = -1; public int EntryFoeStages = 0;
        public bool EntryDownloadHigherOffense = false;

        // Reactive
        public int OnKoSelfStat = -1; public int OnKoSelfStages = 0;
        public int OnHitTakenSelfStat = -1; public int OnHitTakenSelfStages = 0;
        public float ThornsFraction = 0f;
        public float AftermathFraction = 0f;
        public bool ReviveOnce = false; public float ReviveFraction = 0f;
        public bool SurviveLethalOnce = false;

        // Offensive special
        public bool AllMovesStab = false;
        public bool TintedLens = false;
        public float ExecuteThreshold = 0f;
        public bool SuppressFoeBoosts = false;
    }

    public sealed class AbilityInfo
    {
        public readonly string Id, Name, Description;
        public readonly AbilityCategory Category;
        public readonly AbilityEffect Effect;
        public AbilityInfo(string id, string name, string description,
            AbilityCategory category, AbilityEffect effect)
        { Id = id; Name = name; Description = description; Category = category; Effect = effect; }
    }
}
