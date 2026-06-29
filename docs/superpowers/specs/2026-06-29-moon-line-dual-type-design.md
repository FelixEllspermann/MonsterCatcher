# Moon Line (Lunakit→Moonlynx→Eclipseon, Fairy/Dark) — Design

**Datum:** 2026-06-29
**Projekt:** MonsterCatcher (Unity 6 `6000.4.3f1`)
**Status:** Freigegeben, umgesetzt
**Verwandt:** `2026-06-29-electric-line-design.md`, `2026-06-29-fire-line-and-learnsets-design.md`

---

## 1. Ziel

Vierte Monster-Linie und **erster Doppel-Typ** (Fairy/Dark), als **gemischter Angreifer** (Angriff ≈ Sp.Angriff, nutzt beide STABs). Pools auf vier Linien erweitert.

## 2. Dual-Type — keine Engine-Änderung nötig

`TypeChart.Effectiveness(atk, t1, t2, hasSecond)` multipliziert bereits beide Verteidigungs-Typen; `DamageCalculator`/`SimpleAI` geben STAB für Type1 **und** Type2. `SpeciesData` hat `Type1/Type2/HasSecondType` schon. Also nur Daten: Type1=Fairy(17), Type2=Dark(15), HasSecondType=true.

## 3. Mond-Linie (Fairy/Dark, Entwicklung 16 / 34)

| Spezies | KP | Ang | Vert | SpAng | SpVert | Speed | BST |
|---|---|---|---|---|---|---|---|
| Lunakit | 55 | 55 | 50 | 55 | 55 | 70 | 340 |
| Moonlynx | 70 | 75 | 65 | 75 | 65 | 105 | 455 |
| Eclipseon | 90 | 95 | 85 | 95 | 90 | 120 | 575 |

Gemischter Angreifer: Angriff = Sp.Angriff, solide Bulk, gute Speed. Sprites in `Assets/Sprites/` (mussten erst per Reimport `.meta`/GUID bekommen). SpeciesData in `Assets/Resources/Species/`.

## 4. Lernsets (gemischt, Dark phys. + Fairy spez.)

- Lunakit: Tackle1, FairyWind1, Bite6, Crunch10, Moonblast14
- Moonlynx: Tackle1, FairyWind1, Bite1, Crunch18, Moonblast24, PlayRough30
- Eclipseon: Tackle1, FairyWind1, Crunch1, PlayRough1, DarkPulse34, Moonblast44 (End-Kit Crunch/PlayRough phys + DarkPulse/Moonblast spez, beide Typen)

## 5. Einbindung (RunState)

- **Starter-Pool** → `{Mossprig, Cindrop, Voltwig, Lunakit}`.
- **Gegner-Pool** → 4. `EnemyLines`-Reihe; **alle zwölf** Monster können spawnen. `EnemyElement = (nodeId*31 + tier*17) % 4` verteilt sauber über vier Linien.

## 6. Tests (90 EditMode grün)

`MoonLineTests`: laden als Fairy+Dark/HasSecondType, Kette 16/34, gemischtes Profil (Atk==SpA), **Dual-Type-Defense** (Poison 2× via Fairy; Fighting 0.5×Fairy · 2×Dark = 1.0 → beweist Stacking beider Typen), Sprites + Dual-STAB-Lernset. `LevelingTests`: vier Starter / `EnemyPoolAllFourLinesSpawn`.

## 7. Dateien

```
Created:  Assets/Resources/Species/{Lunakit,Moonlynx,Eclipseon}.asset
Modified: Assets/Scripts/Map/Core/RunState.cs  (4th Starters + EnemyLines entry)
Created:  Assets/Scripts/Battle/Tests/MoonLineTests.cs
Modified: Assets/Scripts/Map/Tests/LevelingTests.cs
```

## 8. Entscheidungen

- Fairy/Dark = gemischter Angreifer (4 Archetypen: Tank / Spezial-Nuke / Phys-Sweeper / Mixed-Dual-STAB).
- Doppel-Typ rein über Daten; die Engine konnte es bereits.
- Linien skalieren weiter datengetrieben (`EnemyLines`-Reihe + `Starters`-Eintrag).
