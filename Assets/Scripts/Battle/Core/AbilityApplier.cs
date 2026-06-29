namespace MonsterCatcher.Battle
{
    // Reads a Pokemon's AbilityEffects at engine hook points. Grown one hook-group at a time.
    public static class AbilityApplier
    {
        // Product of all of a Pokemon's stat multipliers for the given stat.
        public static float StatMult(Pokemon p, Stat stat)
        {
            float m = 1f;
            int idx = (int)stat; // Stat: 0=Hp,1=Attack,2=Defense,3=SpAttack,4=SpDefense,5=Speed
            foreach (var e in p.AbilityEffects) m *= e.StatMult[idx];
            return m;
        }
    }
}
