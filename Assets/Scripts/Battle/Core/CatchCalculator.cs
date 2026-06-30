namespace MonsterCatcher.Battle
{
    // Catch success scales with the wild monster's missing HP, with a bonus for status.
    public static class CatchCalculator
    {
        public static double Chance(Pokemon enemy)
        {
            double hpRatio = enemy.MaxHp > 0 ? (double)enemy.CurrentHp / enemy.MaxHp : 1.0;
            double chance = 0.30 + 0.50 * (1.0 - hpRatio) + (enemy.Status != StatusCondition.None ? 0.20 : 0.0);
            if (chance < 0.10) chance = 0.10;
            if (chance > 0.95) chance = 0.95;
            return chance;
        }
    }
}
