# First Moves & Learnsets — Design

**Datum:** 2026-06-29
**Projekt:** MonsterCatcher (Unity 6 `6000.4.3f1`)
**Status:** Freigegeben (Design), Spec zur Umsetzung
**Verwandt:** `2026-06-29-pokemon-battle-system-design.md`, `2026-06-29-grass-starter-line-design.md`, `2026-06-29-leveling-and-enemy-scaling-design.md`

---

## 1. Ziel

Die ersten echten Attacken (Welle 2) plus ein **Lernset pro Level**: Monster lernen Attacken abhängig vom Level. (Item/Event-Attacken + „Vergessen"-Abfrage sind explizit später.)

## 2. Die 6 Attacken (`MoveData`-Assets in `Assets/Resources/Moves/`)

| Move | Type | Category | Power | Accuracy | PP | Effekt |
|---|---|---|---|---|---|---|
| Tackle | Normal | Physical | 40 | 100 | 35 | — |
| Scratch | Normal | Physical | 55 | 85 | 25 | mehr Schaden, weniger Genauigkeit |
| Growl | Normal | Status | 0 | 0 (trifft immer) | 30 | +1 eigener **Attack** (TargetsSelf, 100%) |
| Glare | Normal | Status | 0 | 100 | 30 | −1 gegnerischer **Attack** (100%) |
| Plant Whip | Grass | Special | 50 | 100 | 25 | nutzt Sp.-Angriff |
| Charge | Grass | Special | 120 | 100 | 5 | **2-Runden-Aufladeattacke** (`ChargesUp`) |

Growl/Glare laufen über die vorhandene Stat-Stufen-Mechanik (`StatToChange=Attack`, `StatStageDelta=±1`, `StatChangeTargetsSelf`, `StatChangeChance=100`) — kein Engine-Code nötig.

## 3. Charge-Mechanik (Engine-Erweiterung)

- **`MoveData.ChargesUp`** (bool, Standard false).
- **`Pokemon.ChargingMoveIndex`** (int, −1 = lädt nicht).
- **`BattleEngine.ExecuteTurn`**: am Anfang die Aktion einer ladenden Seite **erzwingen** → `UseMove(ChargingMoveIndex)` (Wahl/Wechsel werden ignoriert, „locked in").
- **`BattleEngine.ExecuteMove`**:
  - Wenn `move.ChargesUp` und `ChargingMoveIndex < 0`: Aufladen starten → `ChargingMoveIndex` setzen, AP abziehen, `MoveUsedEvent` + neues **`ChargingEvent`** („X lädt auf!"), **kein Schaden**, return.
  - Sonst (Freigabe-Runde, `ChargingMoveIndex ≥ 0`): `ChargingMoveIndex = -1`, **kein** weiteres AP, dann normaler (starker Sp.-)Schaden.
  - Normale Moves: unverändert.
- Während des Aufladens ist das Monster normal angreifbar.
- `ChargingEvent` wird in `BattleHud.Describe` als „X is charging up!" gerendert.

## 4. Lernset (abgeleitet aus Level)

- **`SpeciesData.LevelUpLearnset`**: `List<LearnsetEntry { int Level; MoveData Move; }>`, **aufsteigend nach Level** gepflegt.
- **`SpeciesData.MovesAtLevel(int level)`**: liefert die Moves der Einträge mit `Level ≤ level`, in Listenreihenfolge; bei mehr als 4 die **letzten 4** (jüngste). Keine Sortierung nötig (Liste ist aufsteigend gepflegt) → deterministisch.
- Kampf baut Movesets über `MovesAtLevel` (Spieler-Kader **und** Gegner). Fallback: wenn leer → `SampleData.PlaceholderGrassMoves()`.
- Keine pro-Monster-Move-Speicherung nötig (abgeleitet). Item/Event-Moves + „gelernt!"-Abfrage = später.

## 5. Lernsets der Pflanzen-Linie

- **Mossprig:** Tackle (1), Plant Whip (1), Growl (4), Glare (7)
- **Briarstag:** Tackle (1), Plant Whip (1), Scratch (4), Charge (8)
- **Elderthorn:** Tackle (1), Plant Whip (1), Glare (5), Charge (8)

Jede Liste hat ≤ 4 Einträge → `MovesAtLevel` verwirft nie etwas, gibt einfach die freigeschalteten zurück.

## 6. Battle-Anbindung

`BattleController.BuildPlayerFromRoster` und `BuildEnemy` (aus dem Leveling-Wave) nutzen `species.MovesAtLevel(level)` statt `PlaceholderGrassMoves()` (mit dem Fallback). Standalone-`SampleData` bleibt für Direktstart, kann aber ebenfalls auf echte Moves umgestellt werden (optional, hier: bleibt Platzhalter für Standalone).

## 7. Tests (EditMode, Battle.Tests)

- **ChargeTakesTwoTurns:** Runde 1 mit Charge → `ChargingEvent`, **kein** Schaden am Ziel; Runde 2 (Wahl egal) → Ziel nimmt Schaden.
- **MovesAtLevel:** Lernset mit >4 Einträgen (distinkte Level) → `MovesAtLevel` gibt die jüngsten 4, der älteste fehlt.
- **GrowlRaisesAttack:** Growl (über Engine) erhöht die Angriffs-Stufe des Anwenders (kurzer Integrationstest).

## 8. Dateien

```
Modified: Assets/Scripts/Battle/Data/MoveData.cs         (+ ChargesUp)
Modified: Assets/Scripts/Battle/Data/SpeciesData.cs      (+ LearnsetEntry, LevelUpLearnset, MovesAtLevel)
Modified: Assets/Scripts/Battle/Core/Pokemon.cs          (+ ChargingMoveIndex)
Modified: Assets/Scripts/Battle/Core/BattleEngine.cs     (Charge: Override + ExecuteMove)
Modified: Assets/Scripts/Battle/Core/BattleEvent.cs      (+ ChargingEvent)
Modified: Assets/Scripts/Battle/View/BattleHud.cs        (ChargingEvent-Text)
Modified: Assets/Scripts/Battle/Control/BattleController.cs (MovesAtLevel statt Platzhalter)
Created:  Assets/Resources/Moves/{Tackle,Scratch,Growl,Glare,PlantWhip,Charge}.asset
Created:  Assets/Scripts/Battle/Tests/MovesTests.cs
```
Lernsets der 3 Species werden per MCP gesetzt (`LevelUpLearnset` mit Move-Referenzen).

## 9. Entscheidungen

- Growl/Glare → physischer **Attack** (±1). Charge → Pflanze, Sp., 120, 2 Runden, verwundbar beim Aufladen.
- Lernset-Regel: 4 jüngste Level-Moves, kein Vergessen-Dialog (v1). Item/Event-Moves später.
- UX-Wart (akzeptiert): in der Charge-Freigaberunde ignoriert die Engine die Spielerwahl; ein „muss aufladen"-Hinweis im UI kommt später.
