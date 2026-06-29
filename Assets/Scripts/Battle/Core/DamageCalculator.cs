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
            MoveData move, BattleSettings settings, IRng rng)
        {
            var result = new DamageResult { Hit = true, Effectiveness = 1.0 };

            if (move.Accuracy > 0 && !rng.Roll(move.Accuracy / 100.0))
            {
                result.Hit = false;
                return result;
            }

            if (move.Category == MoveCategory.Status || move.Power <= 0)
                return result;

            double eff = TypeChart.Effectiveness(move.Type,
                defender.Species.Type1, defender.Species.Type2, defender.Species.HasSecondType);
            result.Effectiveness = eff;
            if (eff <= 0.0)
            {
                result.Damage = 0;
                return result;
            }

            double critChance = move.HighCrit ? settings.CritChance * 8.0 : settings.CritChance;
            if (critChance > 1.0) critChance = 1.0;
            bool crit = rng.Roll(critChance);
            result.WasCritical = crit;

            int a, d;
            if (move.Category == MoveCategory.Physical)
            {
                a = attacker.EffectiveStat(Stat.Attack, crit, ignoreNegative: true);
                d = defender.EffectiveStat(Stat.Defense, crit, ignorePositive: true);
            }
            else
            {
                a = attacker.EffectiveStat(Stat.SpAttack, crit, ignoreNegative: true);
                d = defender.EffectiveStat(Stat.SpDefense, crit, ignorePositive: true);
            }

            double levelTerm = Math.Floor(2.0 * attacker.Level / 5.0) + 2.0;
            double baseDmg = Math.Floor(Math.Floor(levelTerm * move.Power * a / (double)d) / 50.0) + 2.0;

            double mod = 1.0;
            bool stab = move.Type == attacker.Species.Type1 ||
                        (attacker.Species.HasSecondType && move.Type == attacker.Species.Type2);
            if (stab) mod *= 1.5;
            mod *= eff;
            if (crit) mod *= settings.CritMultiplier;
            mod *= rng.IntInclusive(85, 100) / 100.0;
            if (attacker.Status == StatusCondition.Burn && move.Category == MoveCategory.Physical)
                mod *= settings.BurnAttackMultiplier;

            int dmg = (int)Math.Floor(baseDmg * mod);
            result.Damage = dmg < 1 ? 1 : dmg;
            return result;
        }
    }
}
