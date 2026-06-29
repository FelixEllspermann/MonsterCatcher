# Pokémon-Kampfsystem — Design (Grundsystem)

**Datum:** 2026-06-29
**Projekt:** MonsterCatcher (Unity 6 `6000.4.3f1`, 2D, URP, neues Input System)
**Status:** Entwurf zur Freigabe

---

## 1. Ziel & Umfang

Ein originalgetreues, rundenbasiertes **Einzelkampf**-System im Pokémon-Stil:

- Werte: KP, Angriff, Verteidigung, Spez.-Angriff, Spez.-Verteidigung, Initiative
- Attacken mit Typ, Kategorie (Physisch/Speziell/Status), Stärke, Genauigkeit, AP, Priorität, Effekt
- Vollständige **18-Typen-Effektivitätstabelle** (0 / ½ / 1 / 2)
- **Originalgetreue Schadensformel** inkl. STAB, Typeneffektivität, Volltreffer, Zufallsfaktor (85–100 %), Brand-Malus
- **Statusprobleme:** Gift, Verbrennung, Paralyse, Schlaf
- **Stat-Stufen** (−6..+6) für Angriff/Vert./SpA/SpV/Init
- **Team & Wechsel:** konfigurierbare Teamgröße (Standard 6), Einwechseln im Kampf, Zwangswechsel bei K.O.
- **Spieler gegen einfache KI**, die Attacken wählt **und** wechseln kann
- **Sieg**, wenn ein Team vollständig K.O. ist

## 2. Nicht-Ziele (bewusst erst später)

Fähigkeiten (Abilities), getragene Items, Wetter/Terrain, Mehrfach-/Mehrrunden-Attacken, Genauigkeits-/Fluchtwert-Stufen, Doppelkämpfe, **Fangen** (trotz Projektname zunächst nicht im Kampfkern), Einfrieren. IV/EV/Wesen sind in den Formeln **strukturell vorgesehen**, aber vorerst auf 0/0/neutral gesetzt.

## 3. Architekturüberblick

Datengetrieben mit **ScriptableObjects** für Inhalte + **reine C#-Engine** ohne `UnityEngine`-Abhängigkeit im Kern.

```
Eingabe/UI (MonoBehaviour)  ─┐
                             ├─►  BattleEngine (reines C#)  ──►  Liste von BattleEvents  ──►  UI/Log
Einfache KI (C#)            ─┘         │
                                      └─ liest SpeciesData / MoveData / TypeChart (Daten)
```

Vorteil: Die gesamte Kampflogik ist in **EditMode-Tests** prüfbar, ohne dass Editor oder MCP laufen müssen. Die MonoBehaviour-Schicht ist dünn und nur für Input/Anzeige zuständig.

## 4. Projektstruktur

```
Assets/Scripts/Battle/
  Battle.asmdef
  Core/
    ElementType.cs          (enum, 18 Typen)
    MoveCategory.cs         (enum: Physical, Special, Status)
    StatusCondition.cs      (enum: None, Poison, Burn, Paralysis, Sleep)
    Stat.cs                 (enum: Hp, Attack, Defense, SpAttack, SpDefense, Speed)
    Pokemon.cs              (Runtime-Instanz eines Kämpfers)
    Party.cs                (Team mit konfigurierbarer Maximalgröße)
    BattleAction.cs         (Move | Switch)
    BattleEvent.cs          (Ereignis-Typen für UI/Log)
    BattleEngine.cs         (Rundenauflösung, reine Logik)
    DamageCalculator.cs     (Schadensformel)
    StatStages.cs           (Multiplikator-Tabelle)
  Data/
    SpeciesData.cs          (ScriptableObject)
    MoveData.cs             (ScriptableObject)
    TypeChart.cs            (ScriptableObject + Effektivitäts-Lookup)
    BattleSettings.cs       (ScriptableObject: Teamgröße, Status-Brüche, Crit-Chance …)
  Control/
    BattleController.cs     (MonoBehaviour: Brücke Engine ↔ Input/UI)
    SimpleAI.cs             (Aktionswahl der KI inkl. Wechsel)
  Fixtures/
    SampleData.cs           (code-erzeugte Pokémon/Attacken für Tests & Schnellstart)

Assets/Scripts/Battle/Tests/
  Battle.Tests.asmdef       (EditMode, referenziert Battle.asmdef)
  DamageTests.cs
  TypeChartTests.cs
  TurnOrderTests.cs
  StatusTests.cs
  BattleFlowTests.cs
```

Eigene Assembly Definitions halten Kompilierzeiten klein und trennen Tests sauber.

## 5. Enums & Kernbegriffe

```csharp
public enum ElementType { Normal, Fire, Water, Electric, Grass, Ice, Fighting, Poison,
                          Ground, Flying, Psychic, Bug, Rock, Ghost, Dragon, Dark, Steel, Fairy }
public enum MoveCategory { Physical, Special, Status }
public enum StatusCondition { None, Poison, Burn, Paralysis, Sleep }
public enum Stat { Hp, Attack, Defense, SpAttack, SpDefense, Speed }
```

## 6. Datenmodell

**SpeciesData (SO):** `DisplayName`, `ElementType Type1`, `ElementType Type2` (optional), Basiswerte je `Stat`, Liste erlernbarer `MoveData`.

**MoveData (SO):** `DisplayName`, `ElementType Type`, `MoveCategory Category`, `int Power`, `int Accuracy` (0–100; 0 = trifft immer), `int MaxPp`, `int Priority` (Standard 0), optionaler Effekt:
- `StatusCondition InflictsStatus` + `int StatusChance` (%)
- `Stat StatToChange` + `int Stages` (+/-) + `bool TargetsSelf` + `int StatChangeChance` (%)

**Pokemon (Runtime):** Referenz auf `SpeciesData`, `int Level` (Standard 50), `int CurrentHp`, berechnete Maximalwerte, `StatusCondition Status`, `int SleepTurnsLeft`, Stat-Stufen je Stat (−6..+6), Liste `MoveSlot` (`MoveData` + aktuelle AP). Hilfsmethoden: `IsFainted`, `TakeDamage`, `Heal`, `ApplyStatus`, `ChangeStatStage`.

**Party (Runtime):** Liste von `Pokemon`, `MaxSize` (aus `BattleSettings`), `ActiveIndex`. Methoden: `Active`, `HasUsablePokemon`, `CanSwitchTo(index)`.

**BattleSettings (SO):** zentral konfigurierbar — `MaxPartySize` (Standard 6), `PoisonFraction` (1/8), `BurnFraction` (1/16), `BurnAttackMultiplier` (0.5), `ParalysisSpeedMultiplier` (0.5), `ParalysisFailChance` (25 %), `CritChance` (1/24), `CritMultiplier` (1.5), `MinSleepTurns`/`MaxSleepTurns` (1/3). So lässt sich die Teamgröße (und Balancing) „manipulieren“, ohne Code zu ändern.

## 7. Werteberechnung (Stats)

Originalgetreue Formel (Gen III+), IV/EV/Wesen vorerst auf 0/0/1.0:

```
KP   = floor((2·Basis + IV + floor(EV/4)) · Level / 100) + Level + 10
Wert = floor( (floor((2·Basis + IV + floor(EV/4)) · Level / 100) + 5) · Wesen )
```

Struktur erlaubt späteres Einsetzen echter IV/EV/Wesen ohne Formeländerung.

## 8. Typentabelle

`TypeChart` liefert `float Effectiveness(ElementType atk, ElementType def)` ∈ {0, 0.5, 1, 2}. Gegen Doppeltypen wird multipliziert (z. B. 2·2 = 4×, 0.5·0 = 0). Hinterlegt wird die **kanonische 18×18-Tabelle** (inkl. Immunitäten wie Normal/Kampf → Geist = 0, Boden → Flug = 0, Gift → Stahl = 0). Implementierung als kompakte Map der von 1.0 abweichenden Relationen; alles andere ist neutral.

## 9. Schadensformel

```
base   = floor( floor( (floor(2·Level/5) + 2) · Stärke · A / D ) / 50 ) + 2
Schaden = floor( base · STAB · Typ · Crit · Zufall · Brand )
```

- **A/D:** Angriff/Verteidigung bei physisch, SpA/SpV bei speziell — **inklusive Stat-Stufen**.
- **STAB:** 1.5, wenn Attackentyp einem Typ des Anwenders entspricht, sonst 1.0.
- **Typ:** Produkt der Effektivität gegen beide Verteidigertypen (kann 0 sein → kein Schaden, „wirkungslos“).
- **Crit:** Chance `CritChance` (1/24); bei Volltreffer × `CritMultiplier` (1.5) und negative Angriffsstufen des Anwenders / positive Verteidigungsstufen des Ziels werden ignoriert.
- **Zufall:** ganzzahlig 85–100, geteilt durch 100.
- **Brand:** 0.5, wenn der Anwender verbrannt ist **und** die Attacke physisch ist, sonst 1.0.
- **Genauigkeit:** Vor der Schadensberechnung Trefferwurf gegen `Accuracy` (0 = trifft immer). Status-Attacken (`Power = 0`) machen keinen Schaden, wenden nur Effekte an.

## 10. Statuseffekte

| Status | Wirkung |
|---|---|
| **Gift** | Am Rundenende `PoisonFraction` (1/8) der max. KP Schaden. |
| **Verbrennung** | Am Rundenende `BurnFraction` (1/16) Schaden; physischer Schaden des Trägers ×0.5. |
| **Paralyse** | Initiative × `ParalysisSpeedMultiplier` (0.5); pro Aktion `ParalysisFailChance` (25 %) Chance auszusetzen. |
| **Schlaf** | Kämpfer setzt 1–3 Runden aus (`SleepTurnsLeft`, zufällig gesetzt); zählt pro Aktionsversuch herunter, wacht danach auf. |

Ein Pokémon hat höchstens **einen** nicht-flüchtigen Status gleichzeitig. Status bleibt beim Auswechseln erhalten.

## 11. Stat-Stufen

Stufen −6..+6 je Stat. Multiplikatoren (Kampfwerte Ang/Vert/SpA/SpV/Init):

```
Stufe:   -6    -5    -4    -3    -2    -1    0    +1    +2    +3    +4    +5    +6
Faktor: 0.25  2/7   1/3   0.4   0.5   2/3   1.0  1.5   2.0   2.5   3.0   3.5   4.0
```

(Formel: `+n → (2+n)/2`, `-n → 2/(2+n)`.) Genauigkeits-/Fluchtwert-Stufen sind Nicht-Ziel.

## 12. Rundenablauf

1. **Aktionswahl:** Beide Seiten wählen eine `BattleAction` — `Move(slot)` oder `Switch(index)`. (Spieler über UI, Gegner über `SimpleAI`.)
2. **Reihenfolge:**
   - **Wechsel zuerst** (vor allen Attacken).
   - Sonst nach **Priorität** der Attacke, bei Gleichstand nach **effektiver Initiative** (inkl. Paralyse-Malus); bei exaktem Gleichstand Zufall.
3. **Ausführung je Aktion:**
   - Schlaf/Paralyse-Prüfung (ggf. Aussetzen, Schlafzähler).
   - Trefferwurf (Genauigkeit).
   - Schadens-Attacke → `DamageCalculator`; Status-Attacke → Effekt anwenden.
   - Zusatzeffekte (Status zufügen / Stat-Stufen ändern) gemäß Chance.
   - K.O.-Prüfung des Ziels.
4. **Rundenende:** Status-Schaden (Gift/Brand) für beide aktiven Kämpfer; K.O.-Prüfung.
5. **K.O.-Behandlung:** Ist der aktive Kämpfer K.O., **Zwangswechsel** zum nächsten einsatzfähigen Pokémon (Spieler wählt, KI per Heuristik).
6. **Siegprüfung:** Hat ein Team kein einsatzfähiges Pokémon mehr → Kampfende, anderes Team gewinnt.

Jeder relevante Schritt erzeugt ein **BattleEvent** (siehe §15), sodass UI/Log rein darauf reagieren.

## 13. Team / Party (konfigurierbare Größe)

`Party.MaxSize` stammt aus `BattleSettings.MaxPartySize` (Standard 6). Die Größe ist damit zentral einstellbar (Inspector) und pro Kampf überschreibbar — z. B. für 1‑gegen‑1‑Duelle oder größere Teams. Wechsel ist erlaubt auf jedes nicht-K.O.-Pokémon, das nicht bereits aktiv ist.

## 14. KI (inkl. Wechsel)

`SimpleAI.ChooseAction(self, opponent)` Heuristik:

1. **Wechsel-Erwägung zuerst:** Ist das aktive Pokémon stark im Nachteil — aktive Attacke des Gegners (bzw. Gegner-Typen) erzielt ≥ 2× Effektivität **und** eigene KP niedrig (z. B. < 30 %) — und gibt es auf der Bank ein Pokémon mit klar besserem Typ-Matchup, dann **wechseln**.
2. **Sonst Attacke:** wähle unter Attacken mit AP die mit **höchstem erwartetem Schaden** (Stärke × Typeneffektivität × STAB), Status-Attacken nachrangig.
3. Schwellen (KP-Grenze, Effektivitäts-Grenze) liegen als Konstanten in `SimpleAI` und sind leicht justierbar.

Bewusst einfach und deterministisch testbar; später erweiterbar.

## 15. BattleEvents (für UI/Log)

Eine kleine Hierarchie/Union beschreibt, was passiert ist — damit UI und Kampf-Log entkoppelt bleiben:

```
MoveUsed(user, move)        Missed(user, move)
DamageDealt(target, amount, effectiveness, wasCrit)
StatusInflicted(target, status)    StatusDamage(target, status, amount)
StatChanged(target, stat, deltaStages)
Fainted(pokemon)            SwitchedIn(party, pokemon)
ActionPrevented(pokemon, reason)   // schläft / paralysiert
BattleEnded(winningSide)
```

`BattleEngine.ExecuteTurn(...)` gibt die Ereignisse **geordnet** zurück; die UI spielt sie sequenziell ab (Animationen/Textboxen).

## 16. Öffentliche API (Skizze)

```csharp
var engine = new BattleEngine(playerParty, enemyParty, settings, rng);
IReadOnlyList<BattleEvent> events = engine.ExecuteTurn(playerAction, enemyAction);
bool over = engine.IsOver;          // BattleResult { InProgress, PlayerWon, EnemyWon }
// Bei K.O. fordert die Engine über ein Event einen Wechsel an;
// der Controller liefert die Wahl, danach läuft ExecuteTurn weiter.
```

Der Zufall (`rng`) wird **injiziert** (`System.Random` oder ein `IRandom`), damit Tests deterministisch sind.

## 17. Tests (EditMode)

Fixtures werden **im Code** gebaut (`SampleData`), daher laufen die Tests ohne Editor/Assets:

- **DamageTests:** Referenzwerte der Formel bei festem RNG; STAB/Crit/Brand-Faktoren; „wirkungslos“ (Effektivität 0).
- **TypeChartTests:** Stichproben aller Effektivitätsklassen inkl. Doppeltyp-Multiplikation und Immunitäten.
- **TurnOrderTests:** Initiative-Reihenfolge, Priorität schlägt Initiative, Paralyse-Malus, Wechsel-vor-Attacke.
- **StatusTests:** Gift/Brand-Schaden pro Runde, Brand halbiert physisch, Paralyse-Aussetzen (fester RNG), Schlafdauer & Aufwachen.
- **BattleFlowTests:** K.O. → Zwangswechsel, Siegbedingung, Stat-Stufen verändern Schaden korrekt.

## 18. Lieferung: jetzt vs. nach MCP-Neustart

**Jetzt (reine Dateien, ohne Editor):**
- Alle C#-Skripte: Enums, Datenmodell-SO-Klassen, `BattleEngine`, `DamageCalculator`, `SimpleAI`, `BattleController`, `Fixtures/SampleData`.
- Beide Assembly Definitions + EditMode-Tests.

**Nach MCP-Neustart (über Unity-MCP):**
- `SpeciesData`/`MoveData`/`TypeChart`/`BattleSettings`-**Assets** anlegen und befüllen.
- Kampf-Szene aus dem 2D-Template + einfache UI (KP-Balken, Attacken-Buttons, Kampf-Log) verdrahten; `BattleController` an Eingabe/UI binden.
- Tests im Test-Runner ausführen, Konsole auf Kompilierfehler prüfen, per Screenshot verifizieren.

## 19. Offene Punkte

- Konkrete Start-Inhalte (welche/wie viele Pokémon & Attacken als Erst-Roster) — Vorschlag: ~6 Pokémon über mehrere Typen, ~12 Attacken; final beim Anlegen der Assets.
- Genaue Status-Brüche sind als `BattleSettings`-Werte gesetzt (gängige Werte) und beim Balancing anpassbar.
