# Leveling & Enemy Scaling — Design

**Datum:** 2026-06-29
**Projekt:** MonsterCatcher (Unity 6 `6000.4.3f1`)
**Status:** Freigegeben (Design), Spec zur Umsetzung
**Verwandt:** `2026-06-29-run-map-design.md`, `2026-06-29-pokemon-battle-system-design.md`, `2026-06-29-grass-starter-line-design.md`

---

## 1. Ziel

Roguelike-Progression: man startet mit **Level-1-Monstern**, gewinnt pro Sieg Level, und Gegner skalieren mit der Map.

- Spieler-Kader ist **persistenter Run-Zustand** (Level, Level-Fortschritt, aktuelle KP pro Monster), überlebt den Map↔Battle-Wechsel.
- Pro Sieg: **Teilnehmer +1 Level**, **Bankmonster +0.5** (Level alle 2 Siege).
- **KP bleiben über den Run** (kein Auto-Heilen; besiegt bleibt besiegt bis spätere Rast-Knoten).
- **Gegner-Level = Etagennummer** (Boss = 10); Gegner-Art skaliert mit der Etage.

## 2. Nicht-Ziele

Heilung/Rast-Knoten, Items, echte Movesets (Welle 2), Entwicklungs-**Mechanik** (Auto-Evolve bei Level — Daten existieren, Auslöser später), XP-Punkte-System (wir nutzen Level-Fortschritt als Bruch).

## 3. Run-Kader in `RunState` (Map-Core)

```csharp
public sealed class MonsterSave
{
    public string SpeciesName;   // z.B. "Mossprig" (Resources/Species)
    public int Level;            // Start 1
    public float LevelProgress;  // [0,1), Bruchteil zum nächsten Level
    public int CurrentHp;        // persistiert; int.MaxValue = voll
}

public static List<MonsterSave> PlayerRoster;   // Team für den ganzen Run
public const float BenchShare = 0.5f;           // Bank-Anteil pro Sieg
public const int BossLevel = 10;
```
`NewRun(seed)` legt zusätzlich den Start-Kader an: **Mossprig (L1), Briarstag (L1)**, beide volle KP.

Neue Methoden:
- `int PendingEnemyLevel()` — Reihe des `PendingNodeId`-Knotens; Boss → `BossLevel`; Fallback 5, falls keine Map/Knoten.
- `string PendingEnemySpecies()` — Etage 1–3 → "Mossprig", 4–6 → "Briarstag", 7–8 + Boss → "Elderthorn".
- `void ApplyWin(bool[] participated)` — pro Kader-Index: `+1.0` (Teilnehmer) bzw. `+BenchShare`; `while (Progress ≥ 1) { Level++; Progress -= 1; }`.
- `void WriteBackHp(int index, int hp)` — schreibt aktuelle KP in den Kader.

`ReportBattleResult(won)` bleibt für die Knoten-Progression (Sieg rückt vor, Niederlage = Run vorbei).

## 4. Teilnahme-Tracking

`Pokemon` bekommt `public bool Participated`. Die `BattleEngine` setzt es `true`, sobald ein Monster aktiv wird: im Konstruktor (beide aktiven), bei jedem Wechsel (`ApplyOrQueue`-Switch) und bei `ResolveForcedSwitch`. So sind „Teilnehmer" = alle Monster, die im Kampf auf dem Feld waren.

`Pokemon` bekommt außerdem `public void SetCurrentHp(int hp)` (geklemmt 0..MaxHp) zum Wiederherstellen persistierter KP.

`Party`-Konstruktor startet `ActiveIndex` auf das **erste nicht-besiegte** Mitglied (falls `Members[0]` aus einem früheren Kampf besiegt ist).

## 5. Battle-Anbindung (`BattleController`, Glue-Schicht)

`Battle`-Assembly referenziert neu **`Map`** (für `RunState`). Reine Engine bleibt Map-frei.

`StartBattle()`:
- **Wenn `RunState.InRun`:** Spieler-Partei aus `RunState.PlayerRoster` bauen (Species per `Resources.Load`, auf gespeichertem Level, `SetCurrentHp` falls < Max); Gegner = 1 Monster aus `PendingEnemySpecies()` auf `PendingEnemyLevel()`, volle KP. Platzhalter-Grass-Moves (Welle 2).
- **Sonst (Standalone):** `SampleData` wie bisher.

Bei Kampfende (`_engine.IsOver`, einmalig pro Kampf): wenn `InRun` →
- Teilnahme/KP je Kader-Index aus der Spieler-Partei lesen, `WriteBackHp` für alle,
- bei Sieg `ApplyWin(participated)`,
- `ReportBattleResult(won)`.

Diese Auflösung wandert vom HUD in den Controller (der Controller kennt die Parteien). Das HUD zeigt weiterhin Ergebnis + „Continue"-Button (bei `InRun`).

**Konsequenz (akzeptiert):** Da man nur nach einem Sieg weiterzieht (≥1 Monster überlebt), startet jeder Kampf mit ≥1 einsatzfähigen Monster; besiegte Team-Mitglieder bleiben besiegt (KP 0) bis spätere Heilung.

## 6. Demo/Standalone

Ohne aktiven Run (Battle-Szene direkt) bleibt das bisherige SampleData-Verhalten (Mossprig+Briarstag vs Elderthorn, Level wie in SampleData). `SampleData` bekommt einen Helfer `PlaceholderGrassMoves()`, den auch der Run-Aufbau nutzt.

## 7. Tests (EditMode, Map-Core, rein)

`LevelingTests` (im `Map.Tests`-Assembly):
- `NewRun`: Kader = [Mossprig L1, Briarstag L1], volle KP (`CurrentHp == int.MaxValue`).
- `ApplyWin([true,false])`: Index 0 → Level 2; Index 1 → Level 1, Progress 0.5. Nochmal → Index 1 Level 2, Progress 0.
- `PendingEnemyLevel`: Knoten Reihe r → r; Boss-Knoten → 10.
- `PendingEnemySpecies`: Reihe 2 → "Mossprig", 5 → "Briarstag", 8 → "Elderthorn", Boss → "Elderthorn".

## 8. Dateien

```
Modified: Assets/Scripts/Map/Core/RunState.cs            (Kader, Leveling, Gegner-Formel)
Modified: Assets/Scripts/Battle/Core/Pokemon.cs          (Participated, SetCurrentHp)
Modified: Assets/Scripts/Battle/Core/BattleEngine.cs     (Participated setzen)
Modified: Assets/Scripts/Battle/Core/Party.cs            (Start auf erstem nicht-besiegten)
Modified: Assets/Scripts/Battle/Control/BattleController.cs (Run-Aufbau + Ergebnis-Rückschrieb)
Modified: Assets/Scripts/Battle/Battle.asmdef            (+ "Map"-Referenz)
Modified: Assets/Scripts/Battle/View/BattleHud.cs        (Ergebnis-Logik raus, Continue bleibt)
Modified: Assets/Scripts/Battle/Fixtures/SampleData.cs   (PlaceholderGrassMoves-Helfer)
Created:  Assets/Scripts/Map/Tests/LevelingTests.cs
```

## 9. Entscheidungen

- Bank-Anteil **0.5**, Gegner-Art nach Etage, „besiegt bleibt besiegt" — vom Nutzer freigegeben („passt").
- Start-Level 1 → bewusst kleine Werte am Anfang.
- Architektur: `Battle → Map` (kein Zyklus, Map hängt von nichts ab).
