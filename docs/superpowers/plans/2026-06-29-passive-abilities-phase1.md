# Passive Abilities (Phase 1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a fully-functional passive-ability system — 110 abilities rolled onto monsters (player + enemy), affecting battle via engine hooks.

**Architecture:** Identity/data catalog (`AbilityInfo` + data-driven `AbilityEffect`) lives in the **Map** core (pure C#) so `RunState` can roll and the future UI can display. Behavior lives in the **Battle** core: a single `AbilityApplier` reads each active monster's `AbilityEffect`s at well-defined hook points in `DamageCalculator` / `BattleEngine`. A sync test guarantees every catalog id is covered. Bespoke effects are flags on `AbilityEffect` handled at their hook point.

**Tech Stack:** Unity 6 (`6000.4.3f1`), C# (.NET via Unity), Unity Test Framework (NUnit, EditMode). Pure-C# cores (`Battle`, `Map`) with injected `IRng` for determinism. Tests run via MCP `run_tests` (assembly `Battle.Tests` / `Map.Tests`).

## Global Constraints

- Pure cores stay engine-free: `Map` references nothing; `Battle` references only `Map`. **`AbilityEffect`/`AbilityInfo`/`AbilityCatalog` go in `Map` core** (no UnityEngine). Behavior (`AbilityApplier`) goes in `Battle` core.
- Determinism: all rolling/random uses the injected `IRng` (`MonsterCatcher.Battle.IRng` is in Battle; `RunState` already seeds via `int seed`). Catalog rolling in `Map` uses a `System.Random(seed)`-free approach — see Task 1 (use the passed seed + index hashing, no `Math.Random`).
- Normally 1 ability per monster; the data model is a `List<string>` to allow more later.
- Existing EditMode suite (currently **90 green**) must stay green after every task.
- Ability ids are stable `PascalCase` strings (e.g., `"Brawler"`). Display names/descriptions are the German/English text from the spec (`docs/superpowers/specs/2026-06-29-monster-view-and-abilities-design.md`, §5) — copy verbatim from there when filling the catalog.
- Commit after each task. Branch: work on `main` is allowed in this repo (solo). End commit messages with the `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>` trailer.

---

### Task 1: Ability data model + catalog skeleton + roll (Map core)

**Files:**
- Create: `Assets/Scripts/Map/Core/Ability.cs`
- Create: `Assets/Scripts/Map/Core/AbilityCatalog.cs`
- Modify: `Assets/Scripts/Map/Core/RunState.cs` (add `MonsterSave.AbilityIds`, `RollAbilityId`, roll in `NewRun`)
- Test: `Assets/Scripts/Map/Tests/AbilityCatalogTests.cs`

**Interfaces:**
- Produces: `enum AbilityCategory { Buff, Defining }`; `sealed class AbilityEffect` (data struct, all fields default to no-op — fields added incrementally by later tasks, start with the ones below); `sealed class AbilityInfo { string Id; string Name; string Description; AbilityCategory Category; AbilityEffect Effect; }`; `static class AbilityCatalog { IReadOnlyList<AbilityInfo> All; AbilityInfo ById(string id); string RollId(int seed); }`; `MonsterSave.AbilityIds` (`List<string>`).

- [ ] **Step 1: Write the failing test**

```csharp
// Assets/Scripts/Map/Tests/AbilityCatalogTests.cs
using System.Collections.Generic;
using NUnit.Framework;

namespace MonsterCatcher.Map.Tests
{
    public class AbilityCatalogTests
    {
        [Test] public void RollReturnsACatalogId()
        {
            var id = AbilityCatalog.RollId(123);
            Assert.IsNotNull(AbilityCatalog.ById(id), "rolled id must exist in catalog");
        }

        [Test] public void RollIsDeterministicPerSeed()
        {
            Assert.AreEqual(AbilityCatalog.RollId(7), AbilityCatalog.RollId(7));
        }

        [Test] public void NewRunGivesStarterExactlyOneAbility()
        {
            RunState.NewRun(3);
            Assert.AreEqual(1, RunState.PlayerRoster[0].AbilityIds.Count);
            Assert.IsNotNull(AbilityCatalog.ById(RunState.PlayerRoster[0].AbilityIds[0]));
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run via MCP: `run_tests` EditMode, assembly `Map.Tests`.
Expected: FAIL — `AbilityCatalog`/`AbilityIds` do not exist (compile error).

- [ ] **Step 3: Create the data model**

```csharp
// Assets/Scripts/Map/Core/Ability.cs
namespace MonsterCatcher.Map
{
    public enum AbilityCategory { Buff, Defining }

    // Data-driven effect palette. Every field is a no-op by default; later tasks add fields.
    // Battle core reads these at hook points. Keep this UnityEngine-free.
    public sealed class AbilityEffect
    {
        // Stat multipliers (1.0 = unchanged). Stat index: 0=Hp,1=Atk,2=Def,3=SpAtk,4=SpDef,5=Speed
        public float[] StatMult = { 1f, 1f, 1f, 1f, 1f, 1f };
        // (further fields appended by later tasks)
    }

    public sealed class AbilityInfo
    {
        public readonly string Id, Name, Description;
        public readonly AbilityCategory Category;
        public readonly AbilityEffect Effect;
        public AbilityInfo(string id, string name, string description,
            AbilityCategory category, AbilityEffect effect)
        { Id = id; Name = name; Description = description; Category = category; Effect = effect; }
    }
}
```

```csharp
// Assets/Scripts/Map/Core/AbilityCatalog.cs
using System.Collections.Generic;

namespace MonsterCatcher.Map
{
    public static class AbilityCatalog
    {
        private static readonly List<AbilityInfo> _all = Build();
        private static readonly Dictionary<string, AbilityInfo> _byId = Index(_all);

        public static IReadOnlyList<AbilityInfo> All => _all;
        public static AbilityInfo ById(string id) =>
            id != null && _byId.TryGetValue(id, out var a) ? a : null;

        // Deterministic pick from the seed (no Math.Random — keeps tests stable).
        public static string RollId(int seed)
        {
            int i = (int)((uint)(seed * 2654435761u) % (uint)_all.Count);
            return _all[i].Id;
        }

        private static Dictionary<string, AbilityInfo> Index(List<AbilityInfo> all)
        {
            var d = new Dictionary<string, AbilityInfo>();
            foreach (var a in all) d[a.Id] = a;
            return d;
        }

        // Helper for terse entries; later tasks extend AbilityEffect via object initializers.
        private static AbilityInfo Buff(string id, string name, string desc, AbilityEffect e) =>
            new AbilityInfo(id, name, desc, AbilityCategory.Buff, e);
        private static AbilityInfo Def(string id, string name, string desc, AbilityEffect e) =>
            new AbilityInfo(id, name, desc, AbilityCategory.Defining, e);

        private static List<AbilityInfo> Build()
        {
            var list = new List<AbilityInfo>();
            // Seed with two entries so the catalog is non-empty; later tasks add the rest.
            list.Add(Buff("Brawler", "Brawler", "+12% Attack",
                new AbilityEffect { StatMult = new[] { 1f, 1.12f, 1f, 1f, 1f, 1f } }));
            list.Add(Buff("Mystic", "Mystic", "+12% Sp.Attack",
                new AbilityEffect { StatMult = new[] { 1f, 1f, 1f, 1.12f, 1f, 1f } }));
            return list;
        }
    }
}
```

- [ ] **Step 4: Wire MonsterSave + NewRun roll**

In `Assets/Scripts/Map/Core/RunState.cs`, add to `MonsterSave`:

```csharp
        public List<string> AbilityIds = new List<string>();
```

In `MonsterSave(string speciesName, int level)` constructor body, leave `AbilityIds` empty (roller fills it). Add a roll helper and call it in `NewRun`:

```csharp
        public static void RollStarterAbility(MonsterSave save, int seed)
        {
            if (save.AbilityIds.Count == 0)
                save.AbilityIds.Add(AbilityCatalog.RollId(seed));
        }
```

In `NewRun`, after building `PlayerRoster`, add:

```csharp
            RollStarterAbility(PlayerRoster[0], seed ^ 0x5f3759df);
```

- [ ] **Step 5: Run tests to verify they pass**

Run via MCP: `run_tests` EditMode, assembly `Map.Tests`.
Expected: PASS (all three new tests + existing Map tests).

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Map/Core/Ability.cs Assets/Scripts/Map/Core/AbilityCatalog.cs \
        Assets/Scripts/Map/Core/RunState.cs Assets/Scripts/Map/Tests/AbilityCatalogTests.cs \
        Assets/Scripts/Map/Core/Ability.cs.meta Assets/Scripts/Map/Core/AbilityCatalog.cs.meta \
        Assets/Scripts/Map/Tests/AbilityCatalogTests.cs.meta
git commit -m "Add ability data model + catalog skeleton + starter roll"
```

---

### Task 2: Pokemon ability state + AbilityApplier skeleton + sync test (Battle core)

**Files:**
- Modify: `Assets/Scripts/Battle/Core/Pokemon.cs` (add `AbilityIds`, `AbilityState`, ctor param, helpers)
- Create: `Assets/Scripts/Battle/Core/AbilityApplier.cs`
- Modify: `Assets/Scripts/Battle/Control/BattleController.cs` (pass roster `AbilityIds` into player `Pokemon`)
- Test: `Assets/Scripts/Battle/Tests/AbilityTests.cs`

**Interfaces:**
- Consumes: `MonsterCatcher.Map.AbilityCatalog`, `AbilityInfo`, `AbilityEffect`.
- Produces: `Pokemon.AbilityIds` (`IReadOnlyList<string>`), `Pokemon.HasAbility(string)`, `Pokemon.AbilityEffects` (`IEnumerable<AbilityEffect>` resolved via catalog), `Pokemon.AbilityState` (mutable per-battle: `bool LastStandUsed; bool PhoenixUsed; bool SecondWindUsed; int TurnsOut; bool FirstHitTaken;`); `static class AbilityApplier` (empty methods filled by later tasks) with `static float StatMult(Pokemon p, Stat stat)`.

- [ ] **Step 1: Write the failing test**

```csharp
// Assets/Scripts/Battle/Tests/AbilityTests.cs
using System.Linq;
using NUnit.Framework;
using MonsterCatcher.Map;

namespace MonsterCatcher.Battle.Tests
{
    public class AbilityTests
    {
        [Test] public void EveryCatalogAbilityHasNonNullEffect()
        {
            foreach (var a in AbilityCatalog.All)
                Assert.IsNotNull(a.Effect, a.Id + " effect");
        }

        [Test] public void PokemonExposesItsAbilityEffects()
        {
            var sp = TestFactory.Species("A", ElementType.Normal, 100, 80, 60, 60, 60, 60);
            var mon = new Pokemon(sp, 50, TestFactory.OneMove(), new[] { "Brawler" });
            Assert.IsTrue(mon.HasAbility("Brawler"));
            Assert.AreEqual(1, mon.AbilityEffects.Count());
        }

        [Test] public void StatMultDefaultsToOne()
        {
            var sp = TestFactory.Species("A", ElementType.Normal, 100, 80, 60, 60, 60, 60);
            var mon = new Pokemon(sp, 50, TestFactory.OneMove(), new string[0]);
            Assert.AreEqual(1f, AbilityApplier.StatMult(mon, Stat.Attack), 1e-6);
        }
    }
}
```

If `TestFactory` lacks `OneMove()` or a `Pokemon` ctor overload taking ability ids, add them in this task (see Steps 3–4).

- [ ] **Step 2: Run to verify it fails**

Run via MCP: `run_tests` EditMode, assembly `Battle.Tests`. Expected: FAIL (compile — missing members).

- [ ] **Step 3: Add ability state to `Pokemon`**

In `Assets/Scripts/Battle/Core/Pokemon.cs`, add fields + a constructor overload that accepts ability ids (keep the existing ctor delegating with empty ids):

```csharp
        public sealed class AbilityRuntimeState
        {
            public bool LastStandUsed, PhoenixUsed, SecondWindUsed, FirstHitTaken;
            public int TurnsOut;
        }

        private readonly System.Collections.Generic.List<string> _abilityIds;
        public System.Collections.Generic.IReadOnlyList<string> AbilityIds => _abilityIds;
        public AbilityRuntimeState AbilityState { get; } = new AbilityRuntimeState();

        public bool HasAbility(string id) => _abilityIds.Contains(id);

        public System.Collections.Generic.IEnumerable<MonsterCatcher.Map.AbilityEffect> AbilityEffects
        {
            get
            {
                foreach (var id in _abilityIds)
                {
                    var info = MonsterCatcher.Map.AbilityCatalog.ById(id);
                    if (info != null) yield return info.Effect;
                }
            }
        }
```

Add the new ctor (mirror the existing one, plus ability ids). In the existing ctor(s), initialize `_abilityIds = new List<string>(abilityIds ?? Enumerable.Empty<string>())` (add `using System.Linq;` if needed).

- [ ] **Step 4: Add `TestFactory.OneMove()` if missing, and the `AbilityApplier` skeleton**

In `Assets/Scripts/Battle/Tests/TestFactory.cs`, if no helper exists, add:

```csharp
        public static System.Collections.Generic.List<MoveData> OneMove() =>
            new System.Collections.Generic.List<MoveData> { Move("Tackle", ElementType.Normal, MoveCategory.Physical, 40) };
```

Create the applier:

```csharp
// Assets/Scripts/Battle/Core/AbilityApplier.cs
namespace MonsterCatcher.Battle
{
    // Reads a Pokemon's AbilityEffects at engine hook points. Filled incrementally per task.
    public static class AbilityApplier
    {
        public static float StatMult(Pokemon p, Stat stat)
        {
            float m = 1f;
            int idx = (int)stat;            // Stat: 0=Hp,1=Attack,2=Defense,3=SpAttack,4=SpDefense,5=Speed
            foreach (var e in p.AbilityEffects) m *= e.StatMult[idx];
            return m;
        }
    }
}
```

- [ ] **Step 5: Apply `StatMult` in `Pokemon.EffectiveStat` and pass roster abilities in `BattleController`**

In `Pokemon.EffectiveStat`, multiply the computed stat by `AbilityApplier.StatMult(this, stat)` (round to int; keep HP handled by MaxHp separately — for now apply to non-HP stats; HP-mult handled in a later task). Wire `BattleController.BuildPlayerFromRoster` to pass `save.AbilityIds` into the new `Pokemon` ctor.

- [ ] **Step 6: Run tests to verify pass**

Run via MCP: `run_tests` EditMode, assemblies `Battle.Tests` and `Map.Tests`.
Expected: PASS, existing suite still green.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Battle/Core/Pokemon.cs Assets/Scripts/Battle/Core/AbilityApplier.cs \
        Assets/Scripts/Battle/Control/BattleController.cs Assets/Scripts/Battle/Tests/AbilityTests.cs \
        Assets/Scripts/Battle/Tests/TestFactory.cs Assets/Scripts/Battle/Core/AbilityApplier.cs.meta
git commit -m "Add Pokemon ability state + AbilityApplier (stat multipliers)"
```

---

### Task 3: Fill stat-multiplier abilities + HP-mult + tests

**Files:**
- Modify: `Assets/Scripts/Map/Core/AbilityCatalog.cs` (add all pure stat-mult abilities)
- Modify: `Assets/Scripts/Battle/Core/Pokemon.cs` (`MaxHp` applies HP mult)
- Test: `Assets/Scripts/Battle/Tests/AbilityTests.cs`

**Interfaces:**
- Consumes: `AbilityApplier.StatMult`, `AbilityEffect.StatMult`.
- Produces: catalog entries (ids) for all stat-mult abilities.

Add these abilities (copy Name/Description verbatim from spec §5; effect = `StatMult` with the listed factor; index 0=Hp,1=Atk,2=Def,3=SpAtk,4=SpDef,5=Speed):

- Brawler(Atk×1.12, already), Mystic(SpAtk×1.12, already), Bulwark(Def×1.15), Warden(SpDef×1.15), Fleetfoot(Spe×1.15), Stalwart(Hp×1.10), Vigor(Atk×1.08,SpAtk×1.08), Turtle(Def×1.10,SpDef×1.10), Powerhouse(Atk×1.20), Archmage(SpAtk×1.20), Fortress(Def×1.25), Aegis(SpDef×1.25), Sprinter(Spe×1.25), Giant(Hp×1.20), Balanced(all×1.08), Twin Strike(Atk×1.10,SpAtk×1.10), Stonewall(Def×1.12,SpDef×1.12), Duelist(Spe×1.15,Atk×1.10). All `AbilityCategory.Buff`.

- [ ] **Step 1: Write the failing test**

```csharp
        [Test] public void BrawlerRaisesEffectiveAttack()
        {
            var sp = TestFactory.Species("A", ElementType.Normal, 100, 100, 60, 60, 60, 60);
            var baseMon = new Pokemon(sp, 50, TestFactory.OneMove(), new string[0]);
            var buffed  = new Pokemon(sp, 50, TestFactory.OneMove(), new[] { "Brawler" });
            Assert.Greater(buffed.EffectiveStat(Stat.Attack), baseMon.EffectiveStat(Stat.Attack));
        }

        [Test] public void GiantRaisesMaxHp()
        {
            var sp = TestFactory.Species("A", ElementType.Normal, 100, 80, 60, 60, 60, 60);
            var baseMon = new Pokemon(sp, 50, TestFactory.OneMove(), new string[0]);
            var hpMon   = new Pokemon(sp, 50, TestFactory.OneMove(), new[] { "Giant" });
            Assert.Greater(hpMon.MaxHp, baseMon.MaxHp);
        }
```

- [ ] **Step 2: Run to verify it fails**

Run: `run_tests` `Battle.Tests`. Expected: FAIL (`Giant` not in catalog / MaxHp unaffected).

- [ ] **Step 3: Implement** — add the catalog entries (Step list above) and in `Pokemon.MaxHp` multiply by `AbilityApplier.StatMult(this, Stat.Hp)` (cast to int, min 1). Ensure `EffectiveStat` does **not** double-apply HP.

- [ ] **Step 4: Run to verify pass.** Run: `run_tests` `Battle.Tests` + `Map.Tests`. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Map/Core/AbilityCatalog.cs Assets/Scripts/Battle/Core/Pokemon.cs \
        Assets/Scripts/Battle/Tests/AbilityTests.cs
git commit -m "Add stat-multiplier abilities (incl. HP) + tests"
```

---

### Task 4: Outgoing-damage hooks + abilities + tests

**Files:** Modify `Map/Core/Ability.cs` (extend `AbilityEffect`), `Map/Core/AbilityCatalog.cs`, `Battle/Core/AbilityApplier.cs`, `Battle/Core/DamageCalculator.cs`; Test `Battle/Tests/AbilityTests.cs`.

**Interfaces:**
- Produces on `AbilityEffect`: `float OutgoingMult=1f; ElementType? BoostMoveType; float BoostMoveTypeMult=1f; MoveCategory? BoostCategory; float BoostCategoryMult=1f; float LowHpDamageMult=1f; float FullHpDamageMult=1f; float FirstTurnDamageMult=1f; float FoeLowHpDamageMult=1f; float FoeLowHpThreshold=0f; float VsStatusedDamageMult=1f; float AfterFoeDamageMult=1f; float SuperEffectiveBonusMult=1f; float PerFaintedAllyMult=0f; float RampPerTurn=0f; float RampMax=0f;`
- Produces on `AbilityApplier`: `static float OutgoingMultiplier(Pokemon attacker, Pokemon defender, MoveData move, double effectiveness, BattleContext ctx)` where `ctx` carries `bool AttackerMovedAfter; int FaintedAllies; int TurnsOut;` (pass the few flags the calc needs; if `DamageCalculator` lacks them, thread a small `AbilityContext` struct — define it in this task and default everything to neutral so existing calls compile).

`ElementType`/`MoveCategory` live in Battle; `AbilityEffect` is in Map. To keep Map engine-free, store these as **int** on `AbilityEffect` (`int BoostMoveTypeOrMinus1 = -1; int BoostCategoryOrMinus1 = -1;`) and compare against `(int)move.Type`/`(int)move.Category` in the applier. Use that pattern for every type/category/stat reference in later tasks too.

Abilities (effect params): Bruiser(OutgoingMult 1.10), Aggressor n/a, Berserk(LowHpDamageMult 1.5), Vanguard(FullHpDamageMult 1.10), Early Bird(FirstTurnDamageMult 1.20), Opportunist(FoeLowHpDamageMult 1.15, threshold .5), Finisher(1.25, threshold .25), Bully(VsStatusedDamageMult 1.30), Counterpunch(AfterFoeDamageMult 1.15), Tyrant(SuperEffectiveBonusMult 1.5), Comeback(PerFaintedAllyMult .15), Momentum(RampPerTurn .10, RampMax .40), Glass Cannon(OutgoingMult 1.30 — incoming part in Task 5), Reversal(OutgoingMult 1.40 — move-last in Task 9), Pyromaniac/Galvanize/Naturalist/Moonblessed/Scrappy/Aquatic/Frostbite/Brawl/Venomous/Nightfall(BoostMoveType=<type>, BoostMoveTypeMult 1.20), Iron Fist(BoostCategory=Physical 1.12), Mystic Surge(BoostCategory=Special 1.12).

- [ ] **Step 1: Write failing tests** (representative — one per mechanism family):

```csharp
        [Test] public void BruiserIncreasesDamageDealt()
        {
            var s = TestFactory.Settings();
            var atk = TestFactory.Species("A", ElementType.Normal, 100, 100, 60, 60, 60, 60);
            var def = TestFactory.Species("D", ElementType.Normal, 200, 60, 80, 60, 80, 50);
            var move = TestFactory.Move("Hit", ElementType.Normal, MoveCategory.Physical, 60);
            var plainA = new Pokemon(atk, 50, new() { move }, new string[0]);
            var buffA  = new Pokemon(atk, 50, new() { move }, new[] { "Bruiser" });
            var d1 = DamageCalculator.Calculate(plainA, new Pokemon(def,50,new(){move},new string[0]), move, s, new FakeRng());
            var d2 = DamageCalculator.Calculate(buffA,  new Pokemon(def,50,new(){move},new string[0]), move, s, new FakeRng());
            Assert.Greater(d2.Damage, d1.Damage);
        }

        [Test] public void PyromaniacBoostsOnlyFireMoves()
        {
            var s = TestFactory.Settings();
            var atk = TestFactory.Species("A", ElementType.Fire, 100, 80, 60, 90, 60, 60);
            var def = TestFactory.Species("D", ElementType.Normal, 300, 60, 80, 60, 80, 50);
            var fire = TestFactory.Move("Ember", ElementType.Fire, MoveCategory.Special, 60);
            var norm = TestFactory.Move("Pound", ElementType.Normal, MoveCategory.Physical, 60);
            var mon = new Pokemon(atk, 50, new() { fire, norm }, new[] { "Pyromaniac" });
            var plain = new Pokemon(atk, 50, new() { fire, norm }, new string[0]);
            var df1 = DamageCalculator.Calculate(mon, new Pokemon(def,50,new(){fire},new string[0]), fire, s, new FakeRng()).Damage;
            var df0 = DamageCalculator.Calculate(plain, new Pokemon(def,50,new(){fire},new string[0]), fire, s, new FakeRng()).Damage;
            var dn1 = DamageCalculator.Calculate(mon, new Pokemon(def,50,new(){norm},new string[0]), norm, s, new FakeRng()).Damage;
            var dn0 = DamageCalculator.Calculate(plain, new Pokemon(def,50,new(){norm},new string[0]), norm, s, new FakeRng()).Damage;
            Assert.Greater(df1, df0);           // fire boosted
            Assert.AreEqual(dn0, dn1);          // normal unchanged
        }
```

- [ ] **Step 2: Run to verify fail.** Expected FAIL (missing ability + multiplier not applied).

- [ ] **Step 3: Implement.** Extend `AbilityEffect` (fields above, int-coded types). Add `AbilityApplier.OutgoingMultiplier(...)` computing the product of: flat `OutgoingMult`; type/category boosts when `move` matches; `LowHpDamageMult` when `attacker.CurrentHp*3 < attacker.MaxHp`; `FullHpDamageMult` when full; `FirstTurnDamageMult` when `attacker.AbilityState.TurnsOut==0`; `FoeLowHp` when `defender.CurrentHp <= defender.MaxHp*FoeLowHpThreshold`; `VsStatused` when `defender.Status != None`; `AfterFoe`/`PerFaintedAlly`/`Ramp` from context; `SuperEffectiveBonusMult` when `effectiveness>1`. In `DamageCalculator.Calculate`, multiply final damage by this (add an `AbilityContext` param with a neutral default so existing tests compile; thread real context from `BattleEngine.ExecuteMove`). Catalog: add the abilities above (verbatim names/descs from spec §5).

- [ ] **Step 4: Run to verify pass.** `Battle.Tests` + `Map.Tests`. Expected PASS.

- [ ] **Step 5: Commit** — `git commit -m "Add outgoing-damage ability hooks + abilities"`.

---

### Task 5: Incoming-damage hooks + abilities + tests

**Files:** as Task 4 plus `DamageCalculator.cs`.

**Interfaces:** add `AbilityEffect`: `float IncomingMult=1f; int ResistTypeOrMinus1=-1; float ResistTypeMult=1f; int ResistCategoryOrMinus1=-1; float ResistCategoryMult=1f; float SuperEffTakenMult=1f; float FullHpTakenMult=1f; float FirstHitTakenMult=1f; int EarlyTurnsWindow=0; float EarlyTurnsTakenMult=1f;`. Add `AbilityApplier.IncomingMultiplier(Pokemon defender, Pokemon attacker, MoveData move, double effectiveness)`.

Abilities: Thickhide(IncomingMult .88), Resolute(.90), Glass Cannon(IncomingMult 1.15), Heatproof(ResistType=Fire .5), Cozy(ResistType=Fire .75 + burn-immune in Task 7), Waterproof/Grounded/Frostward/Shade(ResistType=<Water/Electric/Ice/Dark> .5), Plated(ResistCategory=Physical .85), Veiled(ResistCategory=Special .85), Anvil(ResistCategory=Physical .70), Cushion(ResistCategory=Special .70), Scales(IncomingMult .90 — applies to both; model as flat .90), Shellguard(SuperEffTakenMult .75), Multiscale(FullHpTakenMult .5), Toughness(FullHpTakenMult .90), Featherfall(FirstHitTakenMult .80), Fortified(EarlyTurnsWindow 2, EarlyTurnsTakenMult .60).

- [ ] **Step 1: Failing test**

```csharp
        [Test] public void HeatproofHalvesFireDamageOnly()
        {
            var s = TestFactory.Settings();
            var atk = TestFactory.Species("A", ElementType.Fire, 100, 80, 60, 90, 60, 60);
            var defS = TestFactory.Species("D", ElementType.Normal, 300, 60, 80, 60, 80, 50);
            var fire = TestFactory.Move("Ember", ElementType.Fire, MoveCategory.Special, 60);
            var a = new Pokemon(atk, 50, new(){fire}, new string[0]);
            var plainD = new Pokemon(defS, 50, new(){fire}, new string[0]);
            var heatD  = new Pokemon(defS, 50, new(){fire}, new[]{ "Heatproof" });
            Assert.Less(DamageCalculator.Calculate(a, heatD, fire, s, new FakeRng()).Damage,
                        DamageCalculator.Calculate(a, plainD, fire, s, new FakeRng()).Damage);
        }

        [Test] public void MultiscaleHalvesDamageAtFullHp()
        {
            var s = TestFactory.Settings();
            var atk = TestFactory.Species("A", ElementType.Normal, 100, 120, 60, 60, 60, 60);
            var defS = TestFactory.Species("D", ElementType.Normal, 300, 60, 80, 60, 80, 50);
            var move = TestFactory.Move("Hit", ElementType.Normal, MoveCategory.Physical, 80);
            var a = new Pokemon(atk, 50, new(){move}, new string[0]);
            var full = new Pokemon(defS, 50, new(){move}, new[]{ "Multiscale" });   // starts full HP
            var plain = new Pokemon(defS, 50, new(){move}, new string[0]);
            Assert.Less(DamageCalculator.Calculate(a, full, move, s, new FakeRng()).Damage,
                        DamageCalculator.Calculate(a, plain, move, s, new FakeRng()).Damage);
        }
```

- [ ] **Step 2: Run → FAIL.**
- [ ] **Step 3: Implement** the fields + `IncomingMultiplier` (product of matching reductions; `FullHpTakenMult` when `defender.CurrentHp==defender.MaxHp`; `FirstHitTakenMult` when `!defender.AbilityState.FirstHitTaken`; `EarlyTurns` when `defender.AbilityState.TurnsOut < EarlyTurnsWindow`; `SuperEffTakenMult` when `effectiveness>1`). Multiply damage by it in `DamageCalculator`. Set `FirstHitTaken=true` after the first damaging hit (in `BattleEngine.ExecuteMove`). Add catalog entries.
- [ ] **Step 4: Run → PASS.**
- [ ] **Step 5: Commit** `"Add incoming-damage ability hooks + abilities"`.

---

### Task 6: Crit + accuracy/never-miss + abilities + tests

**Files:** `Ability.cs`, `AbilityCatalog.cs`, `AbilityApplier.cs`, `DamageCalculator.cs`; test.

**Interfaces:** add `AbilityEffect`: `float CritChanceMult=1f; bool LowHpAlwaysCrit; float CritDamageMult=1f; bool CritImmune; float AccuracyMult=1f; bool NeverMiss;`. `AbilityApplier`: `CritChance(attacker, baseChance)`, `bool CritImmune(defender)`, `float CritDamageMult(attacker)`, `float Accuracy(attacker, baseAcc)`, `bool NeverMisses(attacker)`.

Abilities: Keen(CritChanceMult 3), Lucky Strike(2), Sniper(CritDamageMult → crit ×2.25 i.e. multiply the crit bonus so 1.5→2.25 ⇒ CritDamageMult 1.5), Unbreakable(CritImmune), Avenger(LowHpAlwaysCrit), Hawk Eye(AccuracyMult 1.20), Focused Aim(1.15), Nimble(AccuracyMult 1.10 + Spe×1.10 via StatMult), Deadeye(NeverMiss).

- [ ] **Step 1: Failing tests**

```csharp
        [Test] public void DeadeyeAlwaysHits()
        {
            var s = TestFactory.Settings();
            var atkS = TestFactory.Species("A", ElementType.Normal, 100, 80, 60, 60, 60, 60);
            var defS = TestFactory.Species("D", ElementType.Normal, 200, 60, 80, 60, 80, 50);
            var lowAcc = TestFactory.Move("Wild", ElementType.Normal, MoveCategory.Physical, 60, accuracy: 1); // 1% acc
            var a = new Pokemon(atkS, 50, new(){lowAcc}, new[]{ "Deadeye" });
            var d = new Pokemon(defS, 50, new(){lowAcc}, new string[0]);
            for (int i = 0; i < 20; i++)
                Assert.IsTrue(DamageCalculator.Calculate(a, d, lowAcc, s, new DefaultRng(i)).Hit);
        }

        [Test] public void UnbreakablePreventsCrits()
        {
            var s = TestFactory.Settings();
            var atkS = TestFactory.Species("A", ElementType.Normal, 100, 80, 60, 60, 60, 60);
            var defS = TestFactory.Species("D", ElementType.Normal, 200, 60, 80, 60, 80, 50);
            var hc = TestFactory.Move("Sharp", ElementType.Normal, MoveCategory.Physical, 50); hc.HighCrit = true;
            var d = new Pokemon(defS, 50, new(){hc}, new[]{ "Unbreakable" });
            var a = new Pokemon(atkS, 50, new(){hc}, new string[0]);
            for (int i = 0; i < 300; i++)
                Assert.IsFalse(DamageCalculator.Calculate(a, d, hc, s, new DefaultRng(i)).WasCritical);
        }
```

- [ ] **Step 2: Run → FAIL.**
- [ ] **Step 3: Implement.** In `DamageCalculator`: crit chance = `AbilityApplier.CritChance(attacker, baseChance)`; force `false` if `AbilityApplier.CritImmune(defender)`; force `true` if attacker has `LowHpAlwaysCrit` and is below ¼ HP. Crit multiplier (currently 1.5) ×= `CritDamageMult(attacker)`. Hit roll: if `NeverMisses(attacker)` skip the accuracy roll (Hit=true); else effective accuracy = `Accuracy(attacker, move.Accuracy)`. Add catalog entries.
- [ ] **Step 4: Run → PASS.**
- [ ] **Step 5: Commit** `"Add crit/accuracy ability hooks + abilities"`.

---

### Task 7: Status immunity / chip / stat-drop / on-hit-inflict + abilities + tests

**Files:** `Ability.cs`, `AbilityCatalog.cs`, `AbilityApplier.cs`, `BattleEngine.cs`; test.

**Interfaces:** add `AbilityEffect`: `int ImmuneStatusOrMinus1=-1; bool ImmuneAllStatus; bool BurnNoChip; bool PoisonNoChip; bool ParalysisNoSpeedCut; bool ImmuneStatDrops; int OnHitInflictOrMinus1=-1; int OnHitInflictChance=0; bool OnHitInflictTargetsAttacker;`. `AbilityApplier`: `bool ImmuneToStatus(Pokemon p, StatusCondition s)`, `bool ImmuneToStatDrops(Pokemon p)`, `bool BurnChipBlocked/PoisonChipBlocked(Pokemon p)`, `bool ParalysisSpeedImmune(Pokemon p)`, plus an on-hit hook applied in `ExecuteMove`.

Abilities: Limber(Paralysis), Stoic(Burn), Wide Awake(Sleep), Antibody(Poison), Guardian n/a (use `ImmuneAllStatus` only if a "Guardian" exists — it doesn't; skip), Bloom(PoisonImmune + heal in Task 8), Cozy(Burn immune + Fire resist from Task 5), Fever Ward(BurnNoChip), Shake It Off(ParalysisNoSpeedCut), Hardy Mind(ImmuneStatDrops), Venomtouch(OnHitInflict=Poison 30), Static Body(OnHitInflict=Paralysis 30, TargetsAttacker).

- [ ] **Step 1: Failing test**

```csharp
        [Test] public void LimberBlocksParalysis()
        {
            var sp = TestFactory.Species("D", ElementType.Normal, 100, 60, 60, 60, 60, 60);
            var mon = new Pokemon(sp, 50, TestFactory.OneMove(), new[]{ "Limber" });
            Assert.IsFalse(mon.TryApplyStatus(StatusCondition.Paralysis, 0));
            Assert.AreEqual(StatusCondition.None, mon.Status);
        }
```

(If `TryApplyStatus` is the entry point, gate it with `AbilityApplier.ImmuneToStatus`. Otherwise gate in `BattleEngine.ApplySecondaryEffects` / status moves.)

- [ ] **Step 2: Run → FAIL.**
- [ ] **Step 3: Implement.** Gate `Pokemon.TryApplyStatus` (or the engine status application) with immunity checks; gate end-of-turn chip with `BurnChipBlocked`/`PoisonChipBlocked`; gate `EffectiveSpeed` paralysis cut with `ParalysisSpeedImmune`; gate foe stat-lowering (`ApplySecondaryEffects` when recipient is the foe and `ImmuneToStatDrops`). After a damaging hit in `ExecuteMove`, roll `OnHitInflict` against `_rng` and apply to defender (or attacker if `TargetsAttacker`). Add catalog entries.
- [ ] **Step 4: Run → PASS.**
- [ ] **Step 5: Commit** `"Add status-immunity + on-hit ability hooks + abilities"`.

---

### Task 8: Sustain (heal/turn, drain-all, recoil, one-time heal) + abilities + tests

**Files:** `Ability.cs`, `AbilityCatalog.cs`, `AbilityApplier.cs`, `BattleEngine.cs`; test.

**Interfaces:** add `AbilityEffect`: `float HealPerTurnFraction=0f; float DrainAllFraction=0f; bool RecoilImmune; float RecoilMoveBonusMult=1f; float OneTimeHealBelowHalf=0f;`. `AbilityApplier`: `int EndOfTurnHeal(Pokemon p)`, `int DrainAmount(Pokemon attacker, int damageDealt)`, `bool RecoilImmune(Pokemon p)`, `float RecoilBonus(Pokemon p)`, and a check for the one-time heal trigger.

Abilities: Regrowth(Heal .0625), Mending(Heal .04), Bloom(Heal .04 + poison-immune already), Siphon(DrainAll .15), Vampiric(DrainAll .08), Reckless(RecoilImmune + RecoilMoveBonusMult 1.20), Second Wind(OneTimeHealBelowHalf .10).

- [ ] **Step 1: Failing test**

```csharp
        [Test] public void RegrowthHealsEachTurn()
        {
            var s = TestFactory.Settings();
            var sp = TestFactory.Species("U", ElementType.Normal, 200, 60, 60, 60, 60, 40);
            var foeSp = TestFactory.Species("F", ElementType.Normal, 200, 60, 300, 60, 300, 5);
            var noop = TestFactory.Move("NoOp", ElementType.Normal, MoveCategory.Status, 0, accuracy: 0);
            var user = new Pokemon(sp, 50, new(){noop}, new[]{ "Regrowth" });
            var foe  = new Pokemon(foeSp, 50, new(){noop}, new string[0]);
            user.SetCurrentHp(10);
            var engine = new BattleEngine(new Party(BattleSide.Player, new(){user}, 6),
                                          new Party(BattleSide.Enemy, new(){foe}, 6), s, new FakeRng());
            engine.ExecuteTurn(BattleAction.UseMove(0), BattleAction.UseMove(0));
            Assert.Greater(user.CurrentHp, 10);
        }
```

- [ ] **Step 2: Run → FAIL.**
- [ ] **Step 3: Implement.** In `BattleEngine.EndOfTurnStatus` (or a sibling end-of-turn pass), heal `EndOfTurnHeal`; trigger one-time heal when first dropping below ½. In `ApplyRecoilAndDrain`: skip recoil if `RecoilImmune`; add drain from `DrainAllFraction` on any damaging move; apply `RecoilBonus` to recoil-move damage (fold into Task 4 outgoing if simpler). Add catalog entries + emit `DrainEvent`/`RecoilEvent` as today.
- [ ] **Step 4: Run → PASS.**
- [ ] **Step 5: Commit** `"Add sustain ability hooks + abilities"`.

---

### Task 9: Tempo (priority, always-last, guaranteed-first, low-HP speed) + abilities + tests

**Files:** `Ability.cs`, `AbilityCatalog.cs`, `AbilityApplier.cs`, `BattleEngine.cs`; test.

**Interfaces:** add `AbilityEffect`: `bool StatusMovePriority; bool FirstTurnPriority; bool GuaranteedFirstTurn1; bool AlwaysMoveLast; float LowHpSpeedMult=1f;`. `AbilityApplier`: `int PriorityBonus(Pokemon p, MoveData move)`, `bool ForcesFirst(Pokemon p)`, `bool ForcesLast(Pokemon p)`, `float SpeedMult(Pokemon p)` (folds Adrenaline + Fleetfoot-style? no — Fleetfoot is StatMult; keep `LowHpSpeedMult` here applied in `EffectiveSpeed`).

Abilities: Trickster(StatusMovePriority), Phantom Step(FirstTurnPriority), Time Warp(GuaranteedFirstTurn1), Reversal(AlwaysMoveLast — damage already in Task 4), Adrenaline(LowHpSpeedMult 1.5).

- [ ] **Step 1: Failing test**

```csharp
        [Test] public void TricksterGivesStatusMovesPriority()
        {
            var s = TestFactory.Settings();
            var slowSp = TestFactory.Species("S", ElementType.Normal, 100, 60, 60, 60, 60, 5);   // very slow
            var fastSp = TestFactory.Species("F", ElementType.Normal, 100, 60, 60, 60, 60, 200);
            var growl = TestFactory.Move("Growl", ElementType.Normal, MoveCategory.Status, 0, accuracy: 0);
            growl.StatToChange = Stat.Attack; growl.StatStageDelta = -1; growl.StatChangeChance = 100;
            var slow = new Pokemon(slowSp, 50, new(){growl}, new[]{ "Trickster" });
            var fast = new Pokemon(fastSp, 50, new(){growl}, new string[0]);
            var engine = new BattleEngine(new Party(BattleSide.Player, new(){slow}, 6),
                                          new Party(BattleSide.Enemy, new(){fast}, 6), s, new FakeRng());
            var ev = engine.ExecuteTurn(BattleAction.UseMove(0), BattleAction.UseMove(0));
            // Slow Trickster user should act first: the FIRST StatChangedEvent targets the fast foe.
            var firstStat = ev.OfType<StatChangedEvent>().First();
            Assert.AreSame(fast, firstStat.Target);
        }
```

- [ ] **Step 2: Run → FAIL.**
- [ ] **Step 3: Implement.** In `MovePriority`/`CompareOrder`: add `AbilityApplier.PriorityBonus` (status-move +1 when `StatusMovePriority` and move category Status; first-turn +1 when `FirstTurnPriority` and `TurnsOut==0`); `ForcesFirst` outranks all (turn-1 only); `ForcesLast` sinks below all. In `EffectiveSpeed`: multiply by `SpeedMult` (Adrenaline when below ⅓). Add catalog entries.
- [ ] **Step 4: Run → PASS.**
- [ ] **Step 5: Commit** `"Add tempo ability hooks + abilities"`.

---

### Task 10: On-entry (stat stages + Download) + abilities + tests

**Files:** `Ability.cs`, `AbilityCatalog.cs`, `AbilityApplier.cs`, `BattleEngine.cs`; test.

**Interfaces:** add `AbilityEffect`: `int EntrySelfStatOrMinus1=-1; int EntrySelfStages=0; int EntryFoeStatOrMinus1=-1; int EntryFoeStages=0; bool EntryDownloadHigherOffense;`. `AbilityApplier`: `void ApplyOnEntry(Pokemon entering, Pokemon opponent, List<BattleEvent> events)`.

Abilities: Intimidate(EntryFoeStat=Attack −1), Battle Cry(EntrySelfStat=Attack +1), Overclock(EntrySelfStat=SpAttack +1), Warm Up(Speed +1), Guard Up(Defense +1), Brace(SpDefense +1), Download(EntryDownloadHigherOffense).

- [ ] **Step 1: Failing test**

```csharp
        [Test] public void IntimidateLowersFoeAttackOnEntry()
        {
            var s = TestFactory.Settings();
            var sp = TestFactory.Species("A", ElementType.Normal, 100, 80, 60, 60, 60, 60);
            var me  = new Pokemon(sp, 50, TestFactory.OneMove(), new[]{ "Intimidate" });
            var foe = new Pokemon(sp, 50, TestFactory.OneMove(), new string[0]);
            new BattleEngine(new Party(BattleSide.Player, new(){me}, 6),
                             new Party(BattleSide.Enemy, new(){foe}, 6), s, new FakeRng());
            Assert.AreEqual(-1, foe.GetStage(Stat.Attack));
        }
```

- [ ] **Step 2: Run → FAIL.**
- [ ] **Step 3: Implement.** Call `AbilityApplier.ApplyOnEntry(active, opponent, events)` for both actives in the `BattleEngine` ctor, and for the switched-in mon in `ApplyOrQueue` (switch) and `ResolveForcedSwitch`. Download: raise Attack or SpAttack (whichever is higher) by 1. Emit `StatChangedEvent`. Add catalog entries.
- [ ] **Step 4: Run → PASS.**
- [ ] **Step 5: Commit** `"Add on-entry ability hooks + abilities"`.

---

### Task 11: Reactive (on-KO, on-hit-self, thorns, aftermath, survive-lethal, phoenix, comeback) + abilities + tests

**Files:** `Ability.cs`, `AbilityCatalog.cs`, `AbilityApplier.cs`, `BattleEngine.cs`; test.

**Interfaces:** add `AbilityEffect`: `int OnKoSelfStatOrMinus1=-1; int OnKoSelfStages=0; int OnHitTakenSelfStatOrMinus1=-1; int OnHitTakenSelfStages=0; float ThornsFraction=0f; float AftermathFraction=0f; bool ReviveOnce; float ReviveFraction=0f; bool SurviveLethalOnce;`. `AbilityApplier`: `bool TrySurviveLethal(Pokemon p)` (returns true & sets HP=1, marks used), `bool TryRevive(Pokemon p)` (sets HP, marks used), `void OnDealtDamage(attacker, defender, events)` (thorns + on-hit-self), `void OnKo(victor, events)`, `void OnFaint(fainter, opponent, events)`.

Abilities: Moxie(OnKoSelfStat=Attack +1), Steadfast(OnHitTakenSelfStat=Speed +1), Rage(OnHitTakenSelfStat=Attack +1), Thorns(ThornsFraction .125), Aftermath(AftermathFraction .25), Last Stand(SurviveLethalOnce), Phoenix(ReviveOnce, ReviveFraction .33), Second Wind already (Task 8), Comeback already (Task 4 needs FaintedAllies from context).

- [ ] **Step 1: Failing tests**

```csharp
        [Test] public void LastStandSurvivesOneLethalHit()
        {
            var s = TestFactory.Settings();
            var atkS = TestFactory.Species("A", ElementType.Normal, 100, 200, 60, 60, 60, 120);
            var defS = TestFactory.Species("D", ElementType.Normal, 40, 60, 10, 60, 10, 5);
            var move = TestFactory.Move("Hit", ElementType.Normal, MoveCategory.Physical, 120);
            var a = new Pokemon(atkS, 50, new(){move}, new string[0]);
            var d = new Pokemon(defS, 50, new(){move}, new[]{ "Last Stand" });
            new BattleEngine(new Party(BattleSide.Player, new(){a}, 6),
                             new Party(BattleSide.Enemy, new(){d}, 6), s, new FakeRng())
                .ExecuteTurn(BattleAction.UseMove(0), BattleAction.UseMove(0));
            Assert.IsFalse(d.IsFainted);
            Assert.AreEqual(1, d.CurrentHp);
        }
```

- [ ] **Step 2: Run → FAIL.**
- [ ] **Step 3: Implement.** In `Pokemon.TakeDamage` (or right before applying lethal damage in `ExecuteMove`): if damage would faint and `SurviveLethalOnce` unused, clamp to 1 HP and mark used. After a damaging hit lands in `ExecuteMove`, call `OnDealtDamage` (thorns → attacker `TakeDamage(MaxHp*frac)`; on-hit-self stat boost on the defender). When a target faints, before/after `FaintedEvent`: `OnKo(attacker)` (Moxie), and `OnFaint(fainter, opponent)` (Aftermath damages opponent; Phoenix revives fainter to `MaxHp*ReviveFraction` and cancels the faint if unused). Thread `FaintedAllies` count into the damage context (Comeback) from each `Party`. Add catalog entries.
- [ ] **Step 4: Run → PASS.**
- [ ] **Step 5: Commit** `"Add reactive ability hooks + abilities"`.

---

### Task 12: Offensive-special (all-STAB, tinted-lens, executioner, suppress-foe-boosts) + abilities + tests

**Files:** `Ability.cs`, `AbilityCatalog.cs`, `AbilityApplier.cs`, `DamageCalculator.cs`, `BattleEngine.cs`; test.

**Interfaces:** add `AbilityEffect`: `bool AllMovesStab; bool TintedLens; float ExecuteThreshold=0f; bool SuppressFoeBoosts;`. Apply in `DamageCalculator`: `AllMovesStab` forces the STAB factor even when the move type isn't the user's; `TintedLens` raises a <1 effectiveness toward 1 (`eff = Math.Max(eff, 1.0)` only for the damage step, keep messaging unchanged or note "not very effective" still). In `ExecuteMove`: `ExecuteThreshold` — if defender below threshold after computing it would survive, set lethal. `SuppressFoeBoosts` — when this mon attacks/defends, treat the opponent's positive stat stages as 0 (read via a flag passed to `EffectiveStat` usage in `DamageCalculator`).

Abilities: Adaptability(AllMovesStab), Tinted Lens(TintedLens), Executioner(ExecuteThreshold .20), Disruptor(SuppressFoeBoosts).

- [ ] **Step 1: Failing test**

```csharp
        [Test] public void ExecutionerKosLowHpFoe()
        {
            var s = TestFactory.Settings();
            var atkS = TestFactory.Species("A", ElementType.Normal, 100, 60, 60, 60, 60, 120);
            var defS = TestFactory.Species("D", ElementType.Normal, 300, 60, 200, 60, 200, 5); // very bulky
            var weak = TestFactory.Move("Tap", ElementType.Normal, MoveCategory.Physical, 10);
            var a = new Pokemon(atkS, 50, new(){weak}, new[]{ "Executioner" });
            var d = new Pokemon(defS, 50, new(){weak}, new string[0]);
            d.SetCurrentHp((int)(d.MaxHp * 0.15));   // below 20%
            new BattleEngine(new Party(BattleSide.Player, new(){a}, 6),
                             new Party(BattleSide.Enemy, new(){d}, 6), s, new FakeRng())
                .ExecuteTurn(BattleAction.UseMove(0), BattleAction.UseMove(0));
            Assert.IsTrue(d.IsFainted);
        }
```

- [ ] **Step 2: Run → FAIL.**
- [ ] **Step 3: Implement** the four flags at the points above. Add catalog entries.
- [ ] **Step 4: Run → PASS.**
- [ ] **Step 5: Commit** `"Add offensive-special ability hooks + abilities"`.

---

### Task 13: Enemy roll + full catalog completeness + integration

**Files:** Modify `Assets/Scripts/Battle/Control/BattleController.cs` (roll an ability for the enemy); Modify `AbilityCatalog.cs` (ensure ALL 110 present); Test `Assets/Scripts/Battle/Tests/AbilityTests.cs`, `Assets/Scripts/Map/Tests/AbilityCatalogTests.cs`.

**Interfaces:** Consumes everything above.

- [ ] **Step 1: Write the failing completeness + enemy tests**

```csharp
// Map.Tests
        [Test] public void CatalogHas110With70Buffs40Defining()
        {
            Assert.AreEqual(110, AbilityCatalog.All.Count);
            Assert.AreEqual(70, AbilityCatalog.All.Count(a => a.Category == AbilityCategory.Buff));
            Assert.AreEqual(40, AbilityCatalog.All.Count(a => a.Category == AbilityCategory.Defining));
            // ids unique
            Assert.AreEqual(110, AbilityCatalog.All.Select(a => a.Id).Distinct().Count());
        }
```

```csharp
// Battle.Tests
        [Test] public void EnemyGetsExactlyOneAbility()
        {
            MonsterCatcher.Map.RunState.NewRun(3);
            // pick a battle node so PendingEnemySpecies resolves
            var f1 = new System.Collections.Generic.List<MonsterCatcher.Map.MapNode>(
                MonsterCatcher.Map.RunState.Map.NodesInRow(1));
            MonsterCatcher.Map.RunState.PendingNodeId = f1[0].Id;
            var c = new BattleController();
            c.StartRunBattle(new FakeRng());                 // or the actual entry the scene uses
            Assert.AreEqual(1, c.Engine.Enemy.Active.AbilityIds.Count);
        }
```

(Adapt the enemy test to `BattleController`'s real API — if it builds the enemy internally, assert on the built enemy's `AbilityIds` after start. Use the existing controller entry point used by `BattleHud`.)

- [ ] **Step 2: Run → FAIL** (count ≠ 110 until all entries added; enemy has 0 abilities).

- [ ] **Step 3: Implement.** Verify every ability from spec §5 (ids listed across Tasks 3–12) is in the catalog — add any missing as data entries. In `BattleController.BuildEnemy`, after constructing the enemy `Pokemon`, roll one ability id (`AbilityCatalog.RollId` seeded from `RunState.PendingNodeId ^ RunState.Tier` or the battle `IRng`) and assign it (use the enemy `Pokemon` ctor with ability ids, or a setter). Keep the roster→player wiring from Task 2.

- [ ] **Step 4: Run → PASS** (Battle.Tests + Map.Tests; full suite green).

- [ ] **Step 5: Commit** `"Complete 110-ability catalog + enemy ability roll"`.

---

## Self-Review

- **Spec coverage:** §2 architecture → Tasks 1–2. §3 hooks + §4 palette → Tasks 3–12 (each hook group). §5 110 abilities → distributed Tasks 3–12, completeness gated in Task 13. §6 roll/persistence/enemy → Tasks 1 (starter), 13 (enemy); persistence is the `MonsterSave.AbilityIds` list (survives tiers/evolution by construction — add a persistence assertion to Task 1 if desired). §10 tests → each task is TDD; sync/counts in Tasks 2/13. **§7 UI, §8 lore → Phase 2 plan (separate).**
- **Placeholders:** none — every step has code or an exact command. The few "adapt to real API" notes (Task 13 enemy entry) are because `BattleController`'s start method name must be read from the file at execution time; the assertion target (`enemy.AbilityIds.Count == 1`) is exact.
- **Type consistency:** `AbilityEffect` fields are additive across tasks (each task lists the fields it adds); `AbilityApplier` method names are referenced consistently; type/category/stat stored as int on `AbilityEffect` (Map stays engine-free) and compared via `(int)` casts in Battle.

## Execution Handoff

Phase 1 only. Phase 2 (monster-view UI + lore + release) will get its own plan after Phase 1 is green.
