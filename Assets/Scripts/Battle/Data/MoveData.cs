using UnityEngine;

namespace MonsterCatcher.Battle
{
    [CreateAssetMenu(menuName = "MonsterCatcher/Move", fileName = "Move")]
    public class MoveData : ScriptableObject
    {
        public string DisplayName = "New Move";
        public ElementType Type = ElementType.Normal;
        public MoveCategory Category = MoveCategory.Physical;
        [Min(0)] public int Power = 40;
        [Range(0, 100)] public int Accuracy = 100;   // 0 = never misses
        [Min(1)] public int MaxPp = 35;
        public int Priority = 0;

        [Header("Secondary effect")]
        public StatusCondition InflictsStatus = StatusCondition.None;
        [Range(0, 100)] public int StatusChance = 0;
        public Stat StatToChange = Stat.Attack;
        public int StatStageDelta = 0;
        public bool StatChangeTargetsSelf = false;
        [Range(0, 100)] public int StatChangeChance = 0;

        [Header("Charge")]
        public bool ChargesUp = false;

        [Header("Power effects")]
        [Range(0, 100)] public int RecoilPercent = 0;   // user takes % of damage dealt
        [Range(0, 100)] public int DrainPercent = 0;    // user heals % of damage dealt
        public bool HighCrit = false;                   // boosted critical-hit chance
    }
}
