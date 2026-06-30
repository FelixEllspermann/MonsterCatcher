using System;
using System.Collections.Generic;

namespace MonsterCatcher.Battle
{
    // Reads a Pokemon's AbilityEffects at engine hook points. Pure C#, fully unit-tested.
    public static class AbilityApplier
    {
        // Product of all of a Pokemon's stat multipliers for the given stat.
        public static float StatMult(Pokemon p, Stat stat)
        {
            float m = 1f;
            int idx = (int)stat;
            foreach (var e in p.AbilityEffects) m *= e.StatMult[idx];
            return m;
        }

        // ---- Outgoing damage ----------------------------------------------------------
        public static float OutgoingMultiplier(Pokemon attacker, Pokemon defender,
            MoveData move, double effectiveness, int faintedAllies, bool movedAfterFoe)
        {
            float m = 1f;
            foreach (var e in attacker.AbilityEffects)
            {
                m *= e.OutgoingMult;
                if (e.BoostMoveType == (int)move.Type) m *= e.BoostMoveTypeMult;
                if (e.BoostCategory == (int)move.Category) m *= e.BoostCategoryMult;
                if (attacker.CurrentHp * 3 <= attacker.MaxHp) m *= e.LowHpDamageMult;
                if (attacker.CurrentHp == attacker.MaxHp) m *= e.FullHpDamageMult;
                if (attacker.AbilityState.TurnsOut == 0) m *= e.FirstTurnDamageMult;
                if (e.FoeLowHpThreshold > 0f &&
                    defender.CurrentHp <= defender.MaxHp * e.FoeLowHpThreshold)
                    m *= e.FoeLowHpDamageMult;
                if (defender.Status != StatusCondition.None) m *= e.VsStatusedDamageMult;
                if (movedAfterFoe) m *= e.AfterFoeDamageMult;
                if (effectiveness > 1.0) m *= e.SuperEffectiveBonusMult;
                m *= 1f + e.PerFaintedAllyMult * faintedAllies;
                if (e.RampPerTurn > 0f)
                    m *= 1f + Math.Min(e.RampMax, e.RampPerTurn * attacker.AbilityState.TurnsOut);
            }
            return m;
        }

        // ---- Incoming damage ----------------------------------------------------------
        public static float IncomingMultiplier(Pokemon defender, Pokemon attacker, MoveData move, double effectiveness)
        {
            float m = 1f;
            foreach (var e in defender.AbilityEffects)
            {
                m *= e.IncomingMult;
                if (e.ResistType == (int)move.Type) m *= e.ResistTypeMult;
                if (e.ResistCategory == (int)move.Category) m *= e.ResistCategoryMult;
                if (effectiveness > 1) m *= e.SuperEffTakenMult;
                if (defender.CurrentHp == defender.MaxHp) m *= e.FullHpTakenMult;
                if (!defender.AbilityState.FirstHitTaken) m *= e.FirstHitTakenMult;
                if (e.EarlyTurnsWindow > 0 && defender.AbilityState.TurnsOut < e.EarlyTurnsWindow) m *= e.EarlyTurnsTakenMult;
            }
            return m;
        }

        // ---- Crit / accuracy ----------------------------------------------------------
        public static double CritChance(Pokemon attacker, double baseChance)
        {
            double m = 1;
            foreach (var e in attacker.AbilityEffects) m *= e.CritChanceMult;
            double c = baseChance * m;
            return c > 1 ? 1 : c;
        }

        public static bool ForceCrit(Pokemon attacker)
        {
            foreach (var e in attacker.AbilityEffects)
                if (e.LowHpAlwaysCrit && attacker.CurrentHp * 4 <= attacker.MaxHp) return true;
            return false;
        }

        public static bool CritImmune(Pokemon defender)
        {
            foreach (var e in defender.AbilityEffects)
                if (e.CritImmune) return true;
            return false;
        }

        public static double CritDamageMult(Pokemon attacker)
        {
            double m = 1;
            foreach (var e in attacker.AbilityEffects) m *= e.CritDamageMult;
            return m;
        }

        public static double AccuracyFactor(Pokemon attacker)
        {
            double m = 1;
            foreach (var e in attacker.AbilityEffects) m *= e.AccuracyMult;
            return m;
        }

        public static bool NeverMisses(Pokemon attacker)
        {
            foreach (var e in attacker.AbilityEffects)
                if (e.NeverMiss) return true;
            return false;
        }

        // ---- Status -------------------------------------------------------------------
        public static bool ImmuneToStatus(Pokemon p, StatusCondition s)
        {
            foreach (var e in p.AbilityEffects)
                if (e.ImmuneAllStatus || e.ImmuneStatus == (int)s) return true;
            return false;
        }

        public static bool BurnChipBlocked(Pokemon p)
        {
            foreach (var e in p.AbilityEffects) if (e.BurnNoChip) return true;
            return false;
        }

        public static bool PoisonChipBlocked(Pokemon p)
        {
            foreach (var e in p.AbilityEffects) if (e.PoisonNoChip) return true;
            return false;
        }

        public static bool ParalysisSpeedImmune(Pokemon p)
        {
            foreach (var e in p.AbilityEffects) if (e.ParalysisNoSpeedCut) return true;
            return false;
        }

        public static bool ImmuneToStatDrops(Pokemon p)
        {
            foreach (var e in p.AbilityEffects) if (e.ImmuneStatDrops) return true;
            return false;
        }

        public static double SecondaryChanceMult(Pokemon p)
        {
            double m = 1.0;
            foreach (var e in p.AbilityEffects) m *= e.SecondaryChanceMult;
            return m;
        }

        public static void OnHitInflict(Pokemon attacker, Pokemon defender, IRng rng, List<BattleEvent> events)
        {
            foreach (var e in attacker.AbilityEffects)
            {
                if (e.OnHitInflict < 0 || e.OnHitInflictChance <= 0) continue;
                if (rng.Roll(e.OnHitInflictChance / 100.0))
                {
                    var target = e.OnHitInflictTargetsAttacker ? attacker : defender;
                    if (target.TryApplyStatus((StatusCondition)e.OnHitInflict))
                        events.Add(new StatusInflictedEvent(target, (StatusCondition)e.OnHitInflict));
                }
            }
        }

        // ---- Sustain ------------------------------------------------------------------
        public static int EndOfTurnHeal(Pokemon p)
        {
            int total = 0;
            foreach (var e in p.AbilityEffects)
                if (e.HealPerTurnFraction > 0f)
                {
                    int h = (int)(p.MaxHp * e.HealPerTurnFraction);
                    if (h < 1) h = 1;
                    total += h;
                }
            return total;
        }

        public static int DrainAmount(Pokemon attacker, int damageDealt)
        {
            int best = 0;
            foreach (var e in attacker.AbilityEffects)
                if (e.DrainAllFraction > 0f)
                {
                    int d = (int)(damageDealt * e.DrainAllFraction);
                    if (d > best) best = d;
                }
            return best;
        }

        public static bool RecoilImmune(Pokemon p)
        {
            foreach (var e in p.AbilityEffects) if (e.RecoilImmune) return true;
            return false;
        }

        public static float RecoilBonus(Pokemon p)
        {
            float m = 1f;
            foreach (var e in p.AbilityEffects) m *= e.RecoilMoveBonusMult;
            return m;
        }

        public static int OneTimeHeal(Pokemon p)
        {
            if (p.AbilityState.SecondWindUsed) return 0;
            if (p.CurrentHp * 2 >= p.MaxHp) return 0;
            float frac = 0f;
            foreach (var e in p.AbilityEffects)
                if (e.OneTimeHealBelowHalf > frac) frac = e.OneTimeHealBelowHalf;
            if (frac <= 0f) return 0;
            p.AbilityState.SecondWindUsed = true;
            return (int)(p.MaxHp * frac);
        }

        // ---- Tempo --------------------------------------------------------------------
        public static int PriorityBonus(Pokemon p, MoveData move)
        {
            int bonus = 0;
            foreach (var e in p.AbilityEffects)
            {
                if (e.StatusMovePriority && move != null && move.Category == MoveCategory.Status) bonus += 1;
                if (e.FirstTurnPriority && p.AbilityState.TurnsOut == 0) bonus += 1;
            }
            return bonus;
        }

        public static bool ForcesFirst(Pokemon p)
        {
            foreach (var e in p.AbilityEffects)
                if (e.GuaranteedFirstTurn1 && p.AbilityState.TurnsOut == 0) return true;
            return false;
        }

        public static bool ForcesLast(Pokemon p)
        {
            foreach (var e in p.AbilityEffects)
                if (e.AlwaysMoveLast) return true;
            return false;
        }

        public static double SpeedFactor(Pokemon p)
        {
            double m = 1.0;
            bool lowHp = p.CurrentHp * 3 <= p.MaxHp;
            foreach (var e in p.AbilityEffects)
                if (lowHp) m *= e.LowHpSpeedMult;
            return m;
        }

        // ---- On-entry -----------------------------------------------------------------
        public static void OnEntry(Pokemon entering, Pokemon opponent, List<BattleEvent> events = null)
        {
            foreach (var e in entering.AbilityEffects)
            {
                if (e.EntrySelfStat >= 0)
                {
                    int d = entering.ChangeStage((Stat)e.EntrySelfStat, e.EntrySelfStages);
                    if (events != null && d != 0)
                        events.Add(new StatChangedEvent(entering, (Stat)e.EntrySelfStat, d));
                }
                if (e.EntryFoeStat >= 0)
                {
                    int d = opponent.ChangeStage((Stat)e.EntryFoeStat, e.EntryFoeStages);
                    if (events != null && d != 0)
                        events.Add(new StatChangedEvent(opponent, (Stat)e.EntryFoeStat, d));
                }
                if (e.EntryDownloadHigherOffense)
                {
                    Stat s = entering.EffectiveStat(Stat.Attack) >= entering.EffectiveStat(Stat.SpAttack)
                        ? Stat.Attack : Stat.SpAttack;
                    int d = entering.ChangeStage(s, 1);
                    if (events != null && d != 0)
                        events.Add(new StatChangedEvent(entering, s, d));
                }
            }
        }

        // ---- Reactive -----------------------------------------------------------------
        public static bool TrySurviveLethal(Pokemon p)
        {
            if (p.AbilityState.LastStandUsed) return false;
            foreach (var e in p.AbilityEffects)
                if (e.SurviveLethalOnce)
                {
                    p.AbilityState.LastStandUsed = true;
                    p.SetCurrentHp(1);
                    return true;
                }
            return false;
        }

        public static bool TryRevive(Pokemon p)
        {
            if (p.AbilityState.PhoenixUsed) return false;
            foreach (var e in p.AbilityEffects)
                if (e.ReviveOnce)
                {
                    p.AbilityState.PhoenixUsed = true;
                    p.SetCurrentHp(Math.Max(1, (int)(p.MaxHp * e.ReviveFraction)));
                    return true;
                }
            return false;
        }

        public static void OnDealtDamage(Pokemon attacker, Pokemon defender, List<BattleEvent> events)
        {
            foreach (var e in defender.AbilityEffects)
            {
                if (e.ThornsFraction > 0f)
                {
                    int t = Math.Max(1, (int)(attacker.MaxHp * e.ThornsFraction));
                    attacker.TakeDamage(t);
                    events.Add(new StatusDamageEvent(attacker, StatusCondition.None, t));
                }
                if (e.OnHitTakenSelfStat >= 0)
                {
                    int d = defender.ChangeStage((Stat)e.OnHitTakenSelfStat, e.OnHitTakenSelfStages);
                    if (d != 0) events.Add(new StatChangedEvent(defender, (Stat)e.OnHitTakenSelfStat, d));
                }
            }
        }

        public static void OnKo(Pokemon victor, List<BattleEvent> events)
        {
            foreach (var e in victor.AbilityEffects)
                if (e.OnKoSelfStat >= 0)
                {
                    int d = victor.ChangeStage((Stat)e.OnKoSelfStat, e.OnKoSelfStages);
                    if (d != 0) events.Add(new StatChangedEvent(victor, (Stat)e.OnKoSelfStat, d));
                }
        }

        public static void OnFaint(Pokemon fainter, Pokemon opponent, List<BattleEvent> events)
        {
            foreach (var e in fainter.AbilityEffects)
                if (e.AftermathFraction > 0f)
                {
                    int a = Math.Max(1, (int)(opponent.MaxHp * e.AftermathFraction));
                    opponent.TakeDamage(a);
                    events.Add(new StatusDamageEvent(opponent, StatusCondition.None, a));
                }
        }

        // ---- Offensive special --------------------------------------------------------
        public static bool AllMovesStab(Pokemon attacker)
        {
            foreach (var e in attacker.AbilityEffects) if (e.AllMovesStab) return true;
            return false;
        }

        public static double StabFactor(Pokemon attacker)
        {
            foreach (var e in attacker.AbilityEffects) if (e.DoubleStab) return 2.0;
            return 1.5;
        }

        public static double AdjustEffectiveness(Pokemon attacker, double eff)
        {
            foreach (var e in attacker.AbilityEffects)
                if (e.TintedLens && eff > 0 && eff < 1) return 1.0;
            return eff;
        }

        public static bool ExecutesFoe(Pokemon attacker, Pokemon defender)
        {
            foreach (var e in attacker.AbilityEffects)
                if (e.ExecuteThreshold > 0 && defender.CurrentHp <= defender.MaxHp * e.ExecuteThreshold)
                    return true;
            return false;
        }

        public static bool SuppressesFoeBoosts(Pokemon p)
        {
            foreach (var e in p.AbilityEffects) if (e.SuppressFoeBoosts) return true;
            return false;
        }
    }
}
