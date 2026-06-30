namespace MonsterCatcher.Battle
{
    public enum ActionKind { Move, Switch, Pass }

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

        // The actor does nothing this turn (e.g. it used an item); the opponent still acts.
        public static BattleAction Pass()
        {
            return new BattleAction { Kind = ActionKind.Pass, Index = -1 };
        }
    }
}
