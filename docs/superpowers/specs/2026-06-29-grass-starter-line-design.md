# Grass Starter Line (Mossprig → Briarstag → Elderthorn) — Design

**Datum:** 2026-06-29
**Projekt:** MonsterCatcher (Unity 6 `6000.4.3f1`, 2D, URP)
**Status:** Freigegeben (Design), Spec zur Umsetzung
**Verwandt:** Kampfsystem-Spec `2026-06-29-pokemon-battle-system-design.md`

---

## 1. Ziel & Umfang

Die ersten fest definierten Monster ins Spiel bringen: eine **3-stufige Grass-Evolutionskette** mit Stats, Sprites und Evolutions-Verknüpfung, plus **Sprite-Anzeige im Kampf**. Movesets sind **Welle 2**.

- `SpeciesData` um Sprites + Evolution erweitern.
- 3 `SpeciesData`-Assets: **Mossprig → Briarstag → Elderthorn** (alle Typ Grass).
- Sprites (Front/Back) zuweisen und im Kampf anzeigen.
- Demo: Mossprig als Spieler-Starter, damit die Sprites sofort sichtbar sind.

## 2. Nicht-Ziele (später)

Echte Movesets (Welle 2), die eigentliche Entwicklungs-**Mechanik** (Level-up → Form ändern), Fang-/Begegnungs-Logik, die anderen hochgeladenen Linien (Fire: Cindrop/Magmelt/Vulcarion, Electric: Voltwig/Stormbark/Tempestag).

## 3. `SpeciesData`-Erweiterung

Datei `Assets/Scripts/Battle/Data/SpeciesData.cs`, neue Felder:
```csharp
public Sprite FrontSprite;      // Gegner-Ansicht
public Sprite BackSprite;       // eigene Ansicht
public SpeciesData EvolvesInto; // nächste Stufe, null = Endstufe
public int EvolveLevel;         // 0 = entwickelt sich nicht
```
Bestehende Felder/Verhalten unverändert; `Pokemon`-Konstruktor unberührt.

## 4. Die 3 Monster (Assets in `Assets/Resources/Species/`)

Alle: `Type1 = Grass`, `HasSecondType = false`. Profil: schnell, spezial-tanky, wenig Angriff, hohe SpA/SpV.

| DisplayName | KP | Ang | Vert | SpA | SpV | Init | EvolvesInto | EvolveLevel | Front-Datei | Back-Datei |
|---|---|---|---|---|---|---|---|---|---|---|
| Mossprig   | 50 | 40 | 55 | 65  | 70  | 60 | Briarstag  | 16 | `Mosssprig.png`  | `Mossprig Back.png`  |
| Briarstag  | 70 | 50 | 70 | 90  | 95  | 80 | Elderthorn | 34 | `Briarstag.png`  | `Briarstag Back.png` |
| Elderthorn | 90 | 60 | 85 | 120 | 125 | 95 | (none)     | 0  | `Elderthorn.png` | `Elderthorn Back.png`|

Sprites sind als „Multiple" importiert; jede Datei enthält ein Sub-Sprite `<Dateibasis>_0` (z. B. `Mosssprig_0`, `Briarstag_0`, `Elderthorn_0`; Back analog mit `_0`). Zuweisung per `{guid, spriteName}`. **Erstellreihenfolge** wegen `EvolvesInto`: Elderthorn → Briarstag → Mossprig.

## 5. Sprite-Anzeige im Kampf

`BattleHud` bekommt zwei `Image`s:
- **Gegner-Sprite** (Front) oben, etwa über/neben der Gegner-Infobox.
- **Spieler-Sprite** (Back) unten links, über der eigenen Infobox.
- Quelle: `Pokemon.Species.FrontSprite` / `BackSprite`. Beim Einwechseln aktualisieren. Ist ein Sprite `null`, wird das `Image` ausgeblendet (Layout bleibt stabil).
- `Image.preserveAspect = true`, feste Box (~200×200), `raycastTarget = false`.

## 6. Demo-Aufstellung (`SampleData`)

Damit man die Monster sofort sieht, lädt `SampleData` die Assets via `Resources.Load<SpeciesData>("Species/<Name>")`:
- **Spieler-Team:** Mossprig (+ Briarstag als zweites zum Wechseln).
- **Gegner:** Elderthorn.
- **Platzhalter-Attacken** (bis Welle 2), im Code erzeugt und dem `Pokemon` mitgegeben: „Vine Whip" (Grass, physisch, 45) und „Mega Drain" (Grass, speziell, 40). `SpeciesData.LearnableMoves` bleibt leer.

*(Nur Demo-Paarung; echte Begegnungen über die Map kommen später. Mossprig vs. Elderthorn ist absichtlich asymmetrisch, nur zum Sprite-Schaucheck.)*

## 7. Tests (EditMode)

Neuer Test `SpeciesAssetTests` (im `Battle.Tests`-Assembly, lädt aus Resources):
- die 3 Assets laden per `Resources.Load<SpeciesData>("Species/<Name>")` ≠ null.
- alle `Type1 == Grass`.
- Kette: `Mossprig.EvolvesInto == Briarstag`, `Briarstag.EvolvesInto == Elderthorn`, `Elderthorn.EvolvesInto == null`.
- `EvolveLevel`: Mossprig 16, Briarstag 34, Elderthorn 0.
- `FrontSprite`/`BackSprite` aller drei ≠ null.
- Stat-Stichprobe: `Elderthorn.BaseSpAttack == 120`.

## 8. Dateien

```
Modified:  Assets/Scripts/Battle/Data/SpeciesData.cs   (+4 Felder)
Modified:  Assets/Scripts/Battle/View/BattleHud.cs      (Sprite-Anzeige)
Modified:  Assets/Scripts/Battle/Fixtures/SampleData.cs (Resources.Load, Demo-Team)
Created:   Assets/Resources/Species/Mossprig.asset
Created:   Assets/Resources/Species/Briarstag.asset
Created:   Assets/Resources/Species/Elderthorn.asset
Created:   Assets/Scripts/Battle/Tests/SpeciesAssetTests.cs
```

## 9. Entscheidungen

- Name 3. Stufe: **Elderthorn** (passt zur Sprite-Datei).
- Evolution: **Verweis + Level-Trigger** (Felder gesetzt; Mechanik später).
- Sprites **jetzt** im Kampf, **Mossprig** als Demo-Starter.
- Stats & Level (16/34) wie in der Tabelle, vom Nutzer freigegeben („ist gut so").
