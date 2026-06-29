using System;
using System.Collections.Generic;

namespace MonsterCatcher.Battle
{
    public sealed class Pokemon
    {
        private readonly int[] _rawStats = new int[6]; // indexed by (int)Stat
        private readonly int[] _stages = new int[6];
        private readonly List<MoveSlot> _moves = new List<MoveSlot>();

        public SpeciesData Species { get; }
        public int Level { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public StatusCondition Status { get; private set; }
        public int SleepTurnsLeft { get; set; }
        public bool Participated;
        public int ChargingMoveIndex = -1;

        public sealed class AbilityRuntimeState
        {
            public bool LastStandUsed, PhoenixUsed, SecondWindUsed, FirstHitTaken;
            public int TurnsOut;
        }

        private readonly List<string> _abilityIds;
        public IReadOnlyList<string> AbilityIds => _abilityIds;
        public AbilityRuntimeState AbilityState { get; } = new AbilityRuntimeState();
        public bool HasAbility(string id) => _abilityIds.Contains(id);

        public IEnumerable<MonsterCatcher.Map.AbilityEffect> AbilityEffects
        {
            get
            {
                foreach (var id in _abilityIds)
                {
                    var info = MonsterCatcher.Map.AbilityCatalog.ById(id);
                    if (info != null) yield return info.Effect;
                }
            }
        }

        public IReadOnlyList<MoveSlot> Moves => _moves;
        public bool IsFainted => CurrentHp <= 0;

        public Pokemon(SpeciesData species, int level, IList<MoveData> moves)
            : this(species, level, moves, null) { }

        public Pokemon(SpeciesData species, int level, IList<MoveData> moves,
            IEnumerable<string> abilityIds)
        {
            Species = species;
            Level = level;
            _abilityIds = abilityIds != null ? new List<string>(abilityIds) : new List<string>();

            int baseMax = (2 * species.BaseHp * level) / 100 + level + 10;
            float hpMult = AbilityApplier.StatMult(this, Stat.Hp);
            MaxHp = (int)(baseMax * hpMult);
            if (MaxHp < 1) MaxHp = 1;
            _rawStats[(int)Stat.Hp] = MaxHp;
            _rawStats[(int)Stat.Attack] = CalcStat(species.BaseAttack, level);
            _rawStats[(int)Stat.Defense] = CalcStat(species.BaseDefense, level);
            _rawStats[(int)Stat.SpAttack] = CalcStat(species.BaseSpAttack, level);
            _rawStats[(int)Stat.SpDefense] = CalcStat(species.BaseSpDefense, level);
            _rawStats[(int)Stat.Speed] = CalcStat(species.BaseSpeed, level);
            CurrentHp = MaxHp;

            if (moves != null)
            {
                foreach (var m in moves)
                {
                    if (m != null) _moves.Add(new MoveSlot(m));
                }
            }
        }

        private static int CalcStat(int baseStat, int level)
        {
            return (2 * baseStat * level) / 100 + 5;
        }

        public int GetRawStat(Stat stat) => _rawStats[(int)stat];
        public int GetStage(Stat stat) => _stages[(int)stat];

        public int EffectiveStat(Stat stat, bool crit = false,
            bool ignoreNegative = false, bool ignorePositive = false)
        {
            int stage = _stages[(int)stat];
            if (crit && ignoreNegative && stage < 0) stage = 0;
            if (crit && ignorePositive && stage > 0) stage = 0;
            double value = _rawStats[(int)stat] * StatStages.Multiplier(stage);
            if (stat != Stat.Hp) value *= AbilityApplier.StatMult(this, stat);
            int result = (int)Math.Floor(value);
            return result < 1 ? 1 : result;
        }

        public int ChangeStage(Stat stat, int delta)
        {
            int before = _stages[(int)stat];
            int after = before + delta;
            if (after < StatStages.Min) after = StatStages.Min;
            if (after > StatStages.Max) after = StatStages.Max;
            _stages[(int)stat] = after;
            return after - before;
        }

        public void ResetStages()
        {
            for (int i = 0; i < _stages.Length; i++) _stages[i] = 0;
        }

        public void TakeDamage(int amount)
        {
            if (amount < 0) amount = 0;
            CurrentHp -= amount;
            if (CurrentHp < 0) CurrentHp = 0;
        }

        public void Heal(int amount)
        {
            if (amount < 0) amount = 0;
            CurrentHp += amount;
            if (CurrentHp > MaxHp) CurrentHp = MaxHp;
        }

        public void SetCurrentHp(int hp)
        {
            if (hp < 0) hp = 0;
            if (hp > MaxHp) hp = MaxHp;
            CurrentHp = hp;
        }

        public bool TryApplyStatus(StatusCondition status, int sleepTurns = 0)
        {
            if (status == StatusCondition.None) return false;
            if (Status != StatusCondition.None || IsFainted) return false;
            Status = status;
            SleepTurnsLeft = status == StatusCondition.Sleep ? sleepTurns : 0;
            return true;
        }

        public void CureStatus()
        {
            Status = StatusCondition.None;
            SleepTurnsLeft = 0;
        }
    }
}
