# Electric Line (Voltwig→Stormbark→Tempestag) — Design

**Datum:** 2026-06-29
**Projekt:** MonsterCatcher (Unity 6 `6000.4.3f1`)
**Status:** Freigegeben, umgesetzt
**Verwandt:** `2026-06-29-fire-line-and-learnsets-design.md`, `2026-06-29-move-library-design.md`

---

## 1. Ziel

Dritte Monster-Linie (Elektro, **physischer Sweeper**) — der bislang fehlende Archetyp neben Grass (Spezial-Tank) und Fire (Spezial-Glaskanone). Plus Erweiterung von Starter- und Gegner-Pool auf alle drei Linien.

## 2. Elektro-Linie (Type Electric, Entwicklung 16 / 34)

| Spezies | KP | Ang | Vert | SpAng | SpVert | Speed | BST |
|---|---|---|---|---|---|---|---|
| Voltwig | 50 | 65 | 50 | 40 | 45 | 90 | 340 |
| Stormbark | 70 | 95 | 65 | 50 | 60 | 115 | 455 |
| Tempestag | 85 | 125 | 80 | 65 | 75 | 145 | 575 |

Physischer Sweeper: hoher Angriff + Speed, niedriger Sp.Angriff, solide Bulk. Sprites in `Assets/Sprites/`; SpeciesData in `Assets/Resources/Species/`.

## 3. Lernsets (physische Offensive + Paralyse)

- Voltwig: Tackle1, Spark1, StaticShock6, Bite10, VoltTackle14
- Stormbark: Tackle1, Spark1, Bite1, StaticShock18, IronHead24, VoltTackle30
- Tempestag: Tackle1, Spark1, Bite1, StaticShock1, IronHead34, VoltTackle44

Spark (30% Para), Static Shock (100% Para), Volt Tackle (Rückstoß+Para), Bite/Iron Head als Coverage.

## 4. Einbindung (RunState)

- **Starter-Pool** → `{Mossprig, Cindrop, Voltwig}` (`StarterFor`).
- **Gegner-Pool** → `EnemyLines` bekommt die Elektro-Reihe; alle **neun** Monster können spawnen. `EnemyElement` neu als `(nodeId*31 + tier*17) % EnemyLines.Length` (saubere Verteilung über 3 Linien, deterministisch pro Node+Tier; für 2 Linien identisch zur vorherigen Parität).

## 5. Tests (85 EditMode grün)

`ElectricLineTests` (laden als Electric, Kette 16/34, Sweeper-Profil, Sprites+Lernset, Voltwig L1 Electric-Move). `LevelingTests` erweitert: StarterIsRandomFirstStage (3 Starter inkl. Voltwig), EnemyPoolAllThreeLinesSpawn, NewRunRoster.

## 6. Dateien

```
Created:  Assets/Resources/Species/{Voltwig,Stormbark,Tempestag}.asset
Modified: Assets/Scripts/Map/Core/RunState.cs   (Starters + EnemyLines + EnemyElement)
Created:  Assets/Scripts/Battle/Tests/ElectricLineTests.cs
Modified: Assets/Scripts/Map/Tests/LevelingTests.cs
```

## 7. Entscheidungen

- Elektro = physischer Sweeper (3-Wege-Varianz: Tank / Spezial-Nuke / Phys-Sweeper).
- Starter & Gegner skalieren mit der Linienzahl (Pools datengetrieben über `Starters`/`EnemyLines`).
- Offen: weitere Linien lassen sich rein über Daten (neue `EnemyLines`-Reihe + `Starters`-Eintrag) ergänzen.
