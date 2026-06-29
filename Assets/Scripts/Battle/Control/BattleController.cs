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
            IRng rng = _rngSeed != 0 ? new DefaultRng(_rngSeed) : new DefaultRng();
            _engine = new BattleEngine(_player, _enemy, settings, rng);
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
            return new Party(BattleSide.Player, mons, settings.MaxPartySize);
        }

        private static Party BuildEnemy(BattleSettings settings)
        {
            var species = Resources.Load<SpeciesData>("Species/" + RunState.PendingEnemySpecies());
            int level = RunState.PendingEnemyLevel();
            var e = new Pokemon(species, level, MovesFor(species, level));
            return new Party(BattleSide.Enemy, new List<Pokemon> { e }, settings.MaxPartySize);
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
            if (won) RunState.ApplyWin(participated);
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
