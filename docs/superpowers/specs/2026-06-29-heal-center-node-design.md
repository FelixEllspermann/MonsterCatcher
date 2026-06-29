# Heal Center Node — Design

**Datum:** 2026-06-29
**Projekt:** MonsterCatcher (Unity 6 `6000.4.3f1`)
**Status:** Freigegeben (Design), Spec zur Umsetzung
**Verwandt:** `2026-06-29-run-map-design.md`, `2026-06-29-leveling-and-enemy-scaling-design.md`

---

## 1. Ziel

Ein neuer Map‑Knotentyp **Heal Center**: dorthin gehen heilt den gesamten Kader voll (und belebt besiegte Monster wieder). Kein Kampf.

## 2. Platzierung

- **`NodeType.Heal`** (neu, ans Enum-Ende → serialisierte Werte stabil).
- In `MapGenerator`: jeder Etagen-Knoten **ab Etage 2** (Reihen 2–8) wird mit **~10 % Chance** (`HealChance = 0.1`) ein `Heal` statt `Battle`. **Etage 1 immer Kampf.** Start/Boss unverändert.
- Heal-Knoten sind normale Graph-Knoten (gleiche Kanten/Erreichbarkeit) — nur der Typ unterscheidet sich. Generierungs-Invarianten (jede Etage 2–5 Knoten, alles erreichbar) bleiben gültig.

## 3. Wirkung (`RunState`)

- `HealParty()` — setzt bei allen `PlayerRoster`-Einträgen `CurrentHp = int.MaxValue` (Sentinel „voll") → nächster Kampf baut auf volle KP; **belebt besiegte (0 KP) wieder**.
- `VisitHeal(int id)` — wenn `CanSelect(id)`: `HealParty()`, `Cleared.Add(id)`, `CurrentNodeId = id`; gibt `true` zurück. **Kein** Kampf, **kein** Leveln, **kein** Szenenwechsel.
- **PP** sind bereits jeden Kampf voll (Movesets werden pro Kampf frisch aus dem Lernset gebaut). Das Heal-Center stellt also die persistenten **KP** wieder her.

## 4. `MapController` — Klick nach Typ

`OnNodeClicked(id)`: nur wenn `CanSelect(id)`.
- `node.Type == Heal` → `RunState.VisitHeal(id)`, Titel kurz „Party fully healed!", `RefreshNodes()` (Knoten jetzt geschafft, nächste Etage frei). **Bleibt auf der Map.**
- sonst (`Battle`/`Boss`) → `RunState.Select(id)` + Szene `Battle` laden (wie bisher).

## 5. Rendering

- Heal-Knoten in **Rosa** (Available hell, Locked gedimmt, Cleared/Current mittel), Label **„+"**, Größe wie Kampf-Knoten (54×54). Klar abgesetzt von Kampf (grün) und Boss (rot).
- Titel-Text wird als Feld gehalten, um die Heal-Bestätigung anzuzeigen.

## 6. Tests (EditMode, Map-Core)

- `MapGenerator`: über Seeds 0–50 tauchen Heal-Knoten auf (`> 0` gesamt); jeder Heal-Knoten hat `Row` in `[2,8]`; bestehende Invarianten (Erreichbarkeit, Etagengröße) bleiben grün.
- `RunState.VisitHeal`: heilt alle Kader-Monster auf voll (inkl. wiederbeleben), markiert den Knoten `Cleared` und setzt `CurrentNodeId` vor.

## 7. Dateien

```
Modified: Assets/Scripts/Map/Core/NodeType.cs        (+ Heal)
Modified: Assets/Scripts/Map/Core/MapGenerator.cs    (10% Heal ab Etage 2)
Modified: Assets/Scripts/Map/Core/RunState.cs        (HealParty, VisitHeal)
Modified: Assets/Scripts/Map/View/MapController.cs    (Klick-Dispatch, Heal-Farbe/Label, Titel-Feld)
Created:  Assets/Scripts/Map/Tests/HealTests.cs
Modified: Assets/Scripts/Map/Tests/MapGeneratorTests.cs (Heal-Knoten-Test)
```

## 8. Entscheidungen

- 10 % pro Knoten, **ab Etage 2** (vom Nutzer freigegeben).
- Heal = volle KP + Wiederbelebung; PP ohnehin pro Kampf voll.
