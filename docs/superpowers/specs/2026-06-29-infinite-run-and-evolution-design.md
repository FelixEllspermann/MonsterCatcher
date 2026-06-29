# Infinite Run (Tiers) & Evolution — Design

**Datum:** 2026-06-29
**Projekt:** MonsterCatcher (Unity 6 `6000.4.3f1`)
**Status:** Freigegeben (Design), Spec zur Umsetzung
**Verwandt:** `2026-06-29-run-map-design.md`, `2026-06-29-leveling-and-enemy-scaling-design.md`, `2026-06-29-grass-starter-line-design.md`

---

## 1. Ziel

1. **Infinite Run (Ebenen/Tiers):** Boss besiegen → neue Map (nächste Ebene), Kader bleibt; es geht weiter, bis man verliert.
2. **Entwicklungs-Mechanik (mit Abfrage):** erreicht ein Monster sein `EvolveLevel`, wird nach dem Kampf gefragt, ob es sich entwickelt. Durch den endlosen Run sind die bestehenden Level (16/34) jetzt erreichbar.

## 2. Infinite Run

`RunState`:
- Neu `int Tier` (Start 1; in `NewRun` gesetzt).
- `NextTier(int seed)`: `Tier++`, neue `Map = Generate(seed)`, Fortschritt zurück (`CurrentNodeId = StartId`, `Cleared` reset, `PendingNodeId = -1`, `RunWon = false`), **`PlayerRoster` bleibt unverändert**.
- `PendingEnemyLevel()` skaliert mit Tier: `tierBase = (Tier-1) * BossLevel`; Rückgabe `tierBase + (Boss ? BossLevel : node.Row)`. (Ebene 1: 1–8, Boss 10; Ebene 2: 11–18, Boss 20; …)

`MapController.Start()`:
- `Map == null` → `NewRun` (Ebene 1).
- sonst `RunWon == true` (Boss gerade besiegt) → `NextTier(...)` (neue Map, Kader bleibt) — **kein Sieg-Overlay mehr**.
- `RunLost` → Game-Over-Overlay „you reached tier N", Button „New Run" → `NewRun` (Ebene 1, frischer Kader).
- Titel: „Tier N — reach the BOSS".

Niederlage (Kampf verloren) = Run endet (man stirbt). KP bleiben über Ebenen hinweg (kein Auto-Heilen; Heal-Center heilen).

## 3. Evolution (mit Abfrage)

`SpeciesData`:
- `bool CanEvolveAt(int level)` → `EvolvesInto != null && EvolveLevel > 0 && level >= EvolveLevel`.

`BattleController` (kennt `RunState` + `SpeciesData`):
- Nach dem Kampf, bei Sieg, nach `RunState.ApplyWin`: für jeden Kader-Index die Art laden; ist `CanEvolveAt(save.Level)` → `EvolutionOffer { RosterIndex, FromName, ToName }` sammeln. Öffentlich `PendingEvolutions`.
- `EvolveRosterMonster(int rosterIndex)`: setzt `RunState.PlayerRoster[i].SpeciesName = species.EvolvesInto.name`.

`BattleHud` (nach Kampfende, vor „Continue"):
- Gibt es `PendingEvolutions` → Abfrage-Sequenz: Meldung „X entwickelt sich zu Y!" + Buttons **„Evolve" / „Not now"**.
  - *Evolve* → `EvolveRosterMonster(i)`, nächstes Angebot.
  - *Not now* → überspringen (beim nächsten Level-up erneut, da weiterhin berechtigt), nächstes Angebot.
- Nach Abarbeitung aller Angebote → Ergebnis-Text + „Continue".

Effekt: Beim nächsten Kampf wird die **entwickelte Art** gebaut (höhere Werte, neues Lernset → neue Moves); Level/KP bleiben (KP auf neues Maximum geklemmt, kein KP-Verlust).

## 4. Tests (EditMode)

- `RunState.NextTier` (Map.Tests): `Tier` steigt, Map neu, `PlayerRoster` bleibt (gleiche Referenz/Level), Fortschritt zurück, `RunWon` false.
- `PendingEnemyLevel` skaliert mit Tier (Map.Tests): gleiche Etage in Ebene 2 = Ebene-1-Level + `BossLevel`.
- `SpeciesData.CanEvolveAt` (Battle.Tests): unter/auf Level, ohne `EvolvesInto` → false.

## 5. Dateien

```
Modified: Assets/Scripts/Map/Core/RunState.cs           (Tier, NextTier, PendingEnemyLevel-Skalierung)
Modified: Assets/Scripts/Map/View/MapController.cs        (NextTier bei Boss-Sieg, Titel mit Tier)
Modified: Assets/Scripts/Battle/Data/SpeciesData.cs       (CanEvolveAt)
Modified: Assets/Scripts/Battle/Control/BattleController.cs (PendingEvolutions, EvolveRosterMonster)
Modified: Assets/Scripts/Battle/View/BattleHud.cs          (Entwicklungs-Abfrage-Panel + Flow)
Created:  Assets/Scripts/Map/Tests/TierTests.cs
Created:  Assets/Scripts/Battle/Tests/EvolutionTests.cs
```

## 6. Entscheidungen

- Gegner-Skalierung **+10 Level/Ebene** (Boss = Tier·10); Spieler ~+9/Ebene → langsam stärkere Gegner → irgendwann Tod.
- Entwicklungs-Level **16/34** bleiben (jetzt erreichbar durch endlosen Run).
- Entwicklung **mit Abfrage**; „Not now" wird beim nächsten Level-up erneut angeboten.
- Kein Auto-Heilen zwischen Ebenen.
