using UnityEngine;

namespace MonsterCatcher.Battle
{
    [CreateAssetMenu(menuName = "MonsterCatcher/Battle Settings", fileName = "BattleSettings")]
    public class BattleSettings : ScriptableObject
    {
        [Min(1)] public int MaxPartySize = 6;

        [Header("Status")]
        public double PoisonFraction = 1.0 / 8.0;
        public double BurnFraction = 1.0 / 16.0;
        public double BurnAttackMultiplier = 0.5;
        public double ParalysisSpeedMultiplier = 0.5;
        public double ParalysisFailChance = 0.25;
        [Min(1)] public int MinSleepTurns = 1;
        [Min(1)] public int MaxSleepTurns = 3;

        [Header("Critical hits")]
        public double CritChance = 1.0 / 24.0;
        public double CritMultiplier = 1.5;
    }
}
