# Event Rooms + Bigger Scrollable Map — Design

Date: 2026-06-30

Two related additions to the run map:

1. **Event rooms** — a new node type that offers a choice of 3 random beneficial-or-risky events.
2. **Bigger scrollable map** — 20 floors (was 8), spaced further apart and staggered, inside a scrollable viewport.

Both build on the existing `Map` core (pure) / `Map.View` (runtime uGUI) split, mirroring the Heal/Shop node pattern.

---

## Part A — Event Rooms

### Node & placement

- New `NodeType.Event`.
- `MapGenerator`: per floor (from floor 2 up), with `EventChance = 0.30`, **exactly one** node on that floor is made an `Event` (max 1 per floor). The chosen node is not also Heal/Shop. Implementation: roll an `eventIndex` for the floor before the per-node Heal/Shop rolls; the node at `eventIndex` becomes `Event`, the rest roll Heal/Shop/Battle as today.
- Visiting an event clears the node without a battle, exactly like Heal/Shop: `RunState.VisitEvent(id)` (clear + advance, no scene change).
- Rendered violet with a `?` label.

### Flow

On clicking an event node, `MapController` opens an `EventView` overlay (runtime uGUI, like `ShopView`):

1. **Choosing** — show **3 random *applicable* events** (deterministic per node id). Each as a button: name + description.
2. **Targeting** (only if the chosen event needs a monster) — show the roster; pick one. For evolve events only evolvable monsters are enabled.
3. **Result** — apply the effect, show a short result line (especially for gambles), then a `Continue` button that calls `RunState.VisitEvent(nodeId)` + closes + refreshes the map.

If fewer than 3 events are applicable, show what is available.

### Event pool (24)

`*` = needs a chosen monster target. Conditions gate whether the event is *offered*.

**Pure boons (12)**

| Id | Name | Effect | Target | Condition |
|---|---|---|---|---|
| MindExpansion | Mind Expansion | +1 max team size | — | — |
| AncientAwakening | Ancient Awakening | +1 random passive ability | * | — |
| TrainingGrounds | Training Grounds | +4 levels | * | — |
| WarDrums | War Drums | whole team +2 levels | — | — |
| SacredSpring | Sacred Spring | full heal + revive team | — | — |
| HiddenCache | Hidden Cache | +75 gold | — | — |
| SupplyDrop | Supply Drop | +2 Potion, +1 MonsterCatcher, +1 Revive | — | — |
| EvolutionCatalyst | Evolution Catalyst | evolve now | * | a monster can evolve now |
| LuckyVein | Lucky Vein | +130 gold | — | — |
| MentorsGift | Mentor's Gift | +2 levels **and** +1 random ability | * | — |
| TwinDrills | Twin Drills | two random team members +3 levels | — | — |
| Quartermaster | Quartermaster | +4 random items | — | — |

**Trade-offs — good *and* bad at once (10)**

| Id | Name | Good | Bad | Target | Condition |
|---|---|---|---|---|---|
| BloodPact | Blood Pact | team +3 levels | all to 50% HP | — | — |
| CursedRiches | Cursed Riches | +150 gold | a random monster −2 levels | — | — |
| ForbiddenTome | Forbidden Tome | +2 random abilities | −3 levels | * | — |
| SacrificialRite | Sacrificial Rite | rest of team +5 levels | release the chosen monster | * | team > 1 |
| DevilsBargain | Devil's Bargain | +1 team size **and** +100 gold | whole team −1 level | — | — |
| RecklessEvolution | Reckless Evolution | evolve now even if not ready | −2 levels | * | a monster has an evolution |
| GlassCannonBrew | Glass Cannon Brew | +6 levels | drops to 1 HP | * | — |
| SoulTax | Soul Tax | +1 random ability | whole team −1 level | * | — |
| PawnEverything | Pawn Everything | +100 gold | lose a whole random item type | — | has items |
| PhoenixRite | Phoenix Rite | full heal + revive team | −80 gold | — | — |

**Gambles — good *or* bad, RNG (2)**

| Id | Name | Outcome | Target |
|---|---|---|---|
| GamblersDice | Gambler's Dice | 60%: +200 gold · 40%: −60 gold | — |
| MysteryBox | Mystery Box | 50%: +2 random abilities · 50%: −4 levels | * |

### Architecture

**`EventCatalog` (Map core, pure):** `EventInfo { string Id, Name, Description; bool NeedsMonsterTarget; EventCondition Condition; }`, list `All`, `ById`, and `RandomOffer(int seed, int count, IReadOnlyList<string> applicableIds)` → deterministic distinct selection from the applicable set. `EventCondition` enum: `None, TeamAboveOne, HasItems, HasEvolvableNow, HasAnyEvolution`.

**`RunState` effect helpers (pure, tested):**
- `MaxRoster` becomes a mutable `static int` (was `const`), reset to 6 in `NewRun`, kept by `NextTier`. `ExpandRoster(int n = 1)`.
- `AddLevels(int index, int amount)` — `Level` clamped to ≥ 1.
- `AddLevelsAll(int amount)` — each clamped ≥ 1.
- `GrantRandomAbility(int index, int seed)` — adds an `AbilityCatalog` id not already on the monster (no-op if it already has all).
- `LoseRandomItemType(int seed)` — removes one random owned item id entirely; returns whether anything was lost.
- `SpendGoldClamped(int n)` — `Gold = max(0, Gold - n)`.
- existing `AddGold`, `AddItem`, `HealParty`, `ReleaseMonster`.
- `VisitEvent(int id)` — clear + advance like `VisitShop`.
- Pure condition checks: `TeamAboveOne()` (`PlayerRoster.Count > 1`), `HasAnyItem()` (`Inventory.Count > 0`).

**`EventView` (Map.View) orchestrates:** computes the applicable id list (pure conditions via `RunState`, species conditions via `Resources.Load<SpeciesData>` + `CanEvolveAt`/`EvolvesInto`), asks `EventCatalog.RandomOffer` for 3, renders the 3 stages, and applies effects. HP-fraction effects (Blood Pact, Glass Cannon Brew) and evolve effects build a temporary `Pokemon` to read `MaxHp` / read species, then write back to the `MonsterSave`. Everything else calls the `RunState` helpers.

### Side-effect fix

Because `MaxRoster` can now exceed 6, `BattleController.BuildPlayerFromRoster` must build the player `Party` with `Math.Max(settings.MaxPartySize, mons.Count)` (the enemy build already does this), or a 7-monster roster throws in the `Party` constructor.

### Tests (`Map.Tests/EventTests`)

- `NewRun` resets `MaxRoster` to 6; `ExpandRoster` increments and persists across `NextTier`.
- `AddLevels` / `AddLevelsAll` raise and lower, clamped at 1.
- `GrantRandomAbility` takes a monster from 1 → 2 abilities with no duplicate.
- `LoseRandomItemType` removes exactly one item type; `SpendGoldClamped` floors at 0.
- `VisitEvent` clears + advances the node.
- `EventCatalog` has all 24 ids with the right `NeedsMonsterTarget` / `Condition` flags; `RandomOffer` returns the requested distinct count from the applicable set, deterministic per seed.

UI (3-stage choose/target/result, auto-applicability) verified in play mode.

---

## Part B — Bigger Scrollable Map

### Floors

- `MapGenerator.Floors = 20` (was 8) → Start (row 0) + 20 floors + Boss (row 21) = 22 rows.

### Leveling stays coherent

With 20 floors the old per-tier span (`BossLevel = 10`) would let late floors out-level the boss and make levels non-monotonic across tiers. Fix:

- `RunState.BossLevel` becomes `static readonly int BossLevel = MapGenerator.Floors + 2` (= 22) — the per-tier level span and the boss level. Now tier 1 floors are Lv.1–20, the boss is Lv.22, and tier 2 floor 1 is Lv.23 (monotonic).
- Extract `RunState.EnemyLevelFor(NodeType type, int row, int tier)` (`(tier-1)*BossLevel + (boss ? BossLevel : row)`); `PendingEnemyLevel` calls it. Lets the leveling be unit-tested.
- `StageForRow` bands derive from `Floors`: stage 0 for `row ≤ Floors/3` (1–6), stage 1 for `≤ 2*Floors/3` (7–13), else stage 2 (14–20). Boss is always stage 2.

### Scrollable, spaced, staggered layout (`MapController`)

- Replace the fixed centered `_container` with a `ScrollRect`: a root with `ScrollRect`, a `Viewport` child (`Image` + `RectMask2D`, fills the area under the title), and a `Content` child sized to the full map. A vertical `Scrollbar` on the right. `vertical = true`, `horizontal = false`, `movementType = Clamped`. Scroll via mouse wheel + drag (the `InputSystemUIInputModule` already present drives it).
- `Content` size: width ≈ 1100, height = `(RowCount-1) * RowGap + 2*VMargin` with `RowGap ≈ 130`, `VMargin ≈ 80` → ~2890 px tall for 22 rows. Nodes/edges are parented to `Content`.
- `LocalPos(n)` in content-centered coords: `x = lerp(-halfW+HMargin, halfW-HMargin, n.X) + jitterX(n)`, `y = -halfH + VMargin + n.Row*RowGap + jitterY(n)` (row 0 at the bottom, boss at the top). `jitter` is a deterministic hash of the node id (small: ±~38 px x, ±~18 px y) so rows are staggered, not rigid columns. Start/Boss stay centered (n.X = 0.5) with minimal jitter.
- **Auto-scroll**: on build and after each `RefreshNodes`, set `verticalNormalizedPosition ≈ currentRow / (RowCount-1)` so the player's current position is in view.

### Tests (`Map.Tests`)

- Generator: `Floors == 20`, `RowCount == 22`, existing connectivity/reachability invariants still hold; events appear and never more than one per floor across many seeds.
- Leveling: `EnemyLevelFor(Boss,*,1) == 22`; a late floor (`Battle, 20, 1`) is below the boss; `EnemyLevelFor(Battle,1,2) > EnemyLevelFor(Boss,*,1)` (monotonic across tiers).
- `StageForRow` bands: 6 → 0, 7 → 1, 13 → 1, 14 → 2.

Layout/scroll is view-only — verified in play mode with a screenshot.

---

## Build order

Map first (generator floors + leveling rescale + scroll layout), then events. TDD for all core (`Map` / `Battle`) logic; the two overlays verified live.
