# Grass Starter Line Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the 3-stage Grass line (Mossprig → Briarstag → Elderthorn) as SpeciesData assets with stats, sprites and evolution links, show monster sprites in battle, and use Mossprig as the demo starter.

**Architecture:** Extend the existing `SpeciesData` ScriptableObject with sprite + evolution fields; author 3 `.asset` files under `Assets/Resources/Species/` loaded by `SampleData` via `Resources.Load`; add two sprite `Image`s to the runtime `BattleHud`. Movesets stay placeholder until wave 2.

**Tech Stack:** Unity 6 (`6000.4.3f1`), C# 9, uGUI, Unity Test Framework (EditMode), ScriptableObjects, `Resources.Load`.

## Global Constraints

- All three species: `Type1 = Grass`, `HasSecondType = false`. Profile: low Attack, high SpAttack + SpDefense, good Speed.
- Stats (KP/Atk/Def/SpA/SpD/Spe): Mossprig 50/40/55/65/70/60; Briarstag 70/50/70/90/95/80; Elderthorn 90/60/85/120/125/95.
- Evolution: Mossprig→Briarstag at Lv.16; Briarstag→Elderthorn at Lv.34; Elderthorn is final (EvolveLevel 0, EvolvesInto null).
- Sprite files in `Assets/Sprites/`: fronts `Mosssprig.png` / `Briarstag.png` / `Elderthorn.png`; backs `Mossprig Back.png` / `Briarstag Back.png` / `Elderthorn Back.png`. Each is "Multiple" with one sub-sprite `<filebase>_0`.
- Real movesets are wave 2; demo placeholder moves only. `SpeciesData.LearnableMoves` stays empty.
- Tests via MCP `run_tests(mode="EditMode", assembly_names=["Battle.Tests"])`; `read_console(types=["error"])` after edits. Git deferred.

---

### Task 1: Extend SpeciesData

**Files:**
- Modify: `Assets/Scripts/Battle/Data/SpeciesData.cs`

**Interfaces:**
- Produces: `SpeciesData` gains `public Sprite FrontSprite; public Sprite BackSprite; public SpeciesData EvolvesInto; public int EvolveLevel;`.

- [ ] **Step 1: Add the fields** (after the base-stat block, before `LearnableMoves`):
```csharp
        [Header("Sprites")]
        public Sprite FrontSprite;
        public Sprite BackSprite;

        [Header("Evolution")]
        public SpeciesData EvolvesInto;
        [Min(0)] public int EvolveLevel;
```

- [ ] **Step 2: Compile.** `refresh_unity(force, all, request)`, then `read_console(types=["error"])` empty. (Existing 49 tests unaffected — fields are additive.)

- [ ] **Step 3: Commit** (if git): `git add Assets/Scripts/Battle/Data/SpeciesData.cs && git commit -m "feat: add sprite + evolution fields to SpeciesData"`

---

### Task 2: Create the 3 species assets (MCP)

**Files:**
- Create: `Assets/Resources/Species/Elderthorn.asset`, `Briarstag.asset`, `Mossprig.asset`

**Interfaces:**
- Produces: three `SpeciesData` assets loadable via `Resources.Load<SpeciesData>("Species/<Name>")`.

- [ ] **Step 1: Resolve sprite GUIDs + sub-sprite names.** Read the `.meta` of each sprite file (`Read` on `Assets/Sprites/<file>.png.meta`); record `guid` and the sub-sprite name from `nameFileIdTable` (e.g. front `Mosssprig_0`, back `Mossprig Back_0`).

- [ ] **Step 2: Create assets (Elderthorn first — evolution references point upward).** Via MCP `manage_scriptable_object` create each `SpeciesData` asset and set fields. Field values:
  - **Elderthorn** (`Assets/Resources/Species/Elderthorn.asset`): DisplayName "Elderthorn", Type1 Grass, HasSecondType false, BaseHp 90, BaseAttack 60, BaseDefense 85, BaseSpAttack 120, BaseSpDefense 125, BaseSpeed 95, EvolveLevel 0, EvolvesInto null, FrontSprite `{guid: Elderthorn.png, spriteName: Elderthorn_0}`, BackSprite `{guid: "Elderthorn Back.png", spriteName: "Elderthorn Back_0"}`.
  - **Briarstag**: DisplayName "Briarstag", Grass, 70/50/70/90/95/80, EvolveLevel 34, EvolvesInto → Elderthorn.asset, FrontSprite Briarstag_0, BackSprite "Briarstag Back_0".
  - **Mossprig**: DisplayName "Mossprig", Grass, 50/40/55/65/70/60, EvolveLevel 16, EvolvesInto → Briarstag.asset, FrontSprite Mosssprig_0, BackSprite "Mossprig Back_0".
  (Object references via instance id or `{guid, spriteName}` per `manage_components` value rules. After creating, `refresh_unity`.)

- [ ] **Step 3: Verify.** `read_console(types=["error"])` empty; assets exist under `Assets/Resources/Species/`.

- [ ] **Step 4: Commit** (if git): `git add Assets/Resources/Species && git commit -m "feat: add Mossprig/Briarstag/Elderthorn species assets"`

---

### Task 3: Show sprites in battle

**Files:**
- Modify: `Assets/Scripts/Battle/View/BattleHud.cs`

**Interfaces:**
- Consumes: `Pokemon.Species.FrontSprite`/`BackSprite`.
- Produces: enemy front sprite + player back sprite rendered and updated on switch.

- [ ] **Step 1: Add fields** (next to `_enemyHpFill`):
```csharp
        private Image _enemySprite, _playerSprite;
```

- [ ] **Step 2: Create the sprite images in `BuildUi()`** (after the player info panel block, before the message box):
```csharp
            _enemySprite = MakePanel(canvasRt, Color.white);
            SetAnchors(_enemySprite.rectTransform, 0.56f, 0.66f, 0.80f, 0.92f);
            _enemySprite.preserveAspect = true;
            _enemySprite.raycastTarget = false;
            _enemySprite.enabled = false;

            _playerSprite = MakePanel(canvasRt, Color.white);
            SetAnchors(_playerSprite.rectTransform, 0.12f, 0.34f, 0.36f, 0.62f);
            _playerSprite.preserveAspect = true;
            _playerSprite.raycastTarget = false;
            _playerSprite.enabled = false;
```

- [ ] **Step 3: Assign sprites in `UpdatePanel(BattleSide)`.** In the Player branch (after setting the HP bar) add:
```csharp
                if (_playerSprite != null)
                {
                    _playerSprite.sprite = p.Species.BackSprite;
                    _playerSprite.enabled = p.Species.BackSprite != null;
                }
```
In the Enemy branch add:
```csharp
                if (_enemySprite != null)
                {
                    _enemySprite.sprite = e.Species.FrontSprite;
                    _enemySprite.enabled = e.Species.FrontSprite != null;
                }
```

- [ ] **Step 4: Compile.** `refresh_unity`; `read_console(types=["error"])` empty.

- [ ] **Step 5: Commit** (if git): `git add Assets/Scripts/Battle/View/BattleHud.cs && git commit -m "feat: render monster sprites in battle"`

---

### Task 4: Demo team from assets

**Files:**
- Modify: `Assets/Scripts/Battle/Fixtures/SampleData.cs`

**Interfaces:**
- Consumes: `Resources.Load<SpeciesData>`.
- Produces: player party Mossprig (+Briarstag), enemy Elderthorn, all with placeholder Grass moves.

- [ ] **Step 1: Add a loader + rewrite the party builders.** Replace `CreatePlayerParty` and `CreateEnemyParty` with:
```csharp
        private static SpeciesData LoadSpecies(string name)
        {
            var s = Resources.Load<SpeciesData>("Species/" + name);
            return s != null ? s : Species(name, ElementType.Grass, 60, 50, 60, 70, 70, 70);
        }

        public static Party CreatePlayerParty(BattleSettings settings)
        {
            var vine = Move("Vine Whip", ElementType.Grass, MoveCategory.Physical, 45);
            var drain = Move("Mega Drain", ElementType.Grass, MoveCategory.Special, 40);
            var p1 = new Pokemon(LoadSpecies("Mossprig"), 50, new List<MoveData> { vine, drain });
            var p2 = new Pokemon(LoadSpecies("Briarstag"), 50, new List<MoveData> { vine, drain });
            return new Party(BattleSide.Player, new List<Pokemon> { p1, p2 }, settings.MaxPartySize);
        }

        public static Party CreateEnemyParty(BattleSettings settings)
        {
            var vine = Move("Vine Whip", ElementType.Grass, MoveCategory.Physical, 45);
            var drain = Move("Mega Drain", ElementType.Grass, MoveCategory.Special, 40);
            var e1 = new Pokemon(LoadSpecies("Elderthorn"), 50, new List<MoveData> { vine, drain });
            return new Party(BattleSide.Enemy, new List<Pokemon> { e1 }, settings.MaxPartySize);
        }
```
(Keep the existing `CreateSettings`, `Move`, `Species` helpers; `using UnityEngine;` is already present for `Resources`.)

- [ ] **Step 2: Compile + battle tests unaffected.** `refresh_unity`; `read_console(types=["error"])` empty; `run_tests(mode="EditMode", assembly_names=["Battle.Tests"])` → still passes (SampleData isn't unit-tested).

- [ ] **Step 3: Commit** (if git): `git add Assets/Scripts/Battle/Fixtures/SampleData.cs && git commit -m "feat: demo team uses Grass starter assets"`

---

### Task 5: Species asset tests

**Files:**
- Create: `Assets/Scripts/Battle/Tests/SpeciesAssetTests.cs`

**Interfaces:**
- Consumes: the 3 assets (Task 2), `SpeciesData` fields (Task 1).

- [ ] **Step 1: Write the tests**
```csharp
using NUnit.Framework;
using UnityEngine;

namespace MonsterCatcher.Battle.Tests
{
    public class SpeciesAssetTests
    {
        private static SpeciesData Load(string n) => Resources.Load<SpeciesData>("Species/" + n);

        [Test] public void AllThreeLoad()
        {
            Assert.IsNotNull(Load("Mossprig"));
            Assert.IsNotNull(Load("Briarstag"));
            Assert.IsNotNull(Load("Elderthorn"));
        }

        [Test] public void AllAreGrass()
        {
            Assert.AreEqual(ElementType.Grass, Load("Mossprig").Type1);
            Assert.AreEqual(ElementType.Grass, Load("Briarstag").Type1);
            Assert.AreEqual(ElementType.Grass, Load("Elderthorn").Type1);
        }

        [Test] public void EvolutionChainLinks()
        {
            Assert.AreSame(Load("Briarstag"), Load("Mossprig").EvolvesInto);
            Assert.AreSame(Load("Elderthorn"), Load("Briarstag").EvolvesInto);
            Assert.IsNull(Load("Elderthorn").EvolvesInto);
        }

        [Test] public void EvolveLevels()
        {
            Assert.AreEqual(16, Load("Mossprig").EvolveLevel);
            Assert.AreEqual(34, Load("Briarstag").EvolveLevel);
            Assert.AreEqual(0, Load("Elderthorn").EvolveLevel);
        }

        [Test] public void SpritesAssigned()
        {
            foreach (var n in new[] { "Mossprig", "Briarstag", "Elderthorn" })
            {
                var s = Load(n);
                Assert.IsNotNull(s.FrontSprite, n + " front");
                Assert.IsNotNull(s.BackSprite, n + " back");
            }
        }

        [Test] public void StatSpotCheck()
        {
            Assert.AreEqual(120, Load("Elderthorn").BaseSpAttack);
            Assert.AreEqual(40, Load("Mossprig").BaseAttack);
        }
    }
}
```

- [ ] **Step 2: Run → PASS.** `run_tests(mode="EditMode", assembly_names=["Battle.Tests"])`; `read_console(types=["error"])` empty.

- [ ] **Step 3: Commit** (if git): `git add Assets/Scripts/Battle/Tests/SpeciesAssetTests.cs && git commit -m "test: verify Grass starter assets"`

---

### Task 6: Verify in Editor

- [ ] **Step 1: Play the battle scene + screenshot.** `manage_scene(action="load", path="Assets/Scenes/Battle.unity")`; `manage_editor(action="play")`; `read_console(types=["error"])` empty; `manage_camera(action="screenshot", include_image=true, max_resolution=720)` → expect Mossprig (back sprite, lower-left) vs Elderthorn (front sprite, upper-right), names "Mossprig (You)" / "Elderthorn (Foe)". `manage_editor(action="stop")`.

- [ ] **Step 2: Adjust sprite anchors if needed** (overlap with panels) and re-verify.

---

## Self-Review

**Spec coverage:** SpeciesData fields §3 → Task 1; 3 assets §4 → Task 2; sprite display §5 → Task 3; demo team + placeholder moves §6 → Task 4; tests §7 → Task 5; files §8 → all; verify → Task 6. ✔

**Placeholder scan:** the only "placeholder" is the intentional demo moves (spec §6/§2 non-goal) and the editor-verify step (inherently visual). No vague steps. ✔

**Type consistency:** `FrontSprite`/`BackSprite`/`EvolvesInto`/`EvolveLevel` defined in Task 1 used identically in Tasks 2/3/5; `Resources.Load<SpeciesData>("Species/<Name>")` path consistent across Tasks 2/4/5; `Move`/`Species`/`Pokemon`/`Party` signatures match existing SampleData. ✔
