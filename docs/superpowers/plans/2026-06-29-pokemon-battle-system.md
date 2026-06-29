# Pokémon-Kampfsystem Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an authentic, turn-based single-battle Pokémon combat system (stats, full 18-type chart, real damage formula, status conditions, stat stages, team switching, player-vs-AI) as a data-driven, unit-tested C# core inside the Unity 6 MonsterCatcher project.

**Architecture:** Content lives in ScriptableObjects (`SpeciesData`, `MoveData`, `BattleSettings`). All combat logic is plain C# with no MonoBehaviour/GameObject dependency, so it runs under EditMode tests without the editor. A thin `BattleController` MonoBehaviour bridges input/UI to the engine. Randomness is injected (`IRng`) for deterministic tests.

**Tech Stack:** Unity 6 (`6000.4.3f1`), C# 9, Unity Test Framework (NUnit, EditMode), two Assembly Definitions (`Battle`, `Battle.Tests`).

## Global Constraints

- **Namespace:** all code in `MonsterCatcher.Battle` (block-scoped namespace; C# 9 only — no file-scoped namespaces, no C# 10+ features).
- **No `UnityEngine` runtime types in combat logic** (`Core/`) except the ScriptableObject *references* it reads. `MonoBehaviour` lives only in `Control/BattleController.cs`.
- **Determinism:** every random decision goes through `IRng`. No `UnityEngine.Random`, no `System.Random` calls outside `DefaultRng`.
- **Damage/stat values are integers**, floored exactly per the formulas in the spec (`docs/superpowers/specs/2026-06-29-pokemon-battle-system-design.md`).
- **Tunable constants live in `BattleSettings`** (party size, status fractions, crit, paralysis, sleep) — never hard-coded in logic.
- **Default battler config:** Level 50, IV 0, EV 0, neutral nature (formulas keep the structure for later).
- **Test execution:** EditMode tests run via the Unity Test Runner or MCP `run_tests(mode="EditMode")` (preferred), or CLI `Unity.exe -runTests -batchmode -projectPath . -testPlatform EditMode -testResults <file>`. In the current MCP-offline session, code + tests are authored, then the full red→green pass is run once the Unity bridge is live after the Claude Code restart.
- **Git:** repo is not initialized. Task 0 runs `git init` so the per-task commit steps work. If the user declines git, skip every commit step.

---

## File Structure

```
Assets/Scripts/Battle/
  Battle.asmdef
  Core/
    ElementType.cs        enum, 18 types
    MoveCategory.cs       enum Physical/Special/Status
    StatusCondition.cs    enum None/Poison/Burn/Paralysis/Sleep
    Stat.cs               enum Hp/Attack/Defense/SpAttack/SpDefense/Speed
    BattleSide.cs         enum Player/Enemy
    BattleResult.cs       enum InProgress/PlayerWon/EnemyWon/Draw
    IRng.cs               randomness interface + DefaultRng
    TypeChart.cs          static 18×18 effectiveness lookup
    StatStages.cs         static stage→multiplier table
    MoveSlot.cs           move + current PP
    Pokemon.cs            runtime battler (stats, status, stages, damage)
    Party.cs              team + active index + switch rules
    BattleAction.cs       Move | Switch value type
    BattleEvent.cs        event records for UI/log
    DamageCalculator.cs   the damage formula
    BattleEngine.cs       turn resolution
  Data/
    SpeciesData.cs        ScriptableObject
    MoveData.cs           ScriptableObject
    BattleSettings.cs     ScriptableObject (tunables)
  Control/
    SimpleAI.cs           AI action selection (incl. switching)
    BattleController.cs    MonoBehaviour bridge (skeleton; UI wired post-MCP)
  Fixtures/
    SampleData.cs         code-built sample species/moves/parties

Assets/Scripts/Battle/Tests/
  Battle.Tests.asmdef
  FakeRng.cs              deterministic IRng for tests
  TestFactory.cs          helpers to build SpeciesData/MoveData/Pokemon in code
  TypeChartTests.cs
  StatStagesTests.cs
  PokemonTests.cs
  PartyTests.cs
  DamageTests.cs
  BattleEngineTests.cs
  SimpleAITests.cs
```

Each `.cs` file holds one type with one responsibility. Tasks build bottom-up so every task only consumes already-built interfaces.

---

### Task 0: Scaffolding & assemblies

**Files:**
- Create: `Assets/Scripts/Battle/Battle.asmdef`
- Create: `Assets/Scripts/Battle/Tests/Battle.Tests.asmdef`
- Create: `Assets/Scripts/Battle/Core/Placeholder.cs` (temporary, deleted in Task 1)
- Create: `Assets/Scripts/Battle/Tests/SmokeTest.cs`

**Interfaces:**
- Produces: assemblies `Battle` and `Battle.Tests` (EditMode) that compile.

- [ ] **Step 1: Init git so commits work**

Run: `git init && git add -A && git commit -m "chore: snapshot before battle system"`
Expected: repo created, initial commit. (Skip this task's commits entirely if the user declined git.)

- [ ] **Step 2: Create `Battle.asmdef`**

```json
{
  "name": "Battle",
  "rootNamespace": "MonsterCatcher.Battle",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

- [ ] **Step 3: Create `Battle.Tests.asmdef`**

```json
{
  "name": "Battle.Tests",
  "rootNamespace": "MonsterCatcher.Battle.Tests",
  "references": ["Battle", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "versionDefines": [],
  "noEngineReferences": false
}
```

- [ ] **Step 4: Add a temporary type so `Battle` has compilable content**

`Assets/Scripts/Battle/Core/Placeholder.cs`:
```csharp
namespace MonsterCatcher.Battle
{
    // Temporary anchor so the assembly compiles before real types exist. Deleted in Task 1.
    internal static class Placeholder { }
}
```

- [ ] **Step 5: Write a smoke test**

`Assets/Scripts/Battle/Tests/SmokeTest.cs`:
```csharp
using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class SmokeTest
    {
        [Test]
        public void AssembliesCompileAndTestsRun()
        {
            Assert.AreEqual(4, 2 + 2);
        }
    }
}
```

- [ ] **Step 6: Run the smoke test**

Run (MCP): `run_tests(mode="EditMode", test_names=["MonsterCatcher.Battle.Tests.SmokeTest.AssembliesCompileAndTestsRun"])`
Expected: 1 passed. (CLI alternative: `Unity.exe -runTests -batchmode -projectPath . -testPlatform EditMode -testResults results.xml`.)

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Battle
git commit -m "chore: scaffold Battle assemblies and EditMode test runner"
```

---

### Task 1: Enums

**Files:**
- Create: `Core/ElementType.cs`, `Core/MoveCategory.cs`, `Core/StatusCondition.cs`, `Core/Stat.cs`, `Core/BattleSide.cs`, `Core/BattleResult.cs`
- Delete: `Core/Placeholder.cs`

**Interfaces:**
- Produces: `ElementType` (18 values), `MoveCategory{Physical,Special,Status}`, `StatusCondition{None,Poison,Burn,Paralysis,Sleep}`, `Stat{Hp,Attack,Defense,SpAttack,SpDefense,Speed}`, `BattleSide{Player,Enemy}`, `BattleResult{InProgress,PlayerWon,EnemyWon,Draw}`.

- [ ] **Step 1: Create the enums, delete the placeholder**

`Core/ElementType.cs`:
```csharp
namespace MonsterCatcher.Battle
{
    public enum ElementType
    {
        Normal, Fire, Water, Electric, Grass, Ice, Fighting, Poison, Ground,
        Flying, Psychic, Bug, Rock, Ghost, Dragon, Dark, Steel, Fairy
    }
}
```
`Core/MoveCategory.cs`:
```csharp
namespace MonsterCatcher.Battle
{
    public enum MoveCategory { Physical, Special, Status }
}
```
`Core/StatusCondition.cs`:
```csharp
namespace MonsterCatcher.Battle
{
    public enum StatusCondition { None, Poison, Burn, Paralysis, Sleep }
}
```
`Core/Stat.cs`:
```csharp
namespace MonsterCatcher.Battle
{
    public enum Stat { Hp, Attack, Defense, SpAttack, SpDefense, Speed }
}
```
`Core/BattleSide.cs`:
```csharp
namespace MonsterCatcher.Battle
{
    public enum BattleSide { Player, Enemy }
}
```
`Core/BattleResult.cs`:
```csharp
namespace MonsterCatcher.Battle
{
    public enum BattleResult { InProgress, PlayerWon, EnemyWon, Draw }
}
```
Then delete `Core/Placeholder.cs`.

- [ ] **Step 2: Verify compile via the smoke test**

Run (MCP): `run_tests(mode="EditMode", test_names=["MonsterCatcher.Battle.Tests.SmokeTest.AssembliesCompileAndTestsRun"])`
Expected: PASS (no compile errors). Then `read_console(types=["error"], count=10)` → empty.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Battle/Core
git commit -m "feat: add battle enums"
```

---

### Task 2: IRng + DefaultRng + FakeRng

**Files:**
- Create: `Core/IRng.cs`
- Create: `Tests/FakeRng.cs`

**Interfaces:**
- Produces:
  - `interface IRng { bool Roll(double probability); int IntInclusive(int minInclusive, int maxInclusive); }`
  - `class DefaultRng : IRng` (seeded + unseeded ctor)
  - `class FakeRng : IRng` (tests) with `bool DefaultRoll`, `int IntResult`, `FakeRng Enqueue(params bool[])`.
- Contract: `Roll(p)` returns `true` when `p >= 1`, `false` when `p <= 0`, otherwise queued/`DefaultRoll` (Fake) or `rng.NextDouble() < p` (Default). `IntInclusive` is inclusive on both ends.

- [ ] **Step 1: Write the failing test**

`Tests/FakeRng.cs` (helper used by later tests) and `Tests/RngTests.cs`:
```csharp
using System.Collections.Generic;

namespace MonsterCatcher.Battle.Tests
{
    public sealed class FakeRng : IRng
    {
        public bool DefaultRoll = false;
        public int IntResult = int.MaxValue;
        private readonly Queue<bool> _rolls = new Queue<bool>();

        public FakeRng Enqueue(params bool[] results)
        {
            foreach (var r in results) _rolls.Enqueue(r);
            return this;
        }

        public bool Roll(double probability)
        {
            if (probability >= 1.0) return true;
            if (probability <= 0.0) return false;
            return _rolls.Count > 0 ? _rolls.Dequeue() : DefaultRoll;
        }

        public int IntInclusive(int minInclusive, int maxInclusive)
        {
            int v = IntResult;
            if (v < minInclusive) v = minInclusive;
            if (v > maxInclusive) v = maxInclusive;
            return v;
        }
    }
}
```
`Tests/RngTests.cs`:
```csharp
using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class RngTests
    {
        [Test]
        public void Roll_Certain_IsAlwaysTrue()
        {
            Assert.IsTrue(new DefaultRng(1).Roll(1.0));
            Assert.IsFalse(new DefaultRng(1).Roll(0.0));
        }

        [Test]
        public void IntInclusive_IsDeterministicForSeed()
        {
            var a = new DefaultRng(42);
            var b = new DefaultRng(42);
            Assert.AreEqual(a.IntInclusive(85, 100), b.IntInclusive(85, 100));
        }
    }
}
```

- [ ] **Step 2: Run → FAIL** (`IRng`/`DefaultRng` not defined).

Run (MCP): `run_tests(mode="EditMode", test_names=["MonsterCatcher.Battle.Tests.RngTests"])`
Expected: compile error / FAIL.

- [ ] **Step 3: Implement `Core/IRng.cs`**

```csharp
using System;

namespace MonsterCatcher.Battle
{
    public interface IRng
    {
        bool Roll(double probability);
        int IntInclusive(int minInclusive, int maxInclusive);
    }

    public sealed class DefaultRng : IRng
    {
        private readonly Random _random;

        public DefaultRng() { _random = new Random(); }
        public DefaultRng(int seed) { _random = new Random(seed); }

        public bool Roll(double probability)
        {
            if (probability >= 1.0) return true;
            if (probability <= 0.0) return false;
            return _random.NextDouble() < probability;
        }

        public int IntInclusive(int minInclusive, int maxInclusive)
        {
            return _random.Next(minInclusive, maxInclusive + 1);
        }
    }
}
```

- [ ] **Step 4: Run → PASS.** Then `read_console(types=["error"])` empty.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Battle/Core/IRng.cs Assets/Scripts/Battle/Tests/FakeRng.cs Assets/Scripts/Battle/Tests/RngTests.cs
git commit -m "feat: add injectable RNG with deterministic test double"
```

---

### Task 3: TypeChart

**Files:**
- Create: `Core/TypeChart.cs`
- Create: `Tests/TypeChartTests.cs`

**Interfaces:**
- Produces: `static class TypeChart` with
  - `static double Effectiveness(ElementType attacking, ElementType defending)`
  - `static double Effectiveness(ElementType attacking, ElementType defType1, ElementType defType2, bool hasSecondType)`
- Contract: returns product over defender types; values ∈ {0, 0.25, 0.5, 1, 2, 4}.

- [ ] **Step 1: Write failing tests**

`Tests/TypeChartTests.cs`:
```csharp
using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class TypeChartTests
    {
        [Test] public void SuperEffective() =>
            Assert.AreEqual(2.0, TypeChart.Effectiveness(ElementType.Water, ElementType.Fire), 1e-6);

        [Test] public void NotVeryEffective() =>
            Assert.AreEqual(0.5, TypeChart.Effectiveness(ElementType.Fire, ElementType.Water), 1e-6);

        [Test] public void Immune() =>
            Assert.AreEqual(0.0, TypeChart.Effectiveness(ElementType.Normal, ElementType.Ghost), 1e-6);

        [Test] public void Neutral() =>
            Assert.AreEqual(1.0, TypeChart.Effectiveness(ElementType.Normal, ElementType.Water), 1e-6);

        [Test] public void DualTypeStacks() =>
            // Rock attacking Fire/Flying = 2 * 2 = 4
            Assert.AreEqual(4.0, TypeChart.Effectiveness(ElementType.Rock, ElementType.Fire, ElementType.Flying, true), 1e-6);

        [Test] public void DualTypeImmunityWins() =>
            // Ground 0 vs Flying, even if other type is weak
            Assert.AreEqual(0.0, TypeChart.Effectiveness(ElementType.Ground, ElementType.Flying, ElementType.Fire, true), 1e-6);
    }
}
```

- [ ] **Step 2: Run → FAIL** (`TypeChart` not defined).

- [ ] **Step 3: Implement `Core/TypeChart.cs`** (canonical Gen VI+ chart; only non-1.0 relations listed)

```csharp
using System.Collections.Generic;

namespace MonsterCatcher.Battle
{
    public static class TypeChart
    {
        // _chart[attacker][defender] = multiplier for entries that are not 1.0.
        private static readonly Dictionary<ElementType, Dictionary<ElementType, double>> _chart =
            Build();

        public static double Effectiveness(ElementType attacking, ElementType defending)
        {
            if (_chart.TryGetValue(attacking, out var row) &&
                row.TryGetValue(defending, out var mult))
            {
                return mult;
            }
            return 1.0;
        }

        public static double Effectiveness(ElementType attacking, ElementType defType1,
            ElementType defType2, bool hasSecondType)
        {
            double e = Effectiveness(attacking, defType1);
            if (hasSecondType) e *= Effectiveness(attacking, defType2);
            return e;
        }

        private static Dictionary<ElementType, Dictionary<ElementType, double>> Build()
        {
            var c = new Dictionary<ElementType, Dictionary<ElementType, double>>();

            void Row(ElementType atk, params (ElementType def, double mult)[] entries)
            {
                var d = new Dictionary<ElementType, double>();
                foreach (var (def, mult) in entries) d[def] = mult;
                c[atk] = d;
            }

            const double H = 0.5, X = 2.0, Z = 0.0;
            Row(ElementType.Normal, (ElementType.Rock, H), (ElementType.Ghost, Z), (ElementType.Steel, H));
            Row(ElementType.Fire, (ElementType.Fire, H), (ElementType.Water, H), (ElementType.Grass, X), (ElementType.Ice, X), (ElementType.Bug, X), (ElementType.Rock, H), (ElementType.Dragon, H), (ElementType.Steel, X));
            Row(ElementType.Water, (ElementType.Fire, X), (ElementType.Water, H), (ElementType.Grass, H), (ElementType.Ground, X), (ElementType.Rock, X), (ElementType.Dragon, H));
            Row(ElementType.Electric, (ElementType.Water, X), (ElementType.Electric, H), (ElementType.Grass, H), (ElementType.Ground, Z), (ElementType.Flying, X), (ElementType.Dragon, H));
            Row(ElementType.Grass, (ElementType.Fire, H), (ElementType.Water, X), (ElementType.Grass, H), (ElementType.Poison, H), (ElementType.Ground, X), (ElementType.Flying, H), (ElementType.Bug, H), (ElementType.Rock, X), (ElementType.Dragon, H), (ElementType.Steel, H));
            Row(ElementType.Ice, (ElementType.Fire, H), (ElementType.Water, H), (ElementType.Grass, X), (ElementType.Ice, H), (ElementType.Ground, X), (ElementType.Flying, X), (ElementType.Dragon, X), (ElementType.Steel, H));
            Row(ElementType.Fighting, (ElementType.Normal, X), (ElementType.Ice, X), (ElementType.Poison, H), (ElementType.Flying, H), (ElementType.Psychic, H), (ElementType.Bug, H), (ElementType.Rock, X), (ElementType.Ghost, Z), (ElementType.Dark, X), (ElementType.Steel, X), (ElementType.Fairy, H));
            Row(ElementType.Poison, (ElementType.Grass, X), (ElementType.Poison, H), (ElementType.Ground, H), (ElementType.Rock, H), (ElementType.Ghost, H), (ElementType.Steel, Z), (ElementType.Fairy, X));
            Row(ElementType.Ground, (ElementType.Fire, X), (ElementType.Electric, X), (ElementType.Grass, H), (ElementType.Poison, X), (ElementType.Flying, Z), (ElementType.Bug, H), (ElementType.Rock, X), (ElementType.Steel, X));
            Row(ElementType.Flying, (ElementType.Electric, H), (ElementType.Grass, X), (ElementType.Fighting, X), (ElementType.Bug, X), (ElementType.Rock, H), (ElementType.Steel, H));
            Row(ElementType.Psychic, (ElementType.Fighting, X), (ElementType.Poison, X), (ElementType.Psychic, H), (ElementType.Dark, Z), (ElementType.Steel, H));
            Row(ElementType.Bug, (ElementType.Fire, H), (ElementType.Grass, X), (ElementType.Fighting, H), (ElementType.Poison, H), (ElementType.Flying, H), (ElementType.Psychic, X), (ElementType.Ghost, H), (ElementType.Dark, X), (ElementType.Steel, H), (ElementType.Fairy, H));
            Row(ElementType.Rock, (ElementType.Fire, X), (ElementType.Ice, X), (ElementType.Fighting, H), (ElementType.Ground, H), (ElementType.Flying, X), (ElementType.Bug, X), (ElementType.Steel, H));
            Row(ElementType.Ghost, (ElementType.Normal, Z), (ElementType.Psychic, X), (ElementType.Ghost, X), (ElementType.Dark, H));
            Row(ElementType.Dragon, (ElementType.Dragon, X), (ElementType.Steel, H), (ElementType.Fairy, Z));
            Row(ElementType.Dark, (ElementType.Fighting, H), (ElementType.Psychic, X), (ElementType.Ghost, X), (ElementType.Dark, H), (ElementType.Fairy, H));
            Row(ElementType.Steel, (ElementType.Fire, H), (ElementType.Water, H), (ElementType.Electric, H), (ElementType.Ice, X), (ElementType.Rock, X), (ElementType.Steel, H), (ElementType.Fairy, X));
            Row(ElementType.Fairy, (ElementType.Fire, H), (ElementType.Fighting, X), (ElementType.Poison, H), (ElementType.Dragon, X), (ElementType.Dark, X), (ElementType.Steel, H));

            return c;
        }
    }
}
```

- [ ] **Step 4: Run → PASS.** `read_console(types=["error"])` empty.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Battle/Core/TypeChart.cs Assets/Scripts/Battle/Tests/TypeChartTests.cs
git commit -m "feat: add 18-type effectiveness chart"
```

---

### Task 4: StatStages

**Files:**
- Create: `Core/StatStages.cs`
- Create: `Tests/StatStagesTests.cs`

**Interfaces:**
- Produces: `static class StatStages` with `const int Min = -6, Max = 6;` and `static double Multiplier(int stage)`.
- Contract: `+n → (2+n)/2`, `-n → 2/(2+n)`; clamps stage to [-6, 6].

- [ ] **Step 1: Failing tests**

`Tests/StatStagesTests.cs`:
```csharp
using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class StatStagesTests
    {
        [Test] public void Neutral() => Assert.AreEqual(1.0, StatStages.Multiplier(0), 1e-6);
        [Test] public void PlusOne() => Assert.AreEqual(1.5, StatStages.Multiplier(1), 1e-6);
        [Test] public void PlusSix() => Assert.AreEqual(4.0, StatStages.Multiplier(6), 1e-6);
        [Test] public void MinusOne() => Assert.AreEqual(2.0 / 3.0, StatStages.Multiplier(-1), 1e-6);
        [Test] public void MinusSix() => Assert.AreEqual(0.25, StatStages.Multiplier(-6), 1e-6);
        [Test] public void ClampsBeyondRange() => Assert.AreEqual(4.0, StatStages.Multiplier(99), 1e-6);
    }
}
```

- [ ] **Step 2: Run → FAIL.**

- [ ] **Step 3: Implement `Core/StatStages.cs`**

```csharp
namespace MonsterCatcher.Battle
{
    public static class StatStages
    {
        public const int Min = -6;
        public const int Max = 6;

        public static double Multiplier(int stage)
        {
            if (stage < Min) stage = Min;
            if (stage > Max) stage = Max;
            return stage >= 0 ? (2.0 + stage) / 2.0 : 2.0 / (2.0 - stage);
        }
    }
}
```

- [ ] **Step 4: Run → PASS.**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Battle/Core/StatStages.cs Assets/Scripts/Battle/Tests/StatStagesTests.cs
git commit -m "feat: add stat-stage multiplier table"
```

---

### Task 5: Data ScriptableObjects

**Files:**
- Create: `Data/MoveData.cs`, `Data/SpeciesData.cs`, `Data/BattleSettings.cs`
- Create: `Tests/TestFactory.cs` (builds these in code for later tasks)
- Create: `Tests/DataTests.cs`

**Interfaces:**
- Produces:
  - `MoveData : ScriptableObject` — public fields: `string DisplayName; ElementType Type; MoveCategory Category; int Power; int Accuracy; int MaxPp; int Priority; StatusCondition InflictsStatus; int StatusChance; Stat StatToChange; int StatStageDelta; bool StatChangeTargetsSelf; int StatChangeChance;`
  - `SpeciesData : ScriptableObject` — `string DisplayName; ElementType Type1; ElementType Type2; bool HasSecondType; int BaseHp, BaseAttack, BaseDefense, BaseSpAttack, BaseSpDefense, BaseSpeed; List<MoveData> LearnableMoves;`
  - `BattleSettings : ScriptableObject` — tunables with defaults (see code).
  - `TestFactory` static helpers: `MoveData Move(...)`, `SpeciesData Species(...)`, `BattleSettings Settings()`, `Pokemon Mon(...)` (Pokemon added in Task 6 — leave `Mon` out until then).

- [ ] **Step 1: Implement `Data/MoveData.cs`**

```csharp
using UnityEngine;

namespace MonsterCatcher.Battle
{
    [CreateAssetMenu(menuName = "MonsterCatcher/Move", fileName = "Move")]
    public class MoveData : ScriptableObject
    {
        public string DisplayName = "New Move";
        public ElementType Type = ElementType.Normal;
        public MoveCategory Category = MoveCategory.Physical;
        [Min(0)] public int Power = 40;
        [Range(0, 100)] public int Accuracy = 100;   // 0 = never misses
        [Min(1)] public int MaxPp = 35;
        public int Priority = 0;

        [Header("Secondary effect")]
        public StatusCondition InflictsStatus = StatusCondition.None;
        [Range(0, 100)] public int StatusChance = 0;
        public Stat StatToChange = Stat.Attack;
        public int StatStageDelta = 0;
        public bool StatChangeTargetsSelf = false;
        [Range(0, 100)] public int StatChangeChance = 0;
    }
}
```

- [ ] **Step 2: Implement `Data/SpeciesData.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace MonsterCatcher.Battle
{
    [CreateAssetMenu(menuName = "MonsterCatcher/Species", fileName = "Species")]
    public class SpeciesData : ScriptableObject
    {
        public string DisplayName = "New Species";
        public ElementType Type1 = ElementType.Normal;
        public ElementType Type2 = ElementType.Normal;
        public bool HasSecondType = false;

        [Min(1)] public int BaseHp = 45;
        [Min(1)] public int BaseAttack = 49;
        [Min(1)] public int BaseDefense = 49;
        [Min(1)] public int BaseSpAttack = 65;
        [Min(1)] public int BaseSpDefense = 65;
        [Min(1)] public int BaseSpeed = 45;

        public List<MoveData> LearnableMoves = new List<MoveData>();

        public int BaseStat(Stat stat)
        {
            switch (stat)
            {
                case Stat.Hp: return BaseHp;
                case Stat.Attack: return BaseAttack;
                case Stat.Defense: return BaseDefense;
                case Stat.SpAttack: return BaseSpAttack;
                case Stat.SpDefense: return BaseSpDefense;
                case Stat.Speed: return BaseSpeed;
                default: return 1;
            }
        }
    }
}
```

- [ ] **Step 3: Implement `Data/BattleSettings.cs`**

```csharp
using UnityEngine;

namespace MonsterCatcher.Battle
{
    [CreateAssetMenu(menuName = "MonsterCatcher/Battle Settings", fileName = "BattleSettings")]
    public class BattleSettings : ScriptableObject
    {
        [Min(1)] public int MaxPartySize = 6;

        [Header("Status")]
        public double PoisonFraction = 1.0 / 8.0;
        public double BurnFraction = 1.0 / 16.0;
        public double BurnAttackMultiplier = 0.5;
        public double ParalysisSpeedMultiplier = 0.5;
        public double ParalysisFailChance = 0.25;
        [Min(1)] public int MinSleepTurns = 1;
        [Min(1)] public int MaxSleepTurns = 3;

        [Header("Critical hits")]
        public double CritChance = 1.0 / 24.0;
        public double CritMultiplier = 1.5;
    }
}
```

- [ ] **Step 4: Implement `Tests/TestFactory.cs` (species/move/settings only for now)**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace MonsterCatcher.Battle.Tests
{
    public static class TestFactory
    {
        public static BattleSettings Settings()
        {
            return ScriptableObject.CreateInstance<BattleSettings>();
        }

        public static MoveData Move(string name, ElementType type, MoveCategory cat,
            int power, int accuracy = 100, int priority = 0, int maxPp = 35)
        {
            var m = ScriptableObject.CreateInstance<MoveData>();
            m.DisplayName = name;
            m.Type = type;
            m.Category = cat;
            m.Power = power;
            m.Accuracy = accuracy;
            m.Priority = priority;
            m.MaxPp = maxPp;
            return m;
        }

        public static SpeciesData Species(string name, ElementType t1, int hp, int atk,
            int def, int spa, int spd, int spe, ElementType? t2 = null,
            List<MoveData> moves = null)
        {
            var s = ScriptableObject.CreateInstance<SpeciesData>();
            s.DisplayName = name;
            s.Type1 = t1;
            s.HasSecondType = t2.HasValue;
            s.Type2 = t2 ?? t1;
            s.BaseHp = hp; s.BaseAttack = atk; s.BaseDefense = def;
            s.BaseSpAttack = spa; s.BaseSpDefense = spd; s.BaseSpeed = spe;
            s.LearnableMoves = moves ?? new List<MoveData>();
            return s;
        }
    }
}
```

- [ ] **Step 5: Test that defaults and factory work**

`Tests/DataTests.cs`:
```csharp
using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class DataTests
    {
        [Test] public void SettingsDefaults()
        {
            var s = TestFactory.Settings();
            Assert.AreEqual(6, s.MaxPartySize);
            Assert.AreEqual(0.125, s.PoisonFraction, 1e-6);
        }

        [Test] public void SpeciesBaseStatLookup()
        {
            var s = TestFactory.Species("Testmon", ElementType.Fire, 50, 60, 40, 70, 50, 80);
            Assert.AreEqual(70, s.BaseStat(Stat.SpAttack));
        }
    }
}
```

- [ ] **Step 6: Run → PASS.** `read_console(types=["error"])` empty.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Battle/Data Assets/Scripts/Battle/Tests/TestFactory.cs Assets/Scripts/Battle/Tests/DataTests.cs
git commit -m "feat: add Move/Species/BattleSettings ScriptableObjects"
```

---

### Task 6: Pokemon + MoveSlot

**Files:**
- Create: `Core/MoveSlot.cs`, `Core/Pokemon.cs`
- Modify: `Tests/TestFactory.cs` (add `Mon(...)`)
- Create: `Tests/PokemonTests.cs`

**Interfaces:**
- Consumes: `SpeciesData`, `MoveData`, `Stat`, `StatusCondition`, `StatStages`.
- Produces:
  - `class MoveSlot { MoveData Move; int CurrentPp; int MaxPp; bool HasPp; bool TryUse(); }`
  - `class Pokemon` with: ctor `Pokemon(SpeciesData species, int level, IList<MoveData> moves)`; props `SpeciesData Species`, `int Level`, `int MaxHp`, `int CurrentHp`, `StatusCondition Status`, `int SleepTurnsLeft`, `IReadOnlyList<MoveSlot> Moves`, `bool IsFainted`;
    methods `int GetRawStat(Stat)`, `int GetStage(Stat)`, `int EffectiveStat(Stat stat, bool crit=false, bool ignoreNegative=false, bool ignorePositive=false)`, `int ChangeStage(Stat, int delta)` (returns applied delta), `void ResetStages()`, `void TakeDamage(int)`, `void Heal(int)`, `bool TryApplyStatus(StatusCondition, int sleepTurns=0)`, `void CureStatus()`.
- Contract (Level 50, IV0/EV0/neutral): `MaxHp = (2*baseHp*level)/100 + level + 10`; other `raw = (2*base*level)/100 + 5`. `EffectiveStat = max(1, floor(raw * StatStages.Multiplier(adjustedStage)))`; crit zeroes negative (attacker) / positive (defender) stages. Paralysis speed is **not** applied here (engine applies it).

- [ ] **Step 1: Failing tests**

`Tests/PokemonTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class PokemonTests
    {
        private static Pokemon Make(int hp, int atk, int def, int spa, int spd, int spe)
        {
            var species = TestFactory.Species("Mon", ElementType.Normal, hp, atk, def, spa, spd, spe);
            return new Pokemon(species, 50, new List<MoveData>());
        }

        [Test] public void MaxHpFormula()
        {
            // (2*45*50)/100 + 50 + 10 = 45 + 60 = 105
            Assert.AreEqual(105, Make(45, 49, 49, 65, 65, 45).MaxHp);
        }

        [Test] public void RawStatFormula()
        {
            // (2*49*50)/100 + 5 = 49 + 5 = 54
            Assert.AreEqual(54, Make(45, 49, 49, 65, 65, 45).GetRawStat(Stat.Attack));
        }

        [Test] public void StageRaisesEffectiveStat()
        {
            var p = Make(45, 100, 49, 65, 65, 45); // raw atk = 105
            p.ChangeStage(Stat.Attack, 1);          // x1.5
            Assert.AreEqual(157, p.EffectiveStat(Stat.Attack)); // floor(105*1.5)=157
        }

        [Test] public void CritIgnoresNegativeAttackerStage()
        {
            var p = Make(45, 100, 49, 65, 65, 45); // raw atk = 105
            p.ChangeStage(Stat.Attack, -2);         // x0.5 normally
            Assert.AreEqual(105, p.EffectiveStat(Stat.Attack, crit: true, ignoreNegative: true));
        }

        [Test] public void TakeDamageAndFaint()
        {
            var p = Make(45, 49, 49, 65, 65, 45);
            p.TakeDamage(1000);
            Assert.IsTrue(p.IsFainted);
            Assert.AreEqual(0, p.CurrentHp);
        }

        [Test] public void StatusAppliesOnlyWhenHealthy()
        {
            var p = Make(45, 49, 49, 65, 65, 45);
            Assert.IsTrue(p.TryApplyStatus(StatusCondition.Poison));
            Assert.IsFalse(p.TryApplyStatus(StatusCondition.Burn)); // already statused
            Assert.AreEqual(StatusCondition.Poison, p.Status);
        }
    }
}
```

- [ ] **Step 2: Run → FAIL.**

- [ ] **Step 3: Implement `Core/MoveSlot.cs`**

```csharp
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
```

- [ ] **Step 4: Implement `Core/Pokemon.cs`**

```csharp
using System;
using System.Collections.Generic;

namespace MonsterCatcher.Battle
{
    public sealed class Pokemon
    {
        private readonly int[] _rawStats = new int[6]; // indexed by (int)Stat
        private readonly int[] _stages = new int[6];
        private readonly List<MoveSlot> _moves = new List<MoveSlot>();

        public SpeciesData Species { get; }
        public int Level { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public StatusCondition Status { get; private set; }
        public int SleepTurnsLeft { get; set; }

        public IReadOnlyList<MoveSlot> Moves => _moves;
        public bool IsFainted => CurrentHp <= 0;

        public Pokemon(SpeciesData species, int level, IList<MoveData> moves)
        {
            Species = species;
            Level = level;

            MaxHp = (2 * species.BaseHp * level) / 100 + level + 10;
            _rawStats[(int)Stat.Hp] = MaxHp;
            _rawStats[(int)Stat.Attack] = CalcStat(species.BaseAttack, level);
            _rawStats[(int)Stat.Defense] = CalcStat(species.BaseDefense, level);
            _rawStats[(int)Stat.SpAttack] = CalcStat(species.BaseSpAttack, level);
            _rawStats[(int)Stat.SpDefense] = CalcStat(species.BaseSpDefense, level);
            _rawStats[(int)Stat.Speed] = CalcStat(species.BaseSpeed, level);
            CurrentHp = MaxHp;

            if (moves != null)
            {
                foreach (var m in moves)
                {
                    if (m != null) _moves.Add(new MoveSlot(m));
                }
            }
        }

        private static int CalcStat(int baseStat, int level)
        {
            return (2 * baseStat * level) / 100 + 5;
        }

        public int GetRawStat(Stat stat) => _rawStats[(int)stat];
        public int GetStage(Stat stat) => _stages[(int)stat];

        public int EffectiveStat(Stat stat, bool crit = false,
            bool ignoreNegative = false, bool ignorePositive = false)
        {
            int stage = _stages[(int)stat];
            if (crit && ignoreNegative && stage < 0) stage = 0;
            if (crit && ignorePositive && stage > 0) stage = 0;
            double value = _rawStats[(int)stat] * StatStages.Multiplier(stage);
            int result = (int)Math.Floor(value);
            return result < 1 ? 1 : result;
        }

        public int ChangeStage(Stat stat, int delta)
        {
            int before = _stages[(int)stat];
            int after = before + delta;
            if (after < StatStages.Min) after = StatStages.Min;
            if (after > StatStages.Max) after = StatStages.Max;
            _stages[(int)stat] = after;
            return after - before;
        }

        public void ResetStages()
        {
            for (int i = 0; i < _stages.Length; i++) _stages[i] = 0;
        }

        public void TakeDamage(int amount)
        {
            if (amount < 0) amount = 0;
            CurrentHp -= amount;
            if (CurrentHp < 0) CurrentHp = 0;
        }

        public void Heal(int amount)
        {
            if (amount < 0) amount = 0;
            CurrentHp += amount;
            if (CurrentHp > MaxHp) CurrentHp = MaxHp;
        }

        public bool TryApplyStatus(StatusCondition status, int sleepTurns = 0)
        {
            if (status == StatusCondition.None) return false;
            if (Status != StatusCondition.None || IsFainted) return false;
            Status = status;
            SleepTurnsLeft = status == StatusCondition.Sleep ? sleepTurns : 0;
            return true;
        }

        public void CureStatus()
        {
            Status = StatusCondition.None;
            SleepTurnsLeft = 0;
        }
    }
}
```

- [ ] **Step 5: Add `Mon(...)` to `Tests/TestFactory.cs`**

```csharp
// add inside TestFactory:
public static Pokemon Mon(SpeciesData species, params MoveData[] moves)
{
    return new Pokemon(species, 50, new List<MoveData>(moves));
}
```

- [ ] **Step 6: Run → PASS.** `read_console(types=["error"])` empty.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Battle/Core/MoveSlot.cs Assets/Scripts/Battle/Core/Pokemon.cs Assets/Scripts/Battle/Tests/TestFactory.cs Assets/Scripts/Battle/Tests/PokemonTests.cs
git commit -m "feat: add Pokemon runtime battler with stats, stages, status"
```

---

### Task 7: Party

**Files:**
- Create: `Core/Party.cs`
- Create: `Tests/PartyTests.cs`

**Interfaces:**
- Consumes: `Pokemon`.
- Produces: `class Party` — ctor `Party(BattleSide side, IList<Pokemon> members, int maxSize)`; props `BattleSide Side`, `IReadOnlyList<Pokemon> Members`, `int ActiveIndex`, `Pokemon Active`; methods `bool HasUsablePokemon()`, `bool CanSwitchTo(int index)`, `void SwitchTo(int index)`, `int FirstUsableIndex()`.
- Contract: `CanSwitchTo` is true iff index in range, not the active index, and that member is not fainted. `SwitchTo` resets stages of the **outgoing** Pokémon.

- [ ] **Step 1: Failing tests**

`Tests/PartyTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class PartyTests
    {
        private static Pokemon Mon()
        {
            return TestFactory.Mon(TestFactory.Species("M", ElementType.Normal, 45, 49, 49, 65, 65, 45));
        }

        [Test] public void ActiveIsFirstByDefault()
        {
            var party = new Party(BattleSide.Player, new List<Pokemon> { Mon(), Mon() }, 6);
            Assert.AreSame(party.Members[0], party.Active);
        }

        [Test] public void CannotSwitchToActiveOrFainted()
        {
            var a = Mon(); var b = Mon();
            var party = new Party(BattleSide.Player, new List<Pokemon> { a, b }, 6);
            Assert.IsFalse(party.CanSwitchTo(0)); // active
            b.TakeDamage(1000);
            Assert.IsFalse(party.CanSwitchTo(1)); // fainted
        }

        [Test] public void SwitchResetsOutgoingStages()
        {
            var a = Mon(); var b = Mon();
            a.ChangeStage(Stat.Attack, 2);
            var party = new Party(BattleSide.Player, new List<Pokemon> { a, b }, 6);
            party.SwitchTo(1);
            Assert.AreEqual(1, party.ActiveIndex);
            Assert.AreEqual(0, a.GetStage(Stat.Attack));
        }

        [Test] public void HasUsableReflectsFaints()
        {
            var a = Mon(); var b = Mon();
            var party = new Party(BattleSide.Player, new List<Pokemon> { a, b }, 6);
            a.TakeDamage(1000); b.TakeDamage(1000);
            Assert.IsFalse(party.HasUsablePokemon());
        }
    }
}
```

- [ ] **Step 2: Run → FAIL.**

- [ ] **Step 3: Implement `Core/Party.cs`**

```csharp
using System;
using System.Collections.Generic;

namespace MonsterCatcher.Battle
{
    public sealed class Party
    {
        private readonly List<Pokemon> _members;

        public BattleSide Side { get; }
        public int MaxSize { get; }
        public int ActiveIndex { get; private set; }

        public Party(BattleSide side, IList<Pokemon> members, int maxSize)
        {
            if (members == null || members.Count == 0)
                throw new ArgumentException("Party needs at least one Pokemon.");
            MaxSize = maxSize;
            if (members.Count > maxSize)
                throw new ArgumentException("Party exceeds max size.");
            Side = side;
            _members = new List<Pokemon>(members);
            ActiveIndex = 0;
        }

        public IReadOnlyList<Pokemon> Members => _members;
        public Pokemon Active => _members[ActiveIndex];

        public bool HasUsablePokemon()
        {
            foreach (var m in _members)
                if (!m.IsFainted) return true;
            return false;
        }

        public bool CanSwitchTo(int index)
        {
            return index >= 0 && index < _members.Count
                   && index != ActiveIndex && !_members[index].IsFainted;
        }

        public void SwitchTo(int index)
        {
            if (index < 0 || index >= _members.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            _members[ActiveIndex].ResetStages();
            ActiveIndex = index;
        }

        public int FirstUsableIndex()
        {
            for (int i = 0; i < _members.Count; i++)
                if (!_members[i].IsFainted) return i;
            return -1;
        }
    }
}
```

- [ ] **Step 4: Run → PASS.**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Battle/Core/Party.cs Assets/Scripts/Battle/Tests/PartyTests.cs
git commit -m "feat: add Party with switch rules"
```

---

### Task 8: DamageCalculator

**Files:**
- Create: `Core/DamageCalculator.cs`
- Create: `Tests/DamageTests.cs`

**Interfaces:**
- Consumes: `Pokemon`, `MoveData`, `BattleSettings`, `IRng`, `TypeChart`.
- Produces:
  - `struct DamageResult { bool Hit; int Damage; double Effectiveness; bool WasCritical; }`
  - `static class DamageCalculator { static DamageResult Calculate(Pokemon attacker, Pokemon defender, MoveData move, BattleSettings settings, IRng rng); }`
- Contract: accuracy roll first (`Accuracy==0` ⇒ always hits). Status/0-power moves return `Hit=true, Damage=0`. Immune (eff 0) ⇒ `Damage=0`. Otherwise damage per the formula in the spec; `Damage >= 1` when eff > 0. RNG order: `Roll(accuracy)` → `Roll(critChance)` → `IntInclusive(85,100)`.

- [ ] **Step 1: Failing tests** (FakeRng: `DefaultRoll=false` ⇒ no crit; `IntResult=int.MaxValue` ⇒ factor 100)

`Tests/DamageTests.cs`:
```csharp
using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class DamageTests
    {
        private static BattleSettings Settings() => TestFactory.Settings();

        private static Pokemon Attacker(ElementType type)
        {
            var s = TestFactory.Species("A", type, 100, 120, 100, 120, 100, 100);
            return TestFactory.Mon(s);
        }

        [Test] public void MissReturnsNoDamage()
        {
            var move = TestFactory.Move("Shaky", ElementType.Normal, MoveCategory.Physical, 50, accuracy: 50);
            var rng = new FakeRng(); // Roll(0.5) -> DefaultRoll false -> miss
            var res = DamageCalculator.Calculate(Attacker(ElementType.Normal),
                Attacker(ElementType.Normal), move, Settings(), rng);
            Assert.IsFalse(res.Hit);
            Assert.AreEqual(0, res.Damage);
        }

        [Test] public void ImmunityDealsZero()
        {
            var move = TestFactory.Move("Tackle", ElementType.Normal, MoveCategory.Physical, 50);
            var defender = TestFactory.Mon(TestFactory.Species("Ghost", ElementType.Ghost, 100, 80, 80, 80, 80, 80));
            var res = DamageCalculator.Calculate(Attacker(ElementType.Normal), defender, move, Settings(), new FakeRng());
            Assert.IsTrue(res.Hit);
            Assert.AreEqual(0, res.Damage);
            Assert.AreEqual(0.0, res.Effectiveness, 1e-6);
        }

        [Test] public void DeterministicDamageMaxRollNoCrit()
        {
            // attacker SpAtk raw = (2*120*50)/100+5 = 125; defender SpDef raw = 85
            // level term = floor(2*50/5)+2 = 22; base = floor(floor(22*90*125/85)/50)+2
            //   = floor(floor(2911.76..)/50)+2 -> floor(2911*? ) compute precisely below.
            var move = TestFactory.Move("Ember", ElementType.Fire, MoveCategory.Special, 90);
            var defender = TestFactory.Mon(TestFactory.Species("Leaf", ElementType.Grass, 100, 80, 80, 80, 80, 80));
            var rng = new FakeRng(); // no crit, factor 100
            var res = DamageCalculator.Calculate(Attacker(ElementType.Fire), defender, move, Settings(), rng);
            // STAB 1.5 (Fire vs Fire attacker) * type 2.0 (Fire vs Grass) * 1.0 random * no crit
            Assert.IsTrue(res.Damage > 0);
            Assert.AreEqual(2.0, res.Effectiveness, 1e-6);
            Assert.IsFalse(res.WasCritical);
            // Lock the exact number once first run prints it (see Step 4 note).
        }

        [Test] public void CritUsesMultiplier()
        {
            var move = TestFactory.Move("Ember", ElementType.Fire, MoveCategory.Special, 90);
            var defender = TestFactory.Mon(TestFactory.Species("Leaf", ElementType.Grass, 100, 80, 80, 80, 80, 80));
            var noCrit = DamageCalculator.Calculate(Attacker(ElementType.Fire), defender, move, Settings(), new FakeRng());
            var crit = DamageCalculator.Calculate(Attacker(ElementType.Fire), defender, move, Settings(),
                new FakeRng().Enqueue(true)); // accuracy p>=1 short-circuits; first queued bool consumed by crit roll
            Assert.Greater(crit.Damage, noCrit.Damage);
        }

        [Test] public void BurnHalvesPhysical()
        {
            var move = TestFactory.Move("Tackle", ElementType.Normal, MoveCategory.Physical, 80);
            var attacker = Attacker(ElementType.Normal);
            attacker.TryApplyStatus(StatusCondition.Burn);
            var defender = TestFactory.Mon(TestFactory.Species("D", ElementType.Water, 100, 80, 80, 80, 80, 80));
            var healthy = Attacker(ElementType.Normal);
            var burned = DamageCalculator.Calculate(attacker, defender, move, Settings(), new FakeRng());
            var normal = DamageCalculator.Calculate(healthy, defender, move, Settings(), new FakeRng());
            Assert.Less(burned.Damage, normal.Damage);
        }
    }
}
```

> Note: `CritUsesMultiplier` relies on the accuracy roll being short-circuited because `Accuracy==100 ⇒ p=1.0 ⇒ Roll returns true without dequeuing`. The first dequeued bool therefore lands on the crit roll. Keep that ordering when implementing.

- [ ] **Step 2: Run → FAIL.**

- [ ] **Step 3: Implement `Core/DamageCalculator.cs`**

```csharp
using System;

namespace MonsterCatcher.Battle
{
    public struct DamageResult
    {
        public bool Hit;
        public int Damage;
        public double Effectiveness;
        public bool WasCritical;
    }

    public static class DamageCalculator
    {
        public static DamageResult Calculate(Pokemon attacker, Pokemon defender,
            MoveData move, BattleSettings settings, IRng rng)
        {
            var result = new DamageResult { Hit = true, Effectiveness = 1.0 };

            if (move.Accuracy > 0 && !rng.Roll(move.Accuracy / 100.0))
            {
                result.Hit = false;
                return result;
            }

            if (move.Category == MoveCategory.Status || move.Power <= 0)
                return result;

            double eff = TypeChart.Effectiveness(move.Type,
                defender.Species.Type1, defender.Species.Type2, defender.Species.HasSecondType);
            result.Effectiveness = eff;
            if (eff <= 0.0)
            {
                result.Damage = 0;
                return result;
            }

            bool crit = rng.Roll(settings.CritChance);
            result.WasCritical = crit;

            int a, d;
            if (move.Category == MoveCategory.Physical)
            {
                a = attacker.EffectiveStat(Stat.Attack, crit, ignoreNegative: true);
                d = defender.EffectiveStat(Stat.Defense, crit, ignorePositive: true);
            }
            else
            {
                a = attacker.EffectiveStat(Stat.SpAttack, crit, ignoreNegative: true);
                d = defender.EffectiveStat(Stat.SpDefense, crit, ignorePositive: true);
            }

            double levelTerm = Math.Floor(2.0 * attacker.Level / 5.0) + 2.0;
            double baseDmg = Math.Floor(Math.Floor(levelTerm * move.Power * a / (double)d) / 50.0) + 2.0;

            double mod = 1.0;
            bool stab = move.Type == attacker.Species.Type1 ||
                        (attacker.Species.HasSecondType && move.Type == attacker.Species.Type2);
            if (stab) mod *= 1.5;
            mod *= eff;
            if (crit) mod *= settings.CritMultiplier;
            mod *= rng.IntInclusive(85, 100) / 100.0;
            if (attacker.Status == StatusCondition.Burn && move.Category == MoveCategory.Physical)
                mod *= settings.BurnAttackMultiplier;

            int dmg = (int)Math.Floor(baseDmg * mod);
            result.Damage = dmg < 1 ? 1 : dmg;
            return result;
        }
    }
}
```

- [ ] **Step 4: Run → PASS.** If `DeterministicDamageMaxRollNoCrit` should assert an exact number, first run prints `res.Damage`; copy that constant into an explicit `Assert.AreEqual(<value>, res.Damage)` and re-run to lock it. `read_console(types=["error"])` empty.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Battle/Core/DamageCalculator.cs Assets/Scripts/Battle/Tests/DamageTests.cs
git commit -m "feat: add authentic damage formula"
```

---

### Task 9: BattleAction + BattleEvent

**Files:**
- Create: `Core/BattleAction.cs`, `Core/BattleEvent.cs`

**Interfaces:**
- Produces:
  - `struct BattleAction` with `enum ActionKind { Move, Switch }`, factory `BattleAction.UseMove(int moveIndex)`, `BattleAction.SwitchTo(int partyIndex)`, fields `Kind`, `Index`.
  - `abstract class BattleEvent` + concrete records: `MoveUsedEvent(Pokemon user, MoveData move)`, `MissedEvent(Pokemon user, MoveData move)`, `DamageEvent(Pokemon target, int amount, double effectiveness, bool wasCritical)`, `StatusInflictedEvent(Pokemon target, StatusCondition status)`, `StatusDamageEvent(Pokemon target, StatusCondition status, int amount)`, `StatChangedEvent(Pokemon target, Stat stat, int deltaStages)`, `ActionPreventedEvent(Pokemon user, StatusCondition reason)`, `FaintedEvent(Pokemon target)`, `SwitchedInEvent(BattleSide side, Pokemon pokemon)`, `BattleEndedEvent(BattleResult result)`.
- Contract: events are immutable data carriers; no logic.

- [ ] **Step 1: Implement `Core/BattleAction.cs`**

```csharp
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
```

- [ ] **Step 2: Implement `Core/BattleEvent.cs`**

```csharp
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
}
```

- [ ] **Step 3: Verify compile** via existing tests (`run_tests(mode="EditMode")`), `read_console(types=["error"])` empty.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Battle/Core/BattleAction.cs Assets/Scripts/Battle/Core/BattleEvent.cs
git commit -m "feat: add battle action and event types"
```

---

### Task 10: BattleEngine

**Files:**
- Create: `Core/BattleEngine.cs`
- Create: `Tests/BattleEngineTests.cs`

**Interfaces:**
- Consumes: all of the above.
- Produces: `class BattleEngine`
  - ctor `BattleEngine(Party player, Party enemy, BattleSettings settings, IRng rng)`
  - props `BattleResult Result`, `bool IsOver`, `Party Player`, `Party Enemy`
  - `bool AwaitingForcedSwitch(BattleSide side)`
  - `IReadOnlyList<BattleEvent> ExecuteTurn(BattleAction playerAction, BattleAction enemyAction)`
  - `IReadOnlyList<BattleEvent> ResolveForcedSwitch(BattleSide side, int partyIndex)`
- Contract (turn order): switches resolve before moves; moves ordered by `move.Priority` desc, then effective speed desc (paralysis applies `settings.ParalysisSpeedMultiplier`), ties broken by `rng`. End-of-turn: poison/burn tick for each non-fainted active. KO sets `AwaitingForcedSwitch` if a replacement exists, else ends the battle. `ExecuteTurn` throws `InvalidOperationException` if `IsOver` or a forced switch is pending.

- [ ] **Step 1: Failing tests**

`Tests/BattleEngineTests.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class BattleEngineTests
    {
        private BattleSettings _settings;

        private MoveData _tackle;
        private MoveData _quickAttack;

        [SetUp] public void Setup()
        {
            _settings = TestFactory.Settings();
            _tackle = TestFactory.Move("Tackle", ElementType.Normal, MoveCategory.Physical, 40);
            _quickAttack = TestFactory.Move("Quick Attack", ElementType.Normal, MoveCategory.Physical, 40, priority: 1);
        }

        private Pokemon Fast() => TestFactory.Mon(TestFactory.Species("Fast", ElementType.Normal, 60, 80, 60, 60, 60, 120), _tackle, _quickAttack);
        private Pokemon Slow() => TestFactory.Mon(TestFactory.Species("Slow", ElementType.Normal, 60, 80, 60, 60, 60, 20), _tackle, _quickAttack);

        private static Party P(BattleSide side, params Pokemon[] mons) =>
            new Party(side, mons.ToList(), 6);

        [Test] public void FasterMovesFirst()
        {
            var player = P(BattleSide.Player, Fast());
            var enemy = P(BattleSide.Enemy, Slow());
            var engine = new BattleEngine(player, enemy, _settings, new FakeRng());
            var events = engine.ExecuteTurn(BattleAction.UseMove(0), BattleAction.UseMove(0));
            var firstMove = events.OfType<MoveUsedEvent>().First();
            Assert.AreSame(player.Active, firstMove.User);
        }

        [Test] public void PriorityBeatsSpeed()
        {
            var player = P(BattleSide.Player, Slow());  // slow uses Quick Attack (priority 1)
            var enemy = P(BattleSide.Enemy, Fast());    // fast uses Tackle (priority 0)
            var engine = new BattleEngine(player, enemy, _settings, new FakeRng());
            var events = engine.ExecuteTurn(BattleAction.UseMove(1), BattleAction.UseMove(0));
            var firstMove = events.OfType<MoveUsedEvent>().First();
            Assert.AreSame(player.Active, firstMove.User);
        }

        [Test] public void FaintTriggersForcedSwitchThenContinues()
        {
            var glass = TestFactory.Mon(TestFactory.Species("Glass", ElementType.Normal, 1, 10, 10, 10, 10, 10), _tackle);
            var bench = TestFactory.Mon(TestFactory.Species("Bench", ElementType.Normal, 60, 80, 60, 60, 60, 50), _tackle);
            var player = P(BattleSide.Player, Fast());
            var enemy = P(BattleSide.Enemy, glass, bench);
            var engine = new BattleEngine(player, enemy, _settings, new FakeRng());
            engine.ExecuteTurn(BattleAction.UseMove(0), BattleAction.UseMove(0));
            Assert.IsTrue(engine.AwaitingForcedSwitch(BattleSide.Enemy));
            var sw = engine.ResolveForcedSwitch(BattleSide.Enemy, 1);
            Assert.IsTrue(sw.OfType<SwitchedInEvent>().Any());
            Assert.IsFalse(engine.IsOver);
        }

        [Test] public void LastFaintEndsBattle()
        {
            var glass = TestFactory.Mon(TestFactory.Species("Glass", ElementType.Normal, 1, 10, 10, 10, 10, 10), _tackle);
            var player = P(BattleSide.Player, Fast());
            var enemy = P(BattleSide.Enemy, glass);
            var engine = new BattleEngine(player, enemy, _settings, new FakeRng());
            var events = engine.ExecuteTurn(BattleAction.UseMove(0), BattleAction.UseMove(0));
            Assert.IsTrue(engine.IsOver);
            Assert.AreEqual(BattleResult.PlayerWon, engine.Result);
            Assert.IsTrue(events.OfType<BattleEndedEvent>().Any());
        }

        [Test] public void PoisonTicksAtEndOfTurn()
        {
            var poison = TestFactory.Move("Poison Sting", ElementType.Poison, MoveCategory.Physical, 15);
            poison.InflictsStatus = StatusCondition.Poison;
            poison.StatusChance = 100;
            var attacker = TestFactory.Mon(TestFactory.Species("P", ElementType.Poison, 60, 80, 60, 60, 60, 120), poison);
            var victim = TestFactory.Mon(TestFactory.Species("V", ElementType.Normal, 200, 80, 200, 60, 200, 5), _tackle);
            var player = P(BattleSide.Player, attacker);
            var enemy = P(BattleSide.Enemy, victim);
            var engine = new BattleEngine(player, enemy, _settings, new FakeRng());
            engine.ExecuteTurn(BattleAction.UseMove(0), BattleAction.UseMove(0));
            Assert.AreEqual(StatusCondition.Poison, victim.Status);
            Assert.Less(victim.CurrentHp, victim.MaxHp); // hit + poison tick
        }

        [Test] public void SwitchHappensBeforeMove()
        {
            var player = P(BattleSide.Player, Fast(), Slow());
            var enemy = P(BattleSide.Enemy, Fast());
            var engine = new BattleEngine(player, enemy, _settings, new FakeRng());
            var events = engine.ExecuteTurn(BattleAction.SwitchTo(1), BattleAction.UseMove(0));
            int switchIdx = events.FindIndex(e => e is SwitchedInEvent);
            int firstMoveIdx = events.FindIndex(e => e is MoveUsedEvent);
            Assert.GreaterOrEqual(firstMoveIdx, 0);
            Assert.Less(switchIdx, firstMoveIdx);
        }
    }
}
```
(Uses `List<BattleEvent>.FindIndex`; `ExecuteTurn` returns `IReadOnlyList<BattleEvent>` backed by `List`, so cast or expose as `List`. Implementation returns `List<BattleEvent>` typed as `IReadOnlyList`; the test calls `.FindIndex` via `((List<BattleEvent>)events)` — see Step 3 note.)

- [ ] **Step 2: Run → FAIL.**

- [ ] **Step 3: Implement `Core/BattleEngine.cs`**

> Note: `ExecuteTurn`/`ResolveForcedSwitch` return the concrete `List<BattleEvent>` (typed as `IReadOnlyList<BattleEvent>`). In the `SwitchHappensBeforeMove` test, wrap with `var list = (System.Collections.Generic.List<BattleEvent>)events;` before `FindIndex`, or change the test to `events.ToList().FindIndex(...)`. Pick one and keep it consistent.

```csharp
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

            // 1. Switches resolve first.
            var pending = new List<(BattleSide side, BattleAction action)>();
            ApplyOrQueue(BattleSide.Player, playerAction, events, pending);
            ApplyOrQueue(BattleSide.Enemy, enemyAction, events, pending);

            // 2. Order remaining move actions by priority, then effective speed.
            pending.Sort((x, y) => CompareOrder(x, y));

            // 3. Execute moves.
            foreach (var (side, action) in pending)
            {
                var user = PartyOf(side).Active;
                if (user.IsFainted) continue;
                ExecuteMove(side, action.Index, events);
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
            List<BattleEvent> events, List<(BattleSide, BattleAction)> pending)
        {
            if (action.Kind == ActionKind.Switch)
            {
                var party = PartyOf(side);
                if (party.CanSwitchTo(action.Index))
                {
                    party.SwitchTo(action.Index);
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

            // Sleep: act on wake turn.
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

            slot.TryUse();
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
            }

            ApplySecondaryEffects(user, target, slot.Move, events);

            if (target.IsFainted)
            {
                events.Add(new FaintedEvent(target));
                CheckBattleEnd(events);
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
            // Active is fainted, so SwitchTo just changes the index (ResetStages on fainted is harmless).
            party.SwitchTo(target);
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
```

- [ ] **Step 4: Run → PASS** (adjust the `FindIndex` cast per the Step 3 note if needed). `read_console(types=["error"])` empty.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Battle/Core/BattleEngine.cs Assets/Scripts/Battle/Tests/BattleEngineTests.cs
git commit -m "feat: add turn-based battle engine"
```

---

### Task 11: SimpleAI

**Files:**
- Create: `Control/SimpleAI.cs`
- Create: `Tests/SimpleAITests.cs`

**Interfaces:**
- Consumes: `Party`, `Pokemon`, `MoveData`, `TypeChart`, `IRng`, `BattleSettings`.
- Produces: `class SimpleAI` with ctor `SimpleAI(double switchHpThreshold = 0.3, double dangerEffectiveness = 2.0)` and `BattleAction ChooseAction(Party self, Party opponent)`.
- Contract: if active HP ratio < threshold AND the opponent's typing is ≥ `dangerEffectiveness` super-effective against the active AND a benched, non-fainted Pokémon takes strictly less type damage from the opponent's typing → `SwitchTo(bestBenchIndex)`. Otherwise pick the move with the highest score `Power * typeEff * (stab?1.5:1)`; status/0-power moves score 0. Returns `UseMove(index)`.

- [ ] **Step 1: Failing tests**

`Tests/SimpleAITests.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MonsterCatcher.Battle.Tests
{
    public class SimpleAITests
    {
        private static Party P(BattleSide side, params Pokemon[] mons) =>
            new Party(side, mons.ToList(), 6);

        [Test] public void PicksStrongestEffectiveMove()
        {
            var ember = TestFactory.Move("Ember", ElementType.Fire, MoveCategory.Special, 40);
            var watergun = TestFactory.Move("Water Gun", ElementType.Water, MoveCategory.Special, 40);
            var self = TestFactory.Mon(TestFactory.Species("Mix", ElementType.Normal, 60, 60, 60, 60, 60, 60), ember, watergun);
            var foe = TestFactory.Mon(TestFactory.Species("Rock", ElementType.Rock, 60, 60, 60, 60, 60, 60)); // Water 2x, Fire 0.5x
            var ai = new SimpleAI();
            var action = ai.ChooseAction(P(BattleSide.Enemy, self), P(BattleSide.Player, foe));
            Assert.AreEqual(ActionKind.Move, action.Kind);
            Assert.AreEqual(1, action.Index); // Water Gun
        }

        [Test] public void SwitchesWhenOutmatchedAndLowHp()
        {
            var tackle = TestFactory.Move("Tackle", ElementType.Normal, MoveCategory.Physical, 40);
            // Active is Grass, opponent is Fire -> Fire 2x vs Grass (danger).
            var active = TestFactory.Mon(TestFactory.Species("Grass", ElementType.Grass, 60, 60, 60, 60, 60, 60), tackle);
            active.TakeDamage(active.MaxHp - 1); // very low HP
            // Bench is Water -> Fire 0.5x vs Water (safer).
            var bench = TestFactory.Mon(TestFactory.Species("Water", ElementType.Water, 60, 60, 60, 60, 60, 60), tackle);
            var foe = TestFactory.Mon(TestFactory.Species("Fire", ElementType.Fire, 60, 60, 60, 60, 60, 60), tackle);
            var ai = new SimpleAI();
            var action = ai.ChooseAction(P(BattleSide.Enemy, active, bench), P(BattleSide.Player, foe));
            Assert.AreEqual(ActionKind.Switch, action.Kind);
            Assert.AreEqual(1, action.Index);
        }

        [Test] public void DoesNotSwitchWhenHealthy()
        {
            var tackle = TestFactory.Move("Tackle", ElementType.Normal, MoveCategory.Physical, 40);
            var active = TestFactory.Mon(TestFactory.Species("Grass", ElementType.Grass, 60, 60, 60, 60, 60, 60), tackle);
            var bench = TestFactory.Mon(TestFactory.Species("Water", ElementType.Water, 60, 60, 60, 60, 60, 60), tackle);
            var foe = TestFactory.Mon(TestFactory.Species("Fire", ElementType.Fire, 60, 60, 60, 60, 60, 60), tackle);
            var ai = new SimpleAI();
            var action = ai.ChooseAction(P(BattleSide.Enemy, active, bench), P(BattleSide.Player, foe));
            Assert.AreEqual(ActionKind.Move, action.Kind);
        }
    }
}
```

- [ ] **Step 2: Run → FAIL.**

- [ ] **Step 3: Implement `Control/SimpleAI.cs`**

```csharp
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
```

- [ ] **Step 4: Run → PASS.**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Battle/Control/SimpleAI.cs Assets/Scripts/Battle/Tests/SimpleAITests.cs
git commit -m "feat: add simple battle AI with switching"
```

---

### Task 12: SampleData + BattleController skeleton

**Files:**
- Create: `Fixtures/SampleData.cs`
- Create: `Control/BattleController.cs`

**Interfaces:**
- Consumes: everything.
- Produces:
  - `static class SampleData` — `BattleSettings CreateSettings()`, `List<SpeciesData> CreateSpecies()`, `Party CreatePlayerParty(BattleSettings)`, `Party CreateEnemyParty(BattleSettings)` (code-built starter roster for demo/playtest before assets exist).
  - `class BattleController : MonoBehaviour` — fields for parties/settings, builds a `BattleEngine`, exposes `event Action<IReadOnlyList<BattleEvent>> TurnResolved`, methods `void StartBattle()`, `void PlayerUseMove(int index)`, `void PlayerSwitch(int index)`, `void ResolvePlayerForcedSwitch(int index)`. UI binding is a TODO completed after the MCP bridge is live.
- Contract: controller contains **no battle math** — it only gathers the player action, asks `SimpleAI` for the enemy action, calls the engine, and raises events. Forced-enemy-switch is resolved via `SimpleAI`/first-usable automatically.

- [ ] **Step 1: Implement `Fixtures/SampleData.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace MonsterCatcher.Battle
{
    public static class SampleData
    {
        public static BattleSettings CreateSettings()
        {
            return ScriptableObject.CreateInstance<BattleSettings>();
        }

        private static MoveData Move(string name, ElementType type, MoveCategory cat,
            int power, int accuracy = 100, int priority = 0)
        {
            var m = ScriptableObject.CreateInstance<MoveData>();
            m.DisplayName = name; m.Type = type; m.Category = cat;
            m.Power = power; m.Accuracy = accuracy; m.Priority = priority; m.MaxPp = 25;
            return m;
        }

        private static SpeciesData Species(string name, ElementType t1, int hp, int atk,
            int def, int spa, int spd, int spe)
        {
            var s = ScriptableObject.CreateInstance<SpeciesData>();
            s.DisplayName = name; s.Type1 = t1; s.HasSecondType = false; s.Type2 = t1;
            s.BaseHp = hp; s.BaseAttack = atk; s.BaseDefense = def;
            s.BaseSpAttack = spa; s.BaseSpDefense = spd; s.BaseSpeed = spe;
            return s;
        }

        public static Party CreatePlayerParty(BattleSettings settings)
        {
            var ember = Move("Ember", ElementType.Fire, MoveCategory.Special, 40);
            var scratch = Move("Scratch", ElementType.Normal, MoveCategory.Physical, 40);
            var fireMon = Species("Flarepup", ElementType.Fire, 55, 60, 45, 70, 50, 70);
            var p1 = new Pokemon(fireMon, 50, new List<MoveData> { ember, scratch });

            var vine = Move("Vine Whip", ElementType.Grass, MoveCategory.Physical, 45);
            var tackle = Move("Tackle", ElementType.Normal, MoveCategory.Physical, 40);
            var grassMon = Species("Leafkit", ElementType.Grass, 60, 55, 60, 65, 60, 50);
            var p2 = new Pokemon(grassMon, 50, new List<MoveData> { vine, tackle });

            return new Party(BattleSide.Player, new List<Pokemon> { p1, p2 }, settings.MaxPartySize);
        }

        public static Party CreateEnemyParty(BattleSettings settings)
        {
            var bubble = Move("Bubble", ElementType.Water, MoveCategory.Special, 40);
            var tackle = Move("Tackle", ElementType.Normal, MoveCategory.Physical, 40);
            var waterMon = Species("Dewfin", ElementType.Water, 60, 50, 55, 65, 60, 60);
            var e1 = new Pokemon(waterMon, 50, new List<MoveData> { bubble, tackle });

            var peck = Move("Peck", ElementType.Flying, MoveCategory.Physical, 35);
            var quick = Move("Quick Attack", ElementType.Normal, MoveCategory.Physical, 40, priority: 1);
            var flyMon = Species("Breezewing", ElementType.Flying, 50, 55, 45, 50, 50, 80);
            var e2 = new Pokemon(flyMon, 50, new List<MoveData> { peck, quick });

            return new Party(BattleSide.Enemy, new List<Pokemon> { e1, e2 }, settings.MaxPartySize);
        }
    }
}
```

- [ ] **Step 2: Implement `Control/BattleController.cs`**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterCatcher.Battle
{
    public sealed class BattleController : MonoBehaviour
    {
        [SerializeField] private BattleSettings _settings;
        [SerializeField] private int _rngSeed = 0;
        [SerializeField] private bool _useSampleData = true;

        private BattleEngine _engine;
        private SimpleAI _ai;
        private Party _player;
        private Party _enemy;

        public event Action<IReadOnlyList<BattleEvent>> TurnResolved;
        public BattleEngine Engine => _engine;

        public void StartBattle()
        {
            var settings = _settings != null ? _settings : SampleData.CreateSettings();
            if (_useSampleData)
            {
                _player = SampleData.CreatePlayerParty(settings);
                _enemy = SampleData.CreateEnemyParty(settings);
            }
            _ai = new SimpleAI();
            IRng rng = _rngSeed != 0 ? new DefaultRng(_rngSeed) : new DefaultRng();
            _engine = new BattleEngine(_player, _enemy, settings, rng);
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

            TurnResolved?.Invoke(events);
        }

        public void ResolvePlayerForcedSwitch(int partyIndex)
        {
            if (_engine == null || !_engine.AwaitingForcedSwitch(BattleSide.Player)) return;
            var events = _engine.ResolveForcedSwitch(BattleSide.Player, partyIndex);
            TurnResolved?.Invoke(events);
        }

        // TODO (after MCP bridge live): bind StartBattle/PlayerUseMove/PlayerSwitch to UI buttons,
        // render TurnResolved events as HP-bar tweens and battle-log text.
    }
}
```

- [ ] **Step 3: Verify full compile + run the whole EditMode suite**

Run (MCP): `run_tests(mode="EditMode")`
Expected: all tests pass; `read_console(types=["error"])` empty.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Battle/Fixtures/SampleData.cs Assets/Scripts/Battle/Control/BattleController.cs
git commit -m "feat: add sample roster and battle controller skeleton"
```

---

## Post-plan: Unity wiring (after MCP bridge is live)

These are **not** code tasks; they run through Unity-MCP after the Claude Code restart:

1. Create `BattleSettings`, `SpeciesData`, `MoveData` **assets** (or keep `SampleData` for the first playtest).
2. New 2D scene from `Assets/Settings/Lit2DSceneTemplate.scenetemplate`; add a `BattleController` GameObject.
3. Simple uGUI: two HP bars, four move buttons, a party/switch panel, a scrolling battle log bound to `TurnResolved`.
4. Run the EditMode suite in the Test Runner; screenshot the battle scene to verify.

---

## Self-Review

**Spec coverage:** stats & formula (Task 6, 8), 18-type chart (Task 3), damage incl. STAB/crit/random/burn (Task 8), status Poison/Burn/Paralysis/Sleep (Task 6 model, Task 10 application), stat stages (Task 4, 6), team + switching + forced switch + win (Task 7, 10), configurable party size (Task 5 `BattleSettings.MaxPartySize`, used in Task 7/12), AI with switching (Task 11), BattleEvents (Task 9), tests (every task), delivery split (post-plan section). ✔ All spec sections map to a task.

**Placeholder scan:** no "TBD/TODO" in code except the one explicit, intentional UI-wiring TODO in `BattleController` (post-MCP, out of scope for the code phase) and the "lock the exact damage constant" note in Task 8 Step 4 (a real, actionable instruction). No vague "add error handling" steps.

**Type consistency:** `Pokemon.EffectiveStat(stat, crit, ignoreNegative, ignorePositive)` used identically in Task 6 and Task 8; `DamageResult` fields match between Task 8 definition and Task 10 usage; `BattleAction.UseMove/SwitchTo` consistent across Tasks 9–12; `Party.CanSwitchTo/SwitchTo/FirstUsableIndex/HasUsablePokemon` consistent Tasks 7/10/11/12; `TypeChart.Effectiveness` 2-arg and 4-arg overloads consistent. ✔
