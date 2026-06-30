namespace MonsterCatcher.Battle
{
    public abstract class BattleEvent { }

    public sealed class MoveUsedEvent : BattleEvent
    {
        public readonly Pokemon User; public readonly MoveData Move;
        public MoveUsedEvent(Pokemon user, MoveData move) { User = user; Move = move; }
    }

    public sealed class MissedEvent : BattleEvent
    {
        public readonly Pokemon User; public readonly MoveData Move;
        public MissedEvent(Pokemon user, MoveData move) { User = user; Move = move; }
    }

    public sealed class DamageEvent : BattleEvent
    {
        public readonly Pokemon Target; public readonly int Amount;
        public readonly double Effectiveness; public readonly bool WasCritical;
        public DamageEvent(Pokemon target, int amount, double eff, bool crit)
        { Target = target; Amount = amount; Effectiveness = eff; WasCritical = crit; }
    }

    public sealed class StatusInflictedEvent : BattleEvent
    {
        public readonly Pokemon Target; public readonly StatusCondition Status;
        public StatusInflictedEvent(Pokemon target, StatusCondition status)
        { Target = target; Status = status; }
    }

    public sealed class StatusDamageEvent : BattleEvent
    {
        public readonly Pokemon Target; public readonly StatusCondition Status; public readonly int Amount;
        public StatusDamageEvent(Pokemon target, StatusCondition status, int amount)
        { Target = target; Status = status; Amount = amount; }
    }

    public sealed class StatChangedEvent : BattleEvent
    {
        public readonly Pokemon Target; public readonly Stat Stat; public readonly int DeltaStages;
        public StatChangedEvent(Pokemon target, Stat stat, int delta)
        { Target = target; Stat = stat; DeltaStages = delta; }
    }

    public sealed class ActionPreventedEvent : BattleEvent
    {
        public readonly Pokemon User; public readonly StatusCondition Reason;
        public ActionPreventedEvent(Pokemon user, StatusCondition reason)
        { User = user; Reason = reason; }
    }

    public sealed class FaintedEvent : BattleEvent
    {
        public readonly Pokemon Target;
        public FaintedEvent(Pokemon target) { Target = target; }
    }

    public sealed class SwitchedInEvent : BattleEvent
    {
        public readonly BattleSide Side; public readonly Pokemon Pokemon;
        public SwitchedInEvent(BattleSide side, Pokemon pokemon) { Side = side; Pokemon = pokemon; }
    }

    public sealed class BattleEndedEvent : BattleEvent
    {
        public readonly BattleResult Result;
        public BattleEndedEvent(BattleResult result) { Result = result; }
    }

    public sealed class ChargingEvent : BattleEvent
    {
        public readonly Pokemon User; public readonly MoveData Move;
        public ChargingEvent(Pokemon user, MoveData move) { User = user; Move = move; }
    }

    public sealed class RecoilEvent : BattleEvent
    {
        public readonly Pokemon User; public readonly int Amount;
        public RecoilEvent(Pokemon user, int amount) { User = user; Amount = amount; }
    }

    public sealed class DrainEvent : BattleEvent
    {
        public readonly Pokemon User; public readonly int Amount;
        public DrainEvent(Pokemon user, int amount) { User = user; Amount = amount; }
    }

    public sealed class RevivedEvent : BattleEvent
    {
        public readonly Pokemon Target;
        public RevivedEvent(Pokemon target) { Target = target; }
    }
}
