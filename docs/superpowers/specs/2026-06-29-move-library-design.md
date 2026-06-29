# Move Library (~76 moves) — Design

**Datum:** 2026-06-29
**Projekt:** MonsterCatcher (Unity 6 `6000.4.3f1`)
**Status:** Freigegeben (Design), Spec zur Umsetzung
**Verwandt:** `2026-06-29-first-moves-and-learnsets-design.md`

---

## 1. Ziel

Eine abwechslungsreiche Attacken-Bibliothek (kein Einheits-Schema): pro Typ 4 Moves + 4 Status-Moves, jede mit eigener Rolle über einen Effekt-Baukasten. Library in `Assets/Resources/Moves/`, vorerst keinem Monster zugewiesen.

## 2. Engine-Erweiterung (umgesetzt)

`MoveData` neu: `int RecoilPercent` (Anwender nimmt % des Schadens), `int DrainPercent` (Anwender heilt % des Schadens), `bool HighCrit` (höhere Volltreffer-Chance = `CritChance*8`). Engine: `BattleEngine.ApplyRecoilAndDrain` nach dem Treffer (+ `RecoilEvent`/`DrainEvent`, im HUD-Log), Selbst-K.O. durch Rückstoß wird geprüft; `DamageCalculator` nutzt die erhöhte Crit-Chance. Schon vorhanden: Priorität, Sekundär-Status (`InflictsStatus`+`StatusChance`), Sekundär-Wertänderung (`StatToChange`/`StatStageDelta`/`StatChangeTargetsSelf`/`StatChangeChance`), 2-Runden-Aufladung (`ChargesUp`).

## 3. Move-Tabelle (Power/Acc/PP, Acc 0 = trifft immer)

Pro Typ: Basis-Phys, Basis-Spez, Stark-Phys, Stark-Spez.

| Typ | Move | Kat | Pow | Acc | PP | Effekt |
|---|---|---|---|---|---|---|
| Normal | Pound | P | 50 | 100 | 30 | — |
| Normal | Swift | S | 60 | 0 | 20 | trifft immer |
| Normal | Double-Edge | P | 120 | 100 | 10 | Rückstoß 33% |
| Normal | Hyper Beam | S | 130 | 90 | 5 | — |
| Fire | Fire Fang | P | 65 | 95 | 15 | 10% Brand, 10% −Vert. |
| Fire | Ember | S | 40 | 100 | 25 | 30% Brand |
| Fire | Flare Blitz | P | 120 | 100 | 10 | Rückstoß 33%, 10% Brand |
| Fire | Fire Blast | S | 110 | 85 | 5 | 10% Brand |
| Water | Aqua Jet | P | 40 | 100 | 20 | Priorität +1 |
| Water | Water Gun | S | 45 | 100 | 25 | — |
| Water | Wave Crash | P | 120 | 100 | 10 | Rückstoß 33% |
| Water | Hydro Pump | S | 110 | 80 | 5 | — |
| Electric | Spark | P | 65 | 100 | 20 | 30% Paralyse |
| Electric | Shock | S | 50 | 100 | 25 | 10% Paralyse |
| Electric | Volt Tackle | P | 120 | 100 | 10 | Rückstoß 33%, 10% Paralyse |
| Electric | Thunder | S | 110 | 70 | 10 | 30% Paralyse |
| Grass | Leaf Blade | P | 70 | 100 | 15 | hohe Crit |
| Grass | Mega Drain | S | 60 | 100 | 15 | Lebensraub 50% |
| Grass | Wood Hammer | P | 120 | 100 | 10 | Rückstoß 33% |
| Grass | Solar Ray | S | 120 | 100 | 10 | 2-Runden-Aufladung |
| Ice | Ice Shard | P | 40 | 100 | 20 | Priorität +1 |
| Ice | Frost Breath | S | 60 | 100 | 15 | hohe Crit |
| Ice | Icicle Crash | P | 110 | 90 | 10 | — |
| Ice | Blizzard | S | 110 | 70 | 5 | — |
| Fighting | Karate Chop | P | 60 | 100 | 25 | hohe Crit |
| Fighting | Force Palm | S | 50 | 100 | 20 | 30% Paralyse |
| Fighting | Close Combat | P | 120 | 100 | 5 | 100% −eigene Vert. |
| Fighting | Aura Sphere | S | 90 | 0 | 10 | trifft immer |
| Poison | Poison Jab | P | 70 | 100 | 15 | 30% Gift |
| Poison | Acid | S | 50 | 100 | 20 | 20% −Vert. |
| Poison | Gunk Shot | P | 120 | 80 | 5 | 30% Gift |
| Poison | Sludge Bomb | S | 95 | 100 | 10 | 30% Gift |
| Ground | Bonemerang | P | 55 | 90 | 10 | — |
| Ground | Mud Shot | S | 55 | 95 | 15 | — |
| Ground | Earthquake | P | 110 | 100 | 10 | — |
| Ground | Earth Power | S | 95 | 100 | 10 | 20% −Sp.Vert. |
| Flying | Wing Attack | P | 60 | 100 | 25 | — |
| Flying | Gust | S | 40 | 100 | 25 | — |
| Flying | Brave Bird | P | 120 | 100 | 10 | Rückstoß 33% |
| Flying | Air Slash | S | 75 | 95 | 15 | hohe Crit |
| Psychic | Zen Headbutt | P | 80 | 90 | 15 | — |
| Psychic | Confusion | S | 50 | 100 | 25 | — |
| Psychic | Psycho Cut | P | 70 | 100 | 15 | hohe Crit |
| Psychic | Psychic | S | 90 | 100 | 10 | 20% −Sp.Vert. |
| Bug | Bug Bite | P | 60 | 100 | 20 | — |
| Bug | Infestation | S | 45 | 100 | 20 | — |
| Bug | Megahorn | P | 120 | 85 | 10 | — |
| Bug | Bug Buzz | S | 90 | 100 | 10 | 20% −Sp.Vert. |
| Rock | Rock Throw | P | 50 | 90 | 15 | — |
| Rock | Ancient Pulse | S | 60 | 100 | 15 | — |
| Rock | Stone Edge | P | 110 | 80 | 5 | hohe Crit |
| Rock | Power Gem | S | 80 | 100 | 15 | — |
| Ghost | Lick | P | 30 | 100 | 30 | 30% Paralyse |
| Ghost | Astonish | S | 40 | 100 | 25 | — |
| Ghost | Phantom Force | P | 90 | 100 | 10 | 2-Runden-Aufladung |
| Ghost | Shadow Ball | S | 90 | 100 | 10 | 20% −Sp.Vert. |
| Dragon | Dragon Claw | P | 80 | 100 | 15 | — |
| Dragon | Twister | S | 40 | 100 | 20 | — |
| Dragon | Outrage | P | 120 | 100 | 10 | — |
| Dragon | Draco Meteor | S | 130 | 90 | 5 | 100% −eigener Sp.Angr. |
| Dark | Bite | P | 60 | 100 | 25 | — |
| Dark | Snarl | S | 55 | 95 | 15 | 100% −Sp.Angr. Gegner |
| Dark | Crunch | P | 80 | 100 | 15 | 20% −Vert. |
| Dark | Dark Pulse | S | 80 | 100 | 15 | — |
| Steel | Metal Claw | P | 50 | 95 | 25 | 10% +eigener Angr. |
| Steel | Mirror Shot | S | 65 | 85 | 15 | — |
| Steel | Iron Head | P | 80 | 100 | 15 | — |
| Steel | Flash Cannon | S | 80 | 100 | 10 | 20% −Sp.Vert. |
| Fairy | Spirit Slap | P | 60 | 100 | 20 | — |
| Fairy | Fairy Wind | S | 40 | 100 | 25 | — |
| Fairy | Play Rough | P | 90 | 90 | 10 | 20% −Angr. |
| Fairy | Moonblast | S | 95 | 100 | 10 | 20% −Sp.Angr. |

**Status-Moves** (Kategorie Status, Power 0, fügt Status zu 100% zu):
Venom Spray (Gift, Acc 90), Searing Heat (Feuer, Acc 85), Static Shock (Elektro, Acc 90), Sleep Spore (Pflanze, Acc 75).

## 4. Tests

Engine: `RecoilDamagesUser`, `DrainHealsUser`, `HighCritIncreasesCritRate` (Battle.Tests). Library: `MoveLibraryTests` lädt alle Moves und prüft Anzahl (= 76) und dass die Status-Moves ihren Status auf 100% setzen.

## 5. Dateien

```
Modified: Assets/Scripts/Battle/Data/MoveData.cs       (RecoilPercent, DrainPercent, HighCrit)
Modified: Assets/Scripts/Battle/Core/DamageCalculator.cs (HighCrit)
Modified: Assets/Scripts/Battle/Core/BattleEngine.cs    (ApplyRecoilAndDrain + Selbst-K.O.)
Modified: Assets/Scripts/Battle/Core/BattleEvent.cs     (RecoilEvent, DrainEvent)
Modified: Assets/Scripts/Battle/View/BattleHud.cs       (Log-Texte)
Modified: Assets/Scripts/Battle/Tests/MovesTests.cs     (3 Effekt-Tests)
Created:  Assets/Resources/Moves/*.asset                (76 Moves)
Created:  Assets/Scripts/Battle/Tests/MoveLibraryTests.cs
```

## 6. Entscheidungen

- Effekt-Baukasten statt Einheits-Stats; Engine um Recoil/Drain/HighCrit erweitert.
- Bibliothek, noch keinem Monster zugewiesen (kommt mit neuen Monstern). Bestehende 6 Moves unberührt.
