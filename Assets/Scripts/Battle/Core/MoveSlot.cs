namespace MonsterCatcher.Battle
{
    public sealed class MoveSlot
    {
        public MoveData Move { get; }
        public int MaxPp { get; }
        public int CurrentPp { get; private set; }

        public MoveSlot(MoveData move)
        {
            Move = move;
            MaxPp = move.MaxPp;
            CurrentPp = move.MaxPp;
        }

        public bool HasPp => CurrentPp > 0;

        public bool TryUse()
        {
            if (CurrentPp <= 0) return false;
            CurrentPp--;
            return true;
        }
    }
}
