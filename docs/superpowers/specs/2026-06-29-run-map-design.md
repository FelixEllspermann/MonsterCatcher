# Run-Map (Slay-the-Spire-Stil) — Design

**Datum:** 2026-06-29
**Projekt:** MonsterCatcher (Unity 6 `6000.4.3f1`, 2D, URP)
**Status:** Entwurf zur Freigabe
**Verwandt:** Kampfsystem-Spec `2026-06-29-pokemon-battle-system-design.md`

---

## 1. Ziel & Umfang

Eine Roguelike-Run-Map im Stil von *Slay the Spire*:

- **Prozedural pro Run** generiert (jeder Run neu, mit Seed).
- **START-Knoten** unten, **BOSS-Knoten** oben, dazwischen **immer 8 Etagen** mit je **zufällig 2–5 Knoten**.
- Knoten sind durch **Wege** (Kanten) verbunden; man bewegt sich nur entlang verbundener Kanten **nach oben**.
- **v1: alle Etagen-Knoten sind Kampf-Knoten**, der oberste ist ein **Boss-Kampf**.
- Knoten anklicken → Kampf-Szene; **Sieg** → Knoten geschafft, weiter; **Niederlage** → **Run vorbei** (Neustart mit neuer Map).
- Boss besiegen → **Run geschafft** → neuer Run.

## 2. Nicht-Ziele (später)

Weitere Knotentypen (Event/Shop/Rast), Belohnungen/Items, Heilung/Lebenspunkte über die Map hinweg, eigene Boss-Encounter (v1 nutzt denselben SampleData-Kampf), Speichern/Laden zwischen App-Starts, Scroll-Map (v1 passt auf einen Screen).

## 3. Architekturüberblick

```
Map.unity  ──(Knoten-Klick)──►  RunState (statisch)  ──►  Battle.unity
   ▲                                  │  Map + Fortschritt          │
   └──────────(Continue)──────────────┴─────────(Kampfende)─────────┘
```

- **`Map` (Core, reines C#):** Datenmodell, Generator, `RunState`. Keine `UnityEngine`-Abhängigkeit → EditMode-testbar.
- **`Map.View`:** `MapController` (MonoBehaviour) baut die UI zur Laufzeit, behandelt Klicks, lädt Szenen.
- **`RunState`** (statisch in Map-Core) hält die generierte Map + Fortschritt und überlebt den Szenenwechsel zur Kampf-Szene.
- Kopplung nur **`Battle.View → Map`** (für `RunState` + „Continue"); die Kampf-**Engine** bleibt unberührt. `Map.View` lädt `Battle.unity` per Namen (keine Assembly-Referenz auf Battle nötig).

## 4. Datenmodell (reines C#)

```csharp
public enum NodeType { Start, Battle, Boss }
public enum NodeStatus { Locked, Available, Current, Cleared }

public sealed class MapNode
{
    public int Id;
    public int Row;             // 0 = START, 1..8 = Etagen, 9 = BOSS
    public float X;             // normalisiert [0..1] für das Layout
    public NodeType Type;
    public readonly List<int> Next = new List<int>();   // Kanten zur Reihe darüber
}

public sealed class MapModel
{
    public IReadOnlyList<MapNode> Nodes;
    public int StartId, BossId;
    public int RowCount;        // 10
    public MapNode Get(int id);
    public IEnumerable<MapNode> NodesInRow(int row);
}
```

`RunState` (statisch):
```csharp
public static class RunState
{
    public static bool InRun;
    public static MapModel Map;
    public static int CurrentNodeId;
    public static int PendingNodeId;          // gerade angeklickter Kampf
    public static bool RunWon, RunLost;
    public static readonly HashSet<int> Cleared = new HashSet<int>();

    public static void NewRun(int seed);      // generiert Map, Current = START, reset Flags
    public static IReadOnlyList<int> Available();          // Next des aktuellen Knotens
    public static bool CanSelect(int id);                  // id ∈ Available()
    public static void Select(int id);                     // setzt PendingNodeId
    public static void ReportBattleResult(bool won);       // Fortschritt/Run-Ende
    public static NodeStatus StatusOf(int id);             // für das Rendering
}
```
`ReportBattleResult(won)`: wenn `won` → `Cleared.Add(PendingNodeId)`, `CurrentNodeId = PendingNodeId`; war es der Boss → `RunWon = true`. Wenn `!won` → `RunLost = true`.
`StatusOf(id)`: `Cleared` falls in `Cleared`; `Current` falls `== CurrentNodeId`; `Available` falls in `Available()`; sonst `Locked`.

## 5. Generierung (`MapGenerator.Generate(int seed)`)

Reihen: `0` START, `1..8` Etagen, `9` BOSS (`RowCount = 10`).

1. **START** (Row 0, X=0.5), **BOSS** (Row 9, X=0.5).
2. Für jede Etage `f` in `1..8`: Knotenzahl `N = rng.IntInclusive(2, 5)`. Knoten `i` bei `X = (i + 0.5) / N` (gleichmäßig über [0..1]).
3. **Kanten** (jeweils nur zwischen Nachbarreihen):
   - `START.Next = ` **alle** Knoten der Etage 1.
   - Für `f` in `1..7`: für jeden Knoten `a` in Etage `f` verbinde mit dem **x-nächsten** Knoten in Etage `f+1`; mit Wahrscheinlichkeit `pBranch = 0.4` zusätzlich mit dem **zweitnächsten**.
   - **Abdeckung:** Knoten in Etage `f+1` ohne eingehende Kante → vom **x-nächsten** Knoten der Etage `f` verbinden.
   - **Etage 8 → BOSS:** jeder Knoten der Etage 8 bekommt `BOSS` in `Next`.
4. Doppelkanten vermeiden.

**Garantierte Invarianten** (siehe Tests): genau 1 START, genau 1 BOSS; jede Etage hat ≥1 Knoten; jeder Knoten hat (außer BOSS) ≥1 ausgehende und (außer START) ≥1 eingehende Kante; jeder Knoten ist von START erreichbar und erreicht BOSS; Kanten nur zwischen Reihe `r` und `r+1`.

Determinismus: gleicher Seed → gleiche Map (für Tests + Reproduzierbarkeit).

## 6. Navigation (Slay-the-Spire-Regel)

- Run-Start: `Current = START`, `Available =` alle verbundenen Knoten der Etage 1.
- Klick auf einen **verfügbaren** Knoten → `Select(id)` → Kampf-Szene laden. Nicht-verfügbare Knoten sind gesperrt. **Kein Zurück.**
- Sieg → Knoten `Cleared`, `Current` rückt auf den Knoten, nächste Reihe wird `Available`. Bis **BOSS**.

## 7. Szenenfluss & Kampf-Anbindung

- **`Map.unity`** (neue Einstiegsszene): beim Laden →
  - `RunWon` → Sieg-Overlay + „Neuer Run" (`NewRun`).
  - `RunLost` → Game-Over-Overlay + „Neuer Run" (`NewRun`).
  - sonst: `RunState.Map` rendern (falls `!InRun`: `NewRun(seed)`).
- Knoten-Klick → `RunState.Select(id)` → `SceneManager.LoadScene("Battle")`.
- **`Battle`-Szene:** läuft wie bisher. Erweiterung in `BattleHud`: bei **Kampfende** und `RunState.InRun` → `RunState.ReportBattleResult(result == PlayerWon)` und ein **„Continue"**-Button → `SceneManager.LoadScene("Map")`. Ohne aktiven Run (Standalone) bleibt das Verhalten wie jetzt (Ergebnis anzeigen, kein Szenenwechsel).
- **Boss-Kampf** nutzt v1 denselben SampleData-Kampf (eigener Boss-Gegner später).

## 8. Rendering (`MapController`, Runtime-UI wie das Battle-HUD)

- Canvas (Screen Space Overlay, ScaleWithScreenSize 1280×720), wie im HUD aufgebaut.
- **Map-Fläche** z. B. y 0.06–0.94. Knoten-Position: `posY = lerp(unten, oben, Row / 9)`, `posX = lerp(linker Rand, rechter Rand, node.X)`.
- **Knoten** = runde Buttons (Icon/Buchstabe je Typ: S / Kampf-Punkt / „BOSS"). **Kanten** = dünne, gedrehte `Image`s hinter den Knoten (Mitte = Mittelpunkt, Breite = Distanz, Winkel = `atan2`).
- **Zustände:** `Current` hervorgehoben, `Available` leuchtend + klickbar, `Locked` abgeblendet + nicht klickbar, `Cleared` markiert (Häkchen/Farbe). Boss optisch abgesetzt.
- Klick auf `Available`-Knoten → Szenenwechsel (s. o.). Über 10 Reihen passt das auf einen Screen (≈ 64–72 px Reihenabstand, kleine Knoten).

## 9. Run-Niederlage / -Sieg

- **Niederlage** (`RunLost`): Game-Over-Overlay auf der Map; Button „Neuer Run" → `NewRun(neuer Seed)` → frische Map.
- **Boss-Sieg** (`RunWon`): Sieg-Overlay; Button „Neuer Run".

## 10. Tests (EditMode, `MapGenerator` rein)

Über viele Seeds (`0..50`) je Map prüfen:
- genau 1 START (Row 0) und 1 BOSS (Row 9); `RowCount == 10`.
- Etagen `1..8` existieren, jede mit Knotenzahl in `[2,5]`.
- jeder Knoten ≠ BOSS hat ≥1 ausgehende Kante; jeder Knoten ≠ START hat ≥1 eingehende Kante.
- **Erreichbarkeit:** BFS von START erreicht alle Knoten; rückwärts erreicht jeder Knoten den BOSS.
- Kanten nur zwischen Reihe `r` und `r+1`.
- Seed-Determinismus: `Generate(s)` zweimal → identische Struktur.

`RunState`-Tests: `NewRun` setzt Current=START; `Available` = Etage-1-Knoten; `Select` nur für verfügbare; `ReportBattleResult(true)` rückt Current vor und schaltet nächste Reihe frei; Boss-Sieg setzt `RunWon`; Niederlage setzt `RunLost`.

## 11. Dateien & Assemblies

```
Assets/Scripts/Map/
  Map.asmdef                 (core, reines C#)
  Core/ NodeType.cs, NodeStatus.cs, MapNode.cs, MapModel.cs, MapGenerator.cs, RunState.cs
  View/ Map.View.asmdef      (refs: Map, UnityEngine.UI), MapController.cs
Assets/Scripts/Map/Tests/
  Map.Tests.asmdef           (EditMode, refs Map), MapGeneratorTests.cs, RunStateTests.cs
Assets/Scenes/Map.unity      (neue Einstiegsszene)
```
- `Battle.View.asmdef` bekommt eine Referenz auf **`Map`** (für `RunState` + „Continue").
- **Build Settings:** `Map.unity` als **Index 0** (Einstieg), `Battle.unity` bleibt enthalten.

## 12. Entscheidungen / offene Punkte

- **Map als Einstiegsszene** (Index 0) — angenommen; SampleScene bleibt vorerst im Projekt, aber nicht Einstieg.
- **Boss = SampleData-Kampf** in v1 — eigener Boss-Gegner, wenn echte Roster existieren.
- **Mapgröße fix:** 8 Etagen, 2–5 Knoten je Etage (vom Nutzer vorgegeben).
- Reihenabstand/Knotengröße werden beim Bauen visuell justiert (Screenshot-Iteration).
