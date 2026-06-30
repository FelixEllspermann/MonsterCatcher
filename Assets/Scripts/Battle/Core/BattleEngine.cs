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
            AbilityApplier.OnEntry(Player.Active, Enemy.Active);
            AbilityApplier.OnEntry(Enemy.Active, Player.Active);
        }

        public bool AwaitingForcedSwitch(BattleSide side) => _forcedSwitch[(int)side];

        // Force the battle to end (e.g. the wild enemy was caught — it isn't fainted, but the fight is over).
        public void EndBattle(BattleResult result)
        {
            if (!IsOver) Result = result;
        }

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
            bool firstMoveDone = false;
            foreach (var entry in pending)
            {
                var user = PartyOf(entry.side).Active;
                if (user.IsFainted) continue;
                ExecuteMove(entry.side, entry.action.Index, events, firstMoveDone);
                firstMoveDone = true;
                if (IsOver) return events;
            }

            // 4. End-of-turn status damage (player first, then enemy).
            EndOfTurnStatus(BattleSide.Player, events);
            if (IsOver) return events;
            EndOfTurnStatus(BattleSide.Enemy, events);
            if (IsOver) return events;

            // Track how many turns each active has been out (first-turn / ramping abilities).
            Player.Active.AbilityState.TurnsOut++;
            Enemy.Active.AbilityState.TurnsOut++;

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
                    party.Active.AbilityState.TurnsOut = 0;
                    AbilityApplier.OnEntry(party.Active, OpponentOf(side).Active, events);
                    events.Add(new SwitchedInEvent(side, party.Active));
                }
            }
            else if (action.Kind == ActionKind.Pass)
            {
                // The side does nothing this turn (used an item); the opponent still acts.
            }
            else
            {
                pending.Add((side, action));
            }
        }

        private int CompareOrder((BattleSide side, BattleAction action) x,
            (BattleSide side, BattleAction action) y)
        {
            var ax = PartyOf(x.side).Active;
            var ay = PartyOf(y.side).Active;
            bool fx = AbilityApplier.ForcesFirst(ax), fy = AbilityApplier.ForcesFirst(ay);
            if (fx != fy) return fx ? -1 : 1;       // Time Warp moves first turn 1
            bool lx = AbilityApplier.ForcesLast(ax), ly = AbilityApplier.ForcesLast(ay);
            if (lx != ly) return lx ? 1 : -1;       // Reversal always moves last

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
            var active = PartyOf(side).Active;
            var moves = active.Moves;
            if (moveIndex < 0 || moveIndex >= moves.Count) return 0;
            var move = moves[moveIndex].Move;
            return move.Priority + AbilityApplier.PriorityBonus(active, move);
        }

        private int EffectiveSpeed(BattleSide side)
        {
            var p = PartyOf(side).Active;
            double speed = p.EffectiveStat(Stat.Speed) * AbilityApplier.SpeedFactor(p);
            if (p.Status == StatusCondition.Paralysis && !AbilityApplier.ParalysisSpeedImmune(p))
                speed *= _settings.ParalysisSpeedMultiplier;
            int s = (int)Math.Floor(speed);
            return s < 1 ? 1 : s;
        }

        private void ExecuteMove(BattleSide side, int moveIndex, List<BattleEvent> events, bool movedAfter = false)
        {
            var user = PartyOf(side).Active;
            var target = OpponentOf(side).Active;

            if (moveIndex < 0 || moveIndex >= user.Moves.Count) return;
            var slot = user.Moves[moveIndex];

            // Sleep: act on the wake turn.
            if (user.Status == StatusCondition.Sleep)
            {
                if (user.StatusTurnsLeft > 1)
                {
                    user.StatusTurnsLeft--;
                    events.Add(new ActionPreventedEvent(user, StatusCondition.Sleep));
                    return;
                }
                user.CureStatus();
                events.Add(new StatusEndedEvent(user, StatusCondition.Sleep));
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

            int faintedAllies = 0;
            foreach (var m in PartyOf(side).Members) if (m.IsFainted) faintedAllies++;
            var dmg = DamageCalculator.Calculate(user, target, slot.Move, _settings, _rng, faintedAllies, movedAfter);
            if (!dmg.Hit)
            {
                events.Add(new MissedEvent(user, slot.Move));
                return;
            }

            if (dmg.Damage > 0)
            {
                if (AbilityApplier.ExecutesFoe(user, target) && dmg.Damage < target.CurrentHp)
                    dmg.Damage = target.CurrentHp;   // Executioner finishes low-HP foes
                target.TakeDamage(dmg.Damage);
                events.Add(new DamageEvent(target, dmg.Damage, dmg.Effectiveness, dmg.WasCritical));
                target.AbilityState.FirstHitTaken = true;
                ApplyRecoilAndDrain(user, slot.Move, dmg.Damage, events);
                int drained = AbilityApplier.DrainAmount(user, dmg.Damage);
                if (drained > 0) { user.Heal(drained); events.Add(new DrainEvent(user, drained)); }
                AbilityApplier.OnDealtDamage(user, target, events);       // Thorns + on-hit self-buff
                AbilityApplier.OnHitInflict(user, target, _rng, events);  // Venomtouch / Static Body
            }

            ApplySecondaryEffects(user, target, slot.Move, events);

            if (target.IsFainted)
            {
                if (AbilityApplier.TryRevive(target))
                {
                    events.Add(new RevivedEvent(target));
                }
                else
                {
                    AbilityApplier.OnKo(user, events);              // Moxie
                    AbilityApplier.OnFaint(target, user, events);   // Aftermath
                    events.Add(new FaintedEvent(target));
                    CheckBattleEnd(events);
                }
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
            double chanceMult = AbilityApplier.SecondaryChanceMult(user);   // Lucky Charm

            if (move.InflictsStatus != StatusCondition.None && move.StatusChance > 0
                && _rng.Roll(move.StatusChance / 100.0 * chanceMult))
            {
                int sleepTurns = move.InflictsStatus == StatusCondition.Sleep
                    ? _rng.IntInclusive(_settings.MinSleepTurns, _settings.MaxSleepTurns) : 0;
                if (target.TryApplyStatus(move.InflictsStatus, sleepTurns))
                    events.Add(new StatusInflictedEvent(target, move.InflictsStatus));
            }

            if (move.StatStageDelta != 0 && move.StatChangeChance > 0
                && _rng.Roll(move.StatChangeChance / 100.0 * chanceMult))
            {
                var recipient = move.StatChangeTargetsSelf ? user : target;
                bool foeDrop = !move.StatChangeTargetsSelf && move.StatStageDelta < 0;
                if (foeDrop && AbilityApplier.ImmuneToStatDrops(recipient))
                    return;   // Hardy Mind ignores foe-inflicted stat drops
                int applied = recipient.ChangeStage(move.StatToChange, move.StatStageDelta);
                if (applied != 0)
                    events.Add(new StatChangedEvent(recipient, move.StatToChange, applied));
            }
        }

        private static void ApplyRecoilAndDrain(Pokemon user, MoveData move, int damageDealt, List<BattleEvent> events)
        {
            if (move.RecoilPercent > 0 && !AbilityApplier.RecoilImmune(user))
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
            if (p.Status == StatusCondition.Poison && !AbilityApplier.PoisonChipBlocked(p))
                dmg = (int)Math.Floor(p.MaxHp * _settings.PoisonFraction);
            else if (p.Status == StatusCondition.Burn && !AbilityApplier.BurnChipBlocked(p))
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
                    return;
                }
            }

            // Non-sleep statuses tick down each turn and wear off (sleep is handled on the wake turn).
            if (p.Status != StatusCondition.None && p.Status != StatusCondition.Sleep)
            {
                if (p.StatusTurnsLeft > 1) p.StatusTurnsLeft--;
                else
                {
                    var ended = p.Status;
                    p.CureStatus();
                    events.Add(new StatusEndedEvent(p, ended));
                }
            }

            // Ability regeneration (Regrowth/Mending/Bloom) + one-time heal (Second Wind).
            int heal = AbilityApplier.EndOfTurnHeal(p) + AbilityApplier.OneTimeHeal(p);
            if (heal > 0 && !p.IsFainted && p.CurrentHp < p.MaxHp)
            {
                p.Heal(heal);
                events.Add(new DrainEvent(p, heal));
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
            party.Active.AbilityState.TurnsOut = 0;
            _forcedSwitch[(int)side] = false;
            AbilityApplier.OnEntry(party.Active, OpponentOf(side).Active, events);
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
