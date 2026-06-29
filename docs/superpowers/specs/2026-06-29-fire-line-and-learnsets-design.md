# Fire Line (Cindrop→Magmelt→Vulcarion) + Level Learnsets — Design

**Datum:** 2026-06-29
**Projekt:** MonsterCatcher (Unity 6 `6000.4.3f1`)
**Status:** Freigegeben, umgesetzt
**Verwandt:** `2026-06-29-...-monster...`, `2026-06-29-move-library-design.md`

---

## 1. Ziel

Zweite Monster-Linie (Feuer, **Spezial-Glaskanone**) als Gegenstück zur Pflanzen-Linie (Spezial-Tank), plus sinnvolle Level-Lernsets aus der 76-Move-Bibliothek für beide Linien. Außerdem: zufälliger Starter (Grass *oder* Fire) und ein Gegner-Pool aus allen sechs Monstern.

## 2. Feuer-Linie (Type Fire, Entwicklung 16 / 34)

| Spezies | KP | Ang | Vert | SpAng | SpVert | Speed | BST |
|---|---|---|---|---|---|---|---|
| Cindrop | 45 | 40 | 40 | 75 | 45 | 95 | 340 |
| Magmelt | 65 | 50 | 50 | 110 | 60 | 120 | 455 |
| Vulcarion | 85 | 60 | 65 | 155 | 75 | 135 | 575 |

Glaskanone: SpAng + Speed dominieren, KP/Def/SpVert niedrig (vgl. Vulcarion SpVert 75 vs. Elderthorn 125). Sprites in `Assets/Sprites/` (Front + Back), als Sprite-Sub-Assets referenziert. SpeciesData in `Assets/Resources/Species/`.

## 3. Lernsets (aktive 4 = die zuletzt gelernten 4)

**Feuer (Spezial-Offensive):** Tackle / Ember (30% Brand) / Swift (trifft immer) / Searing Heat (Brand) / Fire Blast / Hyper Beam — pro Stufe später verfügbar.
- Cindrop: T1, Ember1, Swift5, SearingHeat9, FireBlast13
- Magmelt: T1, Ember1, Swift1, SearingHeat18, FireBlast24, HyperBeam30
- Vulcarion: T1, Ember1, Swift1, SearingHeat1, FireBlast34, HyperBeam44

**Pflanze (Spezial-Tank, neu verteilt):** Tackle / Plant Whip / Mega Drain (Lebensraub) / Glare (−Gegner-Ang.) / Sleep Spore (Schlaf) / Solar Ray (Aufladung).
- Mossprig: T1, PlantWhip1, MegaDrain4, Glare7, SleepSpore10
- Briarstag: T1, PlantWhip1, MegaDrain1, Glare18, SleepSpore24, SolarRay30
- Elderthorn: T1, PlantWhip1, MegaDrain1, Glare1, SleepSpore34, SolarRay42

## 4. Einbindung (RunState)

- **Starter:** `NewRun(seed)` setzt **einen** zufälligen Starter `StarterFor(seed)` aus `{Mossprig, Cindrop}` (erste Stufe). Roster damit 1 Monster (vorher fix 2).
- **Gegner-Pool:** `EnemySpeciesFor(type, row, nodeId, tier)` = `EnemyLines[Element][Stage]`. `StageForRow`: Etage 1–3 → Stufe 0, 4–6 → Stufe 1, 7–8/Boss → Stufe 2. Element (Grass/Fire) deterministisch aus `(nodeId, tier)` — beide Linien können überall auftauchen. `PendingEnemySpecies()` nutzt das.

## 5. Tests (alle grün, 81 EditMode)

`FireLineTests` (laden als Fire, Entwicklungskette + Level 16/34, Glaskanonen-Profil, Sprites + Lernset, Cindrop hat L1 Fire-Move). `SpeciesAssetTests.GrassLearnsetRedistributed`. `LevelingTests`: NewRunRoster (1 Starter), StarterIsRandomFirstStage, EnemyStageByFloor, EnemyPoolBothLinesSpawn, Bench-Leveling (mit manuell hinzugefügtem 2. Monster). `HealTests` analog angepasst.

## 6. Dateien

```
Created:  Assets/Resources/Species/{Cindrop,Magmelt,Vulcarion}.asset
Modified: Assets/Resources/Species/{Mossprig,Briarstag,Elderthorn}.asset  (Lernsets)
Modified: Assets/Scripts/Map/Core/RunState.cs   (StarterFor, EnemyLines/StageForRow/EnemySpeciesFor)
Created:  Assets/Scripts/Battle/Tests/FireLineTests.cs
Modified: Assets/Scripts/Map/Tests/{LevelingTests,HealTests}.cs, Battle/Tests/SpeciesAssetTests.cs
```

## 7. Entscheidungen

- Feuer = Spezial-Glaskanone (Kontrast zum Pflanzen-Tank); Entwicklung 16/34 wie Grass.
- Ein zufälliger Starter statt zwei fester; alle sechs als Gegner möglich (Stufe nach Etage).
- Offen für später: zweites Team-Monster/Fang-Mechanik (aktuell startet man mit 1 Monster).
