# Inventory, Items, Catching & Shop — Design

**Datum:** 2026-06-29
**Projekt:** MonsterCatcher (Unity 6 `6000.4.3f1`)
**Status:** Freigegeben, Spec
**Verwandt:** `2026-06-29-monster-view-and-abilities-design.md`, leveling/heal specs

---

## 1. Ziel

Items + Inventar, Monster fangen, und eine Shop-Node mit Gold. In **3 Phasen**.

## 2. Daten (Map core, pure C#)

- **`RunState`** bekommt: `Dictionary<string,int> Inventory` und `int Gold`. `NewRun` setzt Start: `MonsterCatcher×3, Potion×2, Remedy×1`, Gold 0. `NextTier` **behält** Inventar + Gold. Helfer: `AddItem(id,n)`, `RemoveItem(id,n)`, `ItemCount(id)`, `AddGold(n)`, `TrySpendGold(n)`.
- **`ItemCatalog`** (Map core): `ItemInfo { Id, Name, Description, int Price, ItemTarget }` (Target: ActiveMonster / FaintedAlly / WildEnemy / Self). Start-Set:
  | Id | Name | Effekt | Preis |
  |---|---|---|---|
  | MonsterCatcher | Monster Catcher | fängt ein wildes Nicht-Boss-Monster | 30 |
  | Potion | Potion | heilt aktivem Monster 50 % max KP | 20 |
  | Remedy | Remedy | heilt den Status des aktiven Monsters | 15 |
  | Revive | Revive | belebt ein K.O.-Teammitglied mit 50 % KP | 40 |
  | XAttack | X-Attack | Angriff +1 Stufe (für den Kampf) | 18 |
- **Team-Limit:** `const int MaxRoster = 6`. Fangen nur wenn `PlayerRoster.Count < MaxRoster`.

## 3. Kampf-Item-Nutzung (Battle core)

- **`BattleAction.Pass()`** + `ActionKind.Pass`: in `BattleEngine.ApplyOrQueue` wird Pass **übersprungen** (Anwender handelt nicht); der Gegner-Zug + End-of-Turn laufen normal. So „kostet" Item-Nutzung den Zug.
- **`BattleController.UseItem(string id)`**: prüft Vorrat; wendet Effekt an (Phase 1: Potion/Remedy/Revive/X-Attack); verbraucht 1; führt einen Gegner-Zug via `ExecuteTurn(Pass, AI)` aus; `TurnResolved`. Reject-Fälle (z. B. Revive ohne K.O.-Mitglied, Catcher bei vollem Team/Boss) → Meldung ohne Zugverbrauch.
- **Events fürs HUD:** `ItemUsedEvent(string message)`, `HealedEvent(Pokemon,int)` (HUD-`ApplyVisual` erhöht die HP-Leiste + `Describe`-Text). `CaughtEvent`/`BrokeFreeEvent` (Phase 2).
- **HUD:** das „Items"-Menü listet Inventar (Name + Anzahl), Klick → `UseItem`. Items mit 0 Anzahl ausgegraut. Catcher nur in Nicht-Boss-Kämpfen aktiv.

## 4. Gold (Phase 1)

`BattleController.ApplyRunResultIfOver` bei Sieg: `RunState.AddGold(5 + 3 * enemyLevel)` (enemyLevel = aktives Gegner-Level). Eine `GoldEvent`/Meldung optional.

## 5. Fangen (Phase 2)

- **`CatchCalculator.Chance(Pokemon enemy)`** (Battle core, pure): `0.30 + 0.50*(1 - hp/maxHp) + (enemy.Status!=None ? 0.20 : 0)`, geklemmt **[0.10, 0.95]**. Unit-getestet (volle KP ~0.30; schwach+Status ~0.95; monoton fallend mit KP).
- **Flow** (`UseItem("MonsterCatcher")`): wenn Boss-Kampf → reject („Can't catch a boss!"); wenn Team voll → reject („Your team is full!"); sonst 1 Catcher verbrauchen, `rng.Roll(Chance)`:
  - **Erfolg:** `RunState.PlayerRoster.Add(new MonsterSave(enemy.Species.name, enemy.Level){AbilityIds=enemy.AbilityIds, CurrentHp=enemy.CurrentHp})`; `CaughtEvent`; Kampf endet als **Sieg** (Run-Ergebnis als gewonnen anwenden, Continue → Map).
  - **Fehlschlag:** `BrokeFreeEvent`; Gegner handelt (Pass-Zug).
- „Wild" = jeder Nicht-Boss-Run-Kampf. Boss-Erkennung: `RunState.Map.Get(RunState.PendingNodeId).Type == NodeType.Boss` (zur Kampfzeit) oder ein Flag beim Bauen.

## 6. Shop-Node (Phase 3)

- **`NodeType.Shop`**; `MapGenerator` macht Etagen-Knoten ab Etage 2 mit ~10 % zu Shop (analog Heal; Heal/Shop schließen sich aus). Farbe/Label im `MapController` (z. B. „$").
- **`ShopView`** (Runtime-uGUI in `Map.View`, wie `MonsterView`): Gold-Anzeige; Liste der `ItemCatalog`-Items mit Preis + Kauf-Button (aktiv wenn Gold ≥ Preis); Kauf → `TrySpendGold` + `AddItem`. „Leave"-Button. Klick auf Shop-Node (im `MapController`) öffnet das Overlay statt Kampf (wie Heal: `VisitShop` markiert Node geklärt, kein Szenenwechsel).
- **`RunState.VisitShop(id)`** analog `VisitHeal` (Node klären + Current setzen, kein Heal).

## 7. Tests

Phase 1: Inventar-Helfer (Add/Remove/Count, kein Unterlauf), Gold (Add/Spend, kein Negativ), `NewRun` Start-Inventar, `NextTier` behält Inventar+Gold, `ItemCatalog` vollständig. Phase 2: `CatchCalculator.Chance` (Grenzwerte + Monotonie), Roster-Add bei Erfolg, Team-voll/Boss-Reject. Phase 3: `VisitShop`, Kauf-Logik (Gold-Grenzen), MapGenerator Shop-Vorkommen.

## 8. Dateien (geplant)

```
Map core:   RunState.cs (Inventory/Gold + helpers, MaxRoster), Item.cs (ItemInfo/ItemCatalog/ItemTarget),
            NodeType.cs (Shop), MapGenerator.cs (shop placement), RunState.VisitShop
Battle:     BattleAction.cs (Pass), BattleEngine.cs (Pass skip), BattleController.cs (UseItem + catch + gold),
            BattleEvent.cs (ItemUsedEvent/HealedEvent/CaughtEvent/BrokeFreeEvent), CatchCalculator.cs
Battle.View: BattleHud.cs (Items menu wiring + HealedEvent/Item/Caught messages)
Map.View:   ShopView.cs, MapController.cs (shop node dispatch + button/color)
Tests:      Map/Tests/InventoryTests.cs, ShopTests.cs; Battle/Tests/CatchTests.cs, ItemUseTests.cs
```

## 9. Entscheidungen

- Fang-Chance skaliert mit KP + Status (geklemmt 10–95 %); nur Nicht-Boss; Team-Limit 6.
- Gold-Ökonomie: pro Sieg verdient, im Shop ausgegeben.
- Item-Nutzung kostet den Zug (außer erfolgreicher Fang beendet den Kampf).
- Item-Identität in Map core (Daten), Effekte im `BattleController` (Verhalten) — analog zum Fähigkeiten-Split.
- 3 Phasen mit Checkpoint nach jeder.
