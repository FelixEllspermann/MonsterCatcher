namespace MonsterCatcher.Battle
{
    public static class StatStages
    {
        public const int Min = -6;
        public const int Max = 6;

        public static double Multiplier(int stage)
        {
            if (stage < Min) stage = Min;
            if (stage > Max) stage = Max;
            return stage >= 0 ? (2.0 + stage) / 2.0 : 2.0 / (2.0 - stage);
        }
    }
}
