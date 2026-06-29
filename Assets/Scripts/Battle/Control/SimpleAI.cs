using System.Collections.Generic;

namespace MonsterCatcher.Battle
{
    public sealed class SimpleAI
    {
        private readonly double _switchHpThreshold;
        private readonly double _dangerEffectiveness;

        public SimpleAI(double switchHpThreshold = 0.3, double dangerEffectiveness = 2.0)
        {
            _switchHpThreshold = switchHpThreshold;
            _dangerEffectiveness = dangerEffectiveness;
        }

        public BattleAction ChooseAction(Party self, Party opponent)
        {
            var active = self.Active;
            var foe = opponent.Active;

            double incoming = ThreatAgainst(active, foe);
            double hpRatio = (double)active.CurrentHp / active.MaxHp;

            if (hpRatio < _switchHpThreshold && incoming >= _dangerEffectiveness)
            {
                int best = -1;
                double bestThreat = incoming;
                for (int i = 0; i < self.Members.Count; i++)
                {
                    if (!self.CanSwitchTo(i)) continue;
                    double t = ThreatAgainst(self.Members[i], foe);
                    if (t < bestThreat)
                    {
                        bestThreat = t;
                        best = i;
                    }
                }
                if (best >= 0) return BattleAction.SwitchTo(best);
            }

            return BattleAction.UseMove(BestMoveIndex(active, foe));
        }

        // Highest effectiveness the foe's typing has against 'mon'.
        private static double ThreatAgainst(Pokemon mon, Pokemon foe)
        {
            double a = TypeChart.Effectiveness(foe.Species.Type1,
                mon.Species.Type1, mon.Species.Type2, mon.Species.HasSecondType);
            if (!foe.Species.HasSecondType) return a;
            double b = TypeChart.Effectiveness(foe.Species.Type2,
                mon.Species.Type1, mon.Species.Type2, mon.Species.HasSecondType);
            return a > b ? a : b;
        }

        private static int BestMoveIndex(Pokemon attacker, Pokemon defender)
        {
            int bestIndex = 0;
            double bestScore = -1.0;
            IReadOnlyList<MoveSlot> moves = attacker.Moves;
            for (int i = 0; i < moves.Count; i++)
            {
                var move = moves[i].Move;
                if (!moves[i].HasPp) continue;
                double score = 0.0;
                if (move.Category != MoveCategory.Status && move.Power > 0)
                {
                    double eff = TypeChart.Effectiveness(move.Type,
                        defender.Species.Type1, defender.Species.Type2, defender.Species.HasSecondType);
                    bool stab = move.Type == attacker.Species.Type1 ||
                                (attacker.Species.HasSecondType && move.Type == attacker.Species.Type2);
                    score = move.Power * eff * (stab ? 1.5 : 1.0);
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }
    }
}
