# Monster View (Phase 2) Implementation Plan

> **For agentic workers:** Execute task-by-task. Steps use checkbox (`- [ ]`) syntax. Logic tasks are TDD; the UI task is runtime-uGUI construction verified in play mode.

**Goal:** A "Monsters" button on the map opens an overlay showing the player's party with stats, Pokédex lore, moves + move info, and the rolled ability — plus releasing a monster when the party has more than one.

**Architecture:** A runtime-built uGUI overlay (`MonsterView`, same pattern as `BattleHud`/`MapController`) lives in `Map.View`, which gains a reference to `Battle` so it can load `SpeciesData`/`MoveData`, build a `Pokemon` for ability-adjusted stats, and read the `AbilityCatalog`. Release mutates `RunState.PlayerRoster`. Lore is a new `SpeciesData.LoreText`.

**Tech Stack:** Unity 6 `6000.4.3f1`, C#, Unity Test Framework (EditMode). Catalog/roster in Map core; species/moves/stats in Battle core.

## Global Constraints

- `Map.View` may reference `Battle` (`Map.View→Battle→Map`; no cycle — nothing references `Map.View`).
- Stats shown are **ability-adjusted**: build `new Pokemon(species, level, moves, save.AbilityIds)` and read `EffectiveStat`/`MaxHp`.
- Release is allowed only when `RunState.PlayerRoster.Count > 1`.
- Existing 129 EditMode tests stay green. Commit per task. End commit messages with the `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>` trailer.

---

### Task 1: SpeciesData.LoreText + 12 lore texts

**Files:** Modify `Assets/Scripts/Battle/Data/SpeciesData.cs`; modify the 12 `Assets/Resources/Species/*.asset` via MCP `manage_scriptable_object`.

- [ ] **Step 1:** Add to `SpeciesData` after the sprite fields: `[TextArea] public string LoreText;`.
- [ ] **Step 2:** Refresh/compile; `read_console` clean.
- [ ] **Step 3:** For each of the 12 species set `LoreText` (verbatim from spec §8) via `manage_scriptable_object` action `modify`, patch path `LoreText`.
- [ ] **Step 4:** Add a test `Assets/Scripts/Battle/Tests/LoreTests.cs`: every species in `{Mossprig,...,Eclipseon}` has non-empty `LoreText`. Run `Battle.Tests` → PASS.
- [ ] **Step 5:** Commit `"Add SpeciesData.LoreText + 12 Pokedex lore blurbs"`.

### Task 2: RunState.ReleaseMonster + test

**Files:** Modify `Assets/Scripts/Map/Core/RunState.cs`; test `Assets/Scripts/Map/Tests/ReleaseTests.cs`.

- [ ] **Step 1: Failing test**

```csharp
using NUnit.Framework;
namespace MonsterCatcher.Map.Tests
{
    public class ReleaseTests
    {
        [Test] public void ReleaseRemovesWhenMoreThanOne()
        {
            RunState.NewRun(3);
            RunState.PlayerRoster.Add(new MonsterSave("Briarstag", 5));
            int before = RunState.PlayerRoster.Count;     // 2
            Assert.IsTrue(RunState.ReleaseMonster(1));
            Assert.AreEqual(before - 1, RunState.PlayerRoster.Count);
        }

        [Test] public void ReleaseRefusedWhenOnlyOne()
        {
            RunState.NewRun(3);                            // single starter
            Assert.AreEqual(1, RunState.PlayerRoster.Count);
            Assert.IsFalse(RunState.ReleaseMonster(0));
            Assert.AreEqual(1, RunState.PlayerRoster.Count);
        }
    }
}
```

- [ ] **Step 2:** Run `Map.Tests` → FAIL (no `ReleaseMonster`).
- [ ] **Step 3:** Add to `RunState`:

```csharp
        public static bool ReleaseMonster(int index)
        {
            if (PlayerRoster.Count <= 1) return false;
            if (index < 0 || index >= PlayerRoster.Count) return false;
            PlayerRoster.RemoveAt(index);
            return true;
        }
```

- [ ] **Step 4:** Run `Map.Tests` → PASS.
- [ ] **Step 5:** Commit `"Add RunState.ReleaseMonster"`.

### Task 3: Map.View references Battle

**Files:** Modify `Assets/Scripts/Map/View/Map.View.asmdef`.

- [ ] **Step 1:** Add `"Battle"` to its `references` array.
- [ ] **Step 2:** Refresh/compile; `read_console` clean; run full EditMode suite → still 129+ green.
- [ ] **Step 3:** Commit `"Map.View references Battle (for the monster view)"`.

### Task 4: MonsterView overlay + MapController button

**Files:** Create `Assets/Scripts/Map/View/MonsterView.cs`; modify `Assets/Scripts/Map/View/MapController.cs`.

**Interfaces:** `MonsterView` is a `MonoBehaviour` that builds its own `Canvas` overlay on `Awake` (hidden), exposes `public void Toggle()` / `Show()` / `Hide()`. It reads `RunState.PlayerRoster`, `Resources.Load<SpeciesData>("Species/" + name)`, builds a `Pokemon` for stats, `species.MovesAtLevel(level)` for moves, and `MonsterCatcher.Map.AbilityCatalog.ById(id)` for the ability text.

- [ ] **Step 1:** Implement `MonsterView` (runtime uGUI, mirror `BattleHud` construction): full-screen dimmed panel with a Close button; left party-list column (one button per roster entry, label `"<Name>  Lv.<level>"`, click → select index); right detail panel rebuilt on select:
  - Header: `FrontSprite` image, `DisplayName`, type(s) (`Type1`[`/Type2` if `HasSecondType`]), `Lv.<level>`.
  - Lore: `species.LoreText` (wrapped text).
  - Stats: six lines `HP/Atk/Def/SpA/SpD/Spe = EffectiveStat(...)` (HP = `MaxHp`).
  - Moves: for each `MovesAtLevel`, `"<Name> — <Type>/<Category>  Pow <Power> Acc <Accuracy=0?'—':Accuracy>"` plus a short effect note (status/stat/recoil/drain/charge/highcrit) derived from `MoveData` fields.
  - Ability: `"<info.Name> — <info.Description>"` for each id in `save.AbilityIds`.
  - Release button: interactable only when `RunState.PlayerRoster.Count > 1`; on click show an inline `Release <Name>? [Yes] [No]`; Yes → `RunState.ReleaseMonster(index)` then rebuild the list and select index 0.
- [ ] **Step 2:** In `MapController`, add a "Monsters" button (top bar) whose click calls the `MonsterView.Toggle()` (create the `MonsterView` GameObject in `MapController` build, keep a reference).
- [ ] **Step 3:** Refresh/compile; `read_console` clean.
- [ ] **Step 4:** Commit `"Add monster-view overlay (stats, lore, moves, ability, release) + map button"`.

### Task 5: Play-mode verification

- [ ] **Step 1:** Enter play mode on `Map.unity`; screenshot. Click "Monsters"; screenshot the overlay.
- [ ] **Step 2:** `execute_code` to assert the selected monster's ability id resolves in the catalog and stats are > 0; if party can be grown (add a temp 2nd mon via `execute_code`), exercise Release and confirm the roster shrank.
- [ ] **Step 3:** Stop play mode. If issues, fix and re-verify. Then update memory ([[passive-abilities]] Phase 2 done; [[run-map-status]] monster-view) and commit any fixes.

## Self-Review

- Spec §7 (UI: party, stats, lore, moves+info, ability, release) → Task 4. §8 (lore) → Task 1. Release rule → Task 2. Assembly note → Task 3. Verification → Task 5.
- No placeholders: ReleaseMonster + tests are concrete; the UI task lists each panel region and the exact data source.
- Types: `RunState.ReleaseMonster(int)→bool`; `SpeciesData.LoreText` (string); `MonsterView.Toggle()`.
