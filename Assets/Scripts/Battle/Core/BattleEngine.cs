using System;
using System.Collections.Generic;

namespace MonsterCatcher.Battle
{
    public sealed class BattleEngine
    {
        private readonly BattleSettings _settings;
        private readonly IRng _rng;
        private readonly bool[] _forcedSwitch = new bool[2]; // index by (int)BattleSide

        public Party Player { get; }
        public Party Enemy { get; }
        public BattleResult Result { get; private set; } = BattleResult.InProgress;
        public bool IsOver => Result != BattleResult.InProgress;

        public BattleEngine(Party player, Party enemy, BattleSettings settings, IRng rng)
        {
            Player = player;
            Enemy = enemy;
            _settings = settings;
            _rng = rng;
            Player.Active.Participated = true;
            Enemy.Active.Participated = true;
        }

        public bool AwaitingForcedSwitch(BattleSide side) => _forcedSwitch[(int)side];

        private Party PartyOf(BattleSide side) => side == BattleSide.Player ? Player : Enemy;
        private Party OpponentOf(BattleSide side) => side == BattleSide.Player ? Enemy : Player;

        public IReadOnlyList<BattleEvent> ExecuteTurn(BattleAction playerAction, BattleAction enemyAction)
        {
            if (IsOver) throw new InvalidOperationException("Battle is over.");
            if (_forcedSwitch[0] || _forcedSwitch[1])
                throw new InvalidOperationException("A forced switch is pending.");

            var events = new List<BattleEvent>();

            // Charging Pokemon are locked into their charge move.
            if (Player.Active.ChargingMoveIndex >= 0) playerAction = BattleAction.UseMove(Player.Active.ChargingMoveIndex);
            if (Enemy.Active.ChargingMoveIndex >= 0) enemyAction = BattleAction.UseMove(Enemy.Active.ChargingMoveIndex);

            // 1. Switches resolve first.
            var pending = new List<(BattleSide side, BattleAction action)>();
            ApplyOrQueue(BattleSide.Player, playerAction, events, pending);
            ApplyOrQueue(BattleSide.Enemy, enemyAction, events, pending);

            // 2. Order remaining move actions by priority, then effective speed.
            pending.Sort(CompareOrder);

            // 3. Execute moves.
            foreach (var entry in pending)
            {
                var user = PartyOf(entry.side).Active;
                if (user.IsFainted) continue;
                ExecuteMove(entry.side, entry.action.Index, events);
                if (IsOver) return events;
            }

            // 4. End-of-turn status damage (player first, then enemy).
            EndOfTurnStatus(BattleSide.Player, events);
            if (IsOver) return events;
            EndOfTurnStatus(BattleSide.Enemy, events);
            if (IsOver) return events;

            // 5. Flag forced switches for fainted actives with replacements.
            FlagForcedSwitchIfNeeded(BattleSide.Player);
            FlagForcedSwitchIfNeeded(BattleSide.Enemy);

            return events;
        }

        private void ApplyOrQueue(BattleSide side, BattleAction action,
            List<BattleEvent> events, List<(BattleSide side, BattleAction action)> pending)
        {
            if (action.Kind == ActionKind.Switch)
            {
                var party = PartyOf(side);
                if (party.CanSwitchTo(action.Index))
                {
                    party.SwitchTo(action.Index);
                    party.Active.Participated = true;
                    events.Add(new SwitchedInEvent(side, party.Active));
                }
            }
            else
            {
                pending.Add((side, action));
            }
        }

        private int CompareOrder((BattleSide side, BattleAction action) x,
            (BattleSide side, BattleAction action) y)
        {
            int px = MovePriority(x.side, x.action.Index);
            int py = MovePriority(y.side, y.action.Index);
            if (px != py) return py.CompareTo(px); // higher priority first

            int sx = EffectiveSpeed(x.side);
            int sy = EffectiveSpeed(y.side);
            if (sx != sy) return sy.CompareTo(sx); // faster first

            return _rng.Roll(0.5) ? -1 : 1; // tie
        }

        private int MovePriority(BattleSide side, int moveIndex)
        {
            var moves = PartyOf(side).Active.Moves;
            if (moveIndex < 0 || moveIndex >= moves.Count) return 0;
            return moves[moveIndex].Move.Priority;
        }

        private int EffectiveSpeed(BattleSide side)
        {
            var p = PartyOf(side).Active;
            int speed = p.EffectiveStat(Stat.Speed);
            if (p.Status == StatusCondition.Paralysis)
                speed = (int)Math.Floor(speed * _settings.ParalysisSpeedMultiplier);
            return speed < 1 ? 1 : speed;
        }

        private void ExecuteMove(BattleSide side, int moveIndex, List<BattleEvent> events)
        {
            var user = PartyOf(side).Active;
            var target = OpponentOf(side).Active;

            if (moveIndex < 0 || moveIndex >= user.Moves.Count) return;
            var slot = user.Moves[moveIndex];

            // Sleep: act on the wake turn.
            if (user.Status == StatusCondition.Sleep)
            {
                if (user.SleepTurnsLeft > 1)
                {
                    user.SleepTurnsLeft--;
                    events.Add(new ActionPreventedEvent(user, StatusCondition.Sleep));
                    return;
                }
                user.CureStatus();
                // woke up; proceeds to act
            }

            // Paralysis: chance to be fully paralyzed.
            if (user.Status == StatusCondition.Paralysis && _rng.Roll(_settings.ParalysisFailChance))
            {
                events.Add(new ActionPreventedEvent(user, StatusCondition.Paralysis));
                return;
            }

            if (slot.Move.ChargesUp && user.ChargingMoveIndex < 0)
            {
                slot.TryUse();
                user.ChargingMoveIndex = moveIndex;
                events.Add(new MoveUsedEvent(user, slot.Move));
                events.Add(new ChargingEvent(user, slot.Move));
                return;
            }
            if (user.ChargingMoveIndex >= 0) user.ChargingMoveIndex = -1;
            else slot.TryUse();
            events.Add(new MoveUsedEvent(user, slot.Move));

            var dmg = DamageCalculator.Calculate(user, target, slot.Move, _settings, _rng);
            if (!dmg.Hit)
            {
                events.Add(new MissedEvent(user, slot.Move));
                return;
            }

            if (dmg.Damage > 0)
            {
                target.TakeDamage(dmg.Damage);
                events.Add(new DamageEvent(target, dmg.Damage, dmg.Effectiveness, dmg.WasCritical));
                ApplyRecoilAndDrain(user, slot.Move, dmg.Damage, events);
            }

            ApplySecondaryEffects(user, target, slot.Move, events);

            if (target.IsFainted)
            {
                events.Add(new FaintedEvent(target));
                CheckBattleEnd(events);
            }
            if (user.IsFainted)
            {
                events.Add(new FaintedEvent(user));
                if (!IsOver) CheckBattleEnd(events);
            }
        }

        private void ApplySecondaryEffects(Pokemon user, Pokemon target, MoveData move,
            List<BattleEvent> events)
        {
            if (move.InflictsStatus != StatusCondition.None && move.StatusChance > 0
                && _rng.Roll(move.StatusChance / 100.0))
            {
                int sleepTurns = move.InflictsStatus == StatusCondition.Sleep
                    ? _rng.IntInclusive(_settings.MinSleepTurns, _settings.MaxSleepTurns) : 0;
                if (target.TryApplyStatus(move.InflictsStatus, sleepTurns))
                    events.Add(new StatusInflictedEvent(target, move.InflictsStatus));
            }

            if (move.StatStageDelta != 0 && move.StatChangeChance > 0
                && _rng.Roll(move.StatChangeChance / 100.0))
            {
                var recipient = move.StatChangeTargetsSelf ? user : target;
                int applied = recipient.ChangeStage(move.StatToChange, move.StatStageDelta);
                if (applied != 0)
                    events.Add(new StatChangedEvent(recipient, move.StatToChange, applied));
            }
        }

        private static void ApplyRecoilAndDrain(Pokemon user, MoveData move, int damageDealt, List<BattleEvent> events)
        {
            if (move.RecoilPercent > 0)
            {
                int recoil = damageDealt * move.RecoilPercent / 100;
                if (recoil < 1) recoil = 1;
                user.TakeDamage(recoil);
                events.Add(new RecoilEvent(user, recoil));
            }
            if (move.DrainPercent > 0)
            {
                int drain = damageDealt * move.DrainPercent / 100;
                if (drain < 1) drain = 1;
                user.Heal(drain);
                events.Add(new DrainEvent(user, drain));
            }
        }

        private void EndOfTurnStatus(BattleSide side, List<BattleEvent> events)
        {
            var p = PartyOf(side).Active;
            if (p.IsFainted) return;

            int dmg = 0;
            if (p.Status == StatusCondition.Poison)
                dmg = (int)Math.Floor(p.MaxHp * _settings.PoisonFraction);
            else if (p.Status == StatusCondition.Burn)
                dmg = (int)Math.Floor(p.MaxHp * _settings.BurnFraction);

            if (dmg > 0)
            {
                if (dmg < 1) dmg = 1;
                p.TakeDamage(dmg);
                events.Add(new StatusDamageEvent(p, p.Status, dmg));
                if (p.IsFainted)
                {
                    events.Add(new FaintedEvent(p));
                    CheckBattleEnd(events);
                }
            }
        }

        private void FlagForcedSwitchIfNeeded(BattleSide side)
        {
            var party = PartyOf(side);
            if (party.Active.IsFainted && party.HasUsablePokemon())
                _forcedSwitch[(int)side] = true;
        }

        public IReadOnlyList<BattleEvent> ResolveForcedSwitch(BattleSide side, int partyIndex)
        {
            var events = new List<BattleEvent>();
            if (!_forcedSwitch[(int)side])
                throw new InvalidOperationException("No forced switch pending for " + side);

            var party = PartyOf(side);
            int target = party.CanSwitchTo(partyIndex) ? partyIndex : party.FirstUsableIndex();
            party.SwitchTo(target);
            party.Active.Participated = true;
            _forcedSwitch[(int)side] = false;
            events.Add(new SwitchedInEvent(side, party.Active));
            return events;
        }

        private void CheckBattleEnd(List<BattleEvent> events)
        {
            bool playerAlive = Player.HasUsablePokemon();
            bool enemyAlive = Enemy.HasUsablePokemon();
            if (playerAlive && enemyAlive) return;

            if (!playerAlive && !enemyAlive) Result = BattleResult.Draw;
            else if (!enemyAlive) Result = BattleResult.PlayerWon;
            else Result = BattleResult.EnemyWon;

            events.Add(new BattleEndedEvent(Result));
        }
    }
}
