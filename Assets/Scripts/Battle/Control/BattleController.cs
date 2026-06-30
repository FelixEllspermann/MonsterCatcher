using System;
using System.Collections.Generic;
using UnityEngine;
using MonsterCatcher.Map;

namespace MonsterCatcher.Battle
{
    public struct EvolutionOffer
    {
        public int RosterIndex;
        public string FromName;
        public string ToName;
    }

    public sealed class BattleController : MonoBehaviour
    {
        [SerializeField] private BattleSettings _settings;
        [SerializeField] private int _rngSeed = 0;
        [SerializeField] private bool _useSampleData = true;

        private BattleEngine _engine;
        private SimpleAI _ai;
        private Party _player;
        private Party _enemy;
        private bool _runResultApplied;
        private IRng _rng;

        public event Action<IReadOnlyList<BattleEvent>> TurnResolved;
        public BattleEngine Engine => _engine;

        private readonly List<EvolutionOffer> _pendingEvolutions = new List<EvolutionOffer>();
        public IReadOnlyList<EvolutionOffer> PendingEvolutions => _pendingEvolutions;

        public void StartBattle()
        {
            var settings = _settings != null ? _settings : SampleData.CreateSettings();
            if (RunState.InRun)
            {
                _player = BuildPlayerFromRoster(settings);
                _enemy = BuildEnemy(settings);
            }
            else if (_useSampleData)
            {
                _player = SampleData.CreatePlayerParty(settings);
                _enemy = SampleData.CreateEnemyParty(settings);
            }
            _ai = new SimpleAI();
            _rng = _rngSeed != 0 ? new DefaultRng(_rngSeed) : new DefaultRng();
            _engine = new BattleEngine(_player, _enemy, settings, _rng);
            _runResultApplied = false;
        }

        private static Party BuildPlayerFromRoster(BattleSettings settings)
        {
            var mons = new List<Pokemon>();
            foreach (var save in RunState.PlayerRoster)
            {
                var species = Resources.Load<SpeciesData>("Species/" + save.SpeciesName);
                if (species == null) continue;
                var p = new Pokemon(species, save.Level, MovesFor(species, save.Level), save.AbilityIds);
                if (save.CurrentHp < p.MaxHp) p.SetCurrentHp(save.CurrentHp);
                mons.Add(p);
            }
            return new Party(BattleSide.Player, mons, System.Math.Max(settings.MaxPartySize, mons.Count));
        }

        private static Party BuildEnemy(BattleSettings settings)
        {
            int level = RunState.PendingEnemyLevel();
            var mons = new List<Pokemon>();
            if (RunState.IsBossBattle())
            {
                int n = RunState.BossPartySize();
                for (int i = 0; i < n; i++)
                {
                    var sp = Resources.Load<SpeciesData>("Species/" + RunState.BossEnemySpecies(i));
                    if (sp == null) continue;
                    var ab = AbilityCatalog.RollId(RunState.PendingNodeId * 31 + RunState.Tier * 101 + 7 + i * 13);
                    mons.Add(new Pokemon(sp, level, MovesFor(sp, level), new[] { ab }));
                }
            }
            else
            {
                var sp = Resources.Load<SpeciesData>("Species/" + RunState.PendingEnemySpecies());
                var ab = AbilityCatalog.RollId(RunState.PendingNodeId * 31 + RunState.Tier * 101 + 7);
                mons.Add(new Pokemon(sp, level, MovesFor(sp, level), new[] { ab }));
            }
            return new Party(BattleSide.Enemy, mons, System.Math.Max(settings.MaxPartySize, mons.Count));
        }

        private static List<MoveData> MovesFor(SpeciesData species, int level)
        {
            var moves = species.MovesAtLevel(level);
            return moves.Count > 0 ? moves : SampleData.PlaceholderGrassMoves();
        }

        private void ApplyRunResultIfOver()
        {
            if (!RunState.InRun || _runResultApplied || _engine == null || !_engine.IsOver) return;
            _runResultApplied = true;
            bool won = _engine.Result == BattleResult.PlayerWon;

            var participated = new bool[_player.Members.Count];
            for (int i = 0; i < _player.Members.Count; i++)
            {
                participated[i] = _player.Members[i].Participated;
                RunState.WriteBackHp(i, _player.Members[i].CurrentHp);
            }
            if (won)
            {
                RunState.ApplyWin(participated);
                RunState.AddGold(5 + 3 * RunState.PendingEnemyLevel());
            }
            ComputePendingEvolutions(won);
            RunState.ReportBattleResult(won);
        }

        private void ComputePendingEvolutions(bool won)
        {
            _pendingEvolutions.Clear();
            if (!won) return;
            for (int i = 0; i < RunState.PlayerRoster.Count; i++)
            {
                var save = RunState.PlayerRoster[i];
                var sp = Resources.Load<SpeciesData>("Species/" + save.SpeciesName);
                if (sp != null && sp.CanEvolveAt(save.Level))
                    _pendingEvolutions.Add(new EvolutionOffer
                    {
                        RosterIndex = i,
                        FromName = sp.DisplayName,
                        ToName = sp.EvolvesInto.name
                    });
            }
        }

        public void EvolveRosterMonster(int rosterIndex)
        {
            if (rosterIndex < 0 || rosterIndex >= RunState.PlayerRoster.Count) return;
            var save = RunState.PlayerRoster[rosterIndex];
            var sp = Resources.Load<SpeciesData>("Species/" + save.SpeciesName);
            if (sp != null && sp.EvolvesInto != null) save.SpeciesName = sp.EvolvesInto.name;
        }

        public void PlayerUseMove(int moveIndex) => ResolveTurn(BattleAction.UseMove(moveIndex));
        public void PlayerSwitch(int partyIndex) => ResolveTurn(BattleAction.SwitchTo(partyIndex));

        // Use a held item. Healing/buff/revive cost the turn (the enemy acts); a failed use is
        // rejected with a message and no turn. (Monster Catcher is wired in Phase 2.)
        public void UseItem(string itemId)
        {
            if (_engine == null || _engine.IsOver) return;
            if (_engine.AwaitingForcedSwitch(BattleSide.Player)) return;
            if (RunState.ItemCount(itemId) <= 0) return;

            if (itemId == "MonsterCatcher") { UseMonsterCatcher(); return; }

            var active = _engine.Player.Active;
            var pre = new List<BattleEvent>();
            if (!ApplyItemEffect(itemId, active, pre))
            {
                TurnResolved?.Invoke(pre);   // rejected — message only, no turn spent
                return;
            }
            RunState.RemoveItem(itemId, 1);

            var enemyAction = _ai.ChooseAction(_enemy, _player);
            var events = new List<BattleEvent>(pre);
            events.AddRange(_engine.ExecuteTurn(BattleAction.Pass(), enemyAction));
            if (_engine.AwaitingForcedSwitch(BattleSide.Enemy))
            {
                int idx = _enemy.FirstUsableIndex();
                events.AddRange(_engine.ResolveForcedSwitch(BattleSide.Enemy, idx));
            }
            ApplyRunResultIfOver();
            TurnResolved?.Invoke(events);
        }

        private bool ApplyItemEffect(string itemId, Pokemon active, List<BattleEvent> events)
        {
            switch (itemId)
            {
                case "Potion":
                    if (active.CurrentHp >= active.MaxHp)
                    { events.Add(new ItemUsedEvent(active.Species.DisplayName + " is already at full HP.")); return false; }
                    int before = active.CurrentHp;
                    active.Heal(active.MaxHp / 2);
                    events.Add(new ItemUsedEvent("Used a Potion."));
                    events.Add(new HealedEvent(active, active.CurrentHp - before));
                    return true;
                case "Antidote": return CureSpecific(active, StatusCondition.Poison, events);
                case "BurnHeal": return CureSpecific(active, StatusCondition.Burn, events);
                case "ParalyzeHeal": return CureSpecific(active, StatusCondition.Paralysis, events);
                case "Awakening": return CureSpecific(active, StatusCondition.Sleep, events);
                case "XAttack":
                    active.ChangeStage(Stat.Attack, 1);
                    events.Add(new ItemUsedEvent(active.Species.DisplayName + "'s Attack rose!"));
                    return true;
                case "Revive":
                    var ko = FirstFaintedMember();
                    if (ko == null)
                    { events.Add(new ItemUsedEvent("No fainted team member to revive.")); return false; }
                    ko.SetCurrentHp(ko.MaxHp / 2);
                    events.Add(new ItemUsedEvent(ko.Species.DisplayName + " was revived!"));
                    return true;
                default:
                    events.Add(new ItemUsedEvent("You can't use that here."));
                    return false;
            }
        }

        private static bool CureSpecific(Pokemon active, StatusCondition status, List<BattleEvent> events)
        {
            if (active.Status != status)
            {
                events.Add(new ItemUsedEvent(active.Species.DisplayName + " isn't affected by " + status + "."));
                return false;
            }
            active.CureStatus();
            events.Add(new ItemUsedEvent(active.Species.DisplayName + "'s " + status + " was cured."));
            return true;
        }

        private Pokemon FirstFaintedMember()
        {
            foreach (var m in _player.Members) if (m.IsFainted) return m;
            return null;
        }

        private void UseMonsterCatcher()
        {
            var enemy = _engine.Enemy.Active;
            var events = new List<BattleEvent>();
            if (RunState.IsBossBattle())
            { events.Add(new ItemUsedEvent("You can't catch a boss!")); TurnResolved?.Invoke(events); return; }
            if (RunState.PlayerRoster.Count >= RunState.MaxRoster)
            { events.Add(new ItemUsedEvent("Your team is full! Release a monster first.")); TurnResolved?.Invoke(events); return; }

            RunState.RemoveItem("MonsterCatcher", 1);
            events.Add(new ItemUsedEvent("You threw a Monster Catcher!"));

            if (_rng.Roll(CatchCalculator.Chance(enemy)))
            {
                _engine.EndBattle(BattleResult.PlayerWon);
                events.Add(new CaughtEvent(enemy));
                ApplyRunResultIfOver();      // gold / leveling / evolution for the current team
                AddCaughtToRoster(enemy);     // then add the catch (so it doesn't earn this win's XP)
                TurnResolved?.Invoke(events);
            }
            else
            {
                events.Add(new BrokeFreeEvent(enemy));
                var enemyAction = _ai.ChooseAction(_enemy, _player);
                events.AddRange(_engine.ExecuteTurn(BattleAction.Pass(), enemyAction));
                if (_engine.AwaitingForcedSwitch(BattleSide.Enemy))
                {
                    int idx = _enemy.FirstUsableIndex();
                    events.AddRange(_engine.ResolveForcedSwitch(BattleSide.Enemy, idx));
                }
                ApplyRunResultIfOver();
                TurnResolved?.Invoke(events);
            }
        }

        private void AddCaughtToRoster(Pokemon enemy)
        {
            if (RunState.PlayerRoster.Count >= RunState.MaxRoster) return;
            var save = new MonsterSave(enemy.Species.name, enemy.Level);
            save.AbilityIds = new List<string>(enemy.AbilityIds);
            save.CurrentHp = enemy.CurrentHp;
            RunState.PlayerRoster.Add(save);
        }

        private void ResolveTurn(BattleAction playerAction)
        {
            if (_engine == null || _engine.IsOver) return;

            var enemyAction = _ai.ChooseAction(_enemy, _player);
            var events = new List<BattleEvent>(_engine.ExecuteTurn(playerAction, enemyAction));

            // Auto-resolve the enemy's forced switch; the player's is exposed for the UI.
            if (_engine.AwaitingForcedSwitch(BattleSide.Enemy))
            {
                int idx = _enemy.FirstUsableIndex();
                events.AddRange(_engine.ResolveForcedSwitch(BattleSide.Enemy, idx));
            }

            ApplyRunResultIfOver();
            TurnResolved?.Invoke(events);
        }

        public void ResolvePlayerForcedSwitch(int partyIndex)
        {
            if (_engine == null || !_engine.AwaitingForcedSwitch(BattleSide.Player)) return;
            var events = _engine.ResolveForcedSwitch(BattleSide.Player, partyIndex);
            ApplyRunResultIfOver();
            TurnResolved?.Invoke(events);
        }

        // TODO (after MCP bridge live): bind StartBattle/PlayerUseMove/PlayerSwitch to UI buttons,
        // render TurnResolved events as HP-bar tweens and battle-log text.
    }
}
