using System;
using System.Collections.Generic;

namespace MonsterCatcher.Map
{
    public sealed class MonsterSave
    {
        public string SpeciesName;
        public int Level;
        public float LevelProgress;
        public int CurrentHp;   // persisted; int.MaxValue = full

        public MonsterSave(string speciesName, int level)
        {
            SpeciesName = speciesName;
            Level = level;
            LevelProgress = 0f;
            CurrentHp = int.MaxValue;
        }
    }

    public static class RunState
    {
        public const float BenchShare = 0.5f;
        public const int BossLevel = 10;

        public static bool InRun;
        public static MapModel Map;
        public static int CurrentNodeId;
        public static int PendingNodeId = -1;
        public static bool RunWon;
        public static bool RunLost;
        public static readonly HashSet<int> Cleared = new HashSet<int>();
        public static List<MonsterSave> PlayerRoster = new List<MonsterSave>();
        public static int Tier = 1;

        // The starter is one random first-stage monster; all six can appear as enemies.
        public static readonly string[] Starters = { "Mossprig", "Cindrop", "Voltwig" };
        public static readonly string[][] EnemyLines =
        {
            new[] { "Mossprig", "Briarstag", "Elderthorn" }, // Grass line
            new[] { "Cindrop", "Magmelt", "Vulcarion" },     // Fire line
            new[] { "Voltwig", "Stormbark", "Tempestag" },   // Electric line
        };

        public static string StarterFor(int seed) =>
            Starters[(int)((uint)seed % (uint)Starters.Length)];

        public static int StageForRow(NodeType type, int row)
        {
            if (type == NodeType.Boss) return 2;
            if (row <= 3) return 0;
            if (row <= 6) return 1;
            return 2;
        }

        private static int EnemyElement(int nodeId, int tier)
        {
            int m = (nodeId * 31 + tier * 17) % EnemyLines.Length;
            return m < 0 ? m + EnemyLines.Length : m;
        }

        public static string EnemySpeciesFor(NodeType type, int row, int nodeId, int tier) =>
            EnemyLines[EnemyElement(nodeId, tier)][StageForRow(type, row)];

        public static void NewRun(int seed)
        {
            Map = MapGenerator.Generate(seed);
            CurrentNodeId = Map.StartId;
            PendingNodeId = -1;
            RunWon = false;
            RunLost = false;
            Cleared.Clear();
            Cleared.Add(Map.StartId);
            PlayerRoster = new List<MonsterSave>
            {
                new MonsterSave(StarterFor(seed), 1),
            };
            Tier = 1;
            InRun = true;
        }

        public static void NextTier(int seed)
        {
            Tier += 1;
            Map = MapGenerator.Generate(seed);
            CurrentNodeId = Map.StartId;
            PendingNodeId = -1;
            RunWon = false;
            RunLost = false;
            Cleared.Clear();
            Cleared.Add(Map.StartId);
        }

        public static IReadOnlyList<int> Available()
        {
            if (Map == null) return Array.Empty<int>();
            return Map.Get(CurrentNodeId).Next;
        }

        public static bool CanSelect(int id)
        {
            foreach (var n in Available()) if (n == id) return true;
            return false;
        }

        public static void Select(int id)
        {
            if (CanSelect(id)) PendingNodeId = id;
        }

        public static void ReportBattleResult(bool won)
        {
            if (won)
            {
                Cleared.Add(PendingNodeId);
                CurrentNodeId = PendingNodeId;
                if (PendingNodeId == Map.BossId) RunWon = true;
            }
            else
            {
                RunLost = true;
            }
            PendingNodeId = -1;
        }

        public static NodeStatus StatusOf(int id)
        {
            if (id == CurrentNodeId) return NodeStatus.Current;
            if (Cleared.Contains(id)) return NodeStatus.Cleared;
            if (CanSelect(id)) return NodeStatus.Available;
            return NodeStatus.Locked;
        }

        public static int PendingEnemyLevel()
        {
            if (Map == null || PendingNodeId < 0) return 5;
            var node = Map.Get(PendingNodeId);
            int tierBase = (Tier - 1) * BossLevel;
            return tierBase + (node.Type == NodeType.Boss ? BossLevel : node.Row);
        }

        public static string PendingEnemySpecies()
        {
            if (Map == null || PendingNodeId < 0) return EnemyLines[0][2];
            var node = Map.Get(PendingNodeId);
            return EnemySpeciesFor(node.Type, node.Row, PendingNodeId, Tier);
        }

        public static void ApplyWin(bool[] participated)
        {
            for (int i = 0; i < PlayerRoster.Count; i++)
            {
                float gain = (participated != null && i < participated.Length && participated[i]) ? 1f : BenchShare;
                var m = PlayerRoster[i];
                m.LevelProgress += gain;
                while (m.LevelProgress >= 1f)
                {
                    m.Level += 1;
                    m.LevelProgress -= 1f;
                }
            }
        }

        public static void WriteBackHp(int index, int hp)
        {
            if (index >= 0 && index < PlayerRoster.Count) PlayerRoster[index].CurrentHp = hp;
        }

        public static void HealParty()
        {
            foreach (var m in PlayerRoster) m.CurrentHp = int.MaxValue;
        }

        public static bool VisitHeal(int id)
        {
            if (!CanSelect(id)) return false;
            HealParty();
            Cleared.Add(id);
            CurrentNodeId = id;
            return true;
        }
    }
}
