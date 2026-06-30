using System;

namespace MonsterCatcher.Battle
{
    public struct DamageResult
    {
        public bool Hit;
        public int Damage;
        public double Effectiveness;
        public bool WasCritical;
    }

    public static class DamageCalculator
    {
        public static DamageResult Calculate(Pokemon attacker, Pokemon defender,
            MoveData move, BattleSettings settings, IRng rng,
            int faintedAllies = 0, bool attackerMovedAfter = false)
        {
            var result = new DamageResult { Hit = true, Effectiveness = 1.0 };

            // Accuracy: Deadeye never misses; accuracy abilities scale the hit chance.
            if (!AbilityApplier.NeverMisses(attacker) && move.Accuracy > 0)
            {
                double acc = move.Accuracy / 100.0 * AbilityApplier.AccuracyFactor(attacker);
                if (acc < 1.0 && !rng.Roll(acc)) { result.Hit = false; return result; }
            }

            if (move.Category == MoveCategory.Status || move.Power <= 0)
                return result;

            double eff = TypeChart.Effectiveness(move.Type,
                defender.Species.Type1, defender.Species.Type2, defender.Species.HasSecondType);
            result.Effectiveness = eff;
            if (eff <= 0.0) { result.Damage = 0; return result; }

            // Tinted Lens raises a resisted multiplier toward neutral (damage step only).
            double effForDamage = AbilityApplier.AdjustEffectiveness(attacker, eff);

            double baseCrit = move.HighCrit ? settings.CritChance * 8.0 : settings.CritChance;
            bool crit = AbilityApplier.ForceCrit(attacker) || rng.Roll(AbilityApplier.CritChance(attacker, baseCrit));
            if (AbilityApplier.CritImmune(defender)) crit = false;
            result.WasCritical = crit;

            bool suppress = AbilityApplier.SuppressesFoeBoosts(attacker); // Disruptor ignores foe boosts
            int a, d;
            if (move.Category == MoveCategory.Physical)
            {
                a = attacker.EffectiveStat(Stat.Attack, crit, ignoreNegative: true);
                d = defender.EffectiveStat(Stat.Defense, crit, ignorePositive: true, unconditional: suppress);
            }
            else
            {
                a = attacker.EffectiveStat(Stat.SpAttack, crit, ignoreNegative: true);
                d = defender.EffectiveStat(Stat.SpDefense, crit, ignorePositive: true, unconditional: suppress);
            }

            double levelTerm = Math.Floor(2.0 * attacker.Level / 5.0) + 2.0;
            double baseDmg = Math.Floor(Math.Floor(levelTerm * move.Power * a / (double)d) / 50.0) + 2.0;

            double mod = 1.0;
            bool stab = move.Type == attacker.Species.Type1 ||
                        (attacker.Species.HasSecondType && move.Type == attacker.Species.Type2) ||
                        AbilityApplier.AllMovesStab(attacker);
            if (stab) mod *= AbilityApplier.StabFactor(attacker);     // Titan doubles STAB
            mod *= effForDamage;
            if (crit) mod *= settings.CritMultiplier * AbilityApplier.CritDamageMult(attacker);
            mod *= rng.IntInclusive(85, 100) / 100.0;
            if (attacker.Status == StatusCondition.Burn && move.Category == MoveCategory.Physical)
                mod *= settings.BurnAttackMultiplier;

            // Ability damage multipliers: outgoing x incoming (+ recoil-move bonus).
            double abilityMod = AbilityApplier.OutgoingMultiplier(attacker, defender, move, eff, faintedAllies, attackerMovedAfter)
                              * AbilityApplier.IncomingMultiplier(defender, attacker, move, eff);
            if (move.RecoilPercent > 0) abilityMod *= AbilityApplier.RecoilBonus(attacker);
            mod *= abilityMod;

            int dmg = (int)Math.Floor(baseDmg * mod);
            result.Damage = dmg < 1 ? 1 : dmg;
            return result;
        }
    }
}
