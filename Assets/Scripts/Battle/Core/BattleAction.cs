namespace MonsterCatcher.Battle
{
    public enum ActionKind { Move, Switch }

    public struct BattleAction
    {
        public ActionKind Kind;
        public int Index;

        public static BattleAction UseMove(int moveIndex)
        {
            return new BattleAction { Kind = ActionKind.Move, Index = moveIndex };
        }

        public static BattleAction SwitchTo(int partyIndex)
        {
            return new BattleAction { Kind = ActionKind.Switch, Index = partyIndex };
        }
    }
}
