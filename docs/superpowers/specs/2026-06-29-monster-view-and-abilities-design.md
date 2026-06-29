# Monster View + Passive Abilities (110) — Design

**Datum:** 2026-06-29
**Projekt:** MonsterCatcher (Unity 6 `6000.4.3f1`)
**Status:** Freigegeben (Design), Spec zur Umsetzung
**Verwandt:** `2026-06-29-moon-line-dual-type-design.md`, `2026-06-29-leveling-and-enemy-scaling-design.md`, `2026-06-29-move-library-design.md`

---

## 1. Ziel & Umfang

Zwei gekoppelte Einheiten:

- **A) Passiv-Fähigkeiten-System** — 110 Fähigkeiten (70 einfache Buffs, 40 definierende), zufällig auf Monster gerollt (normal 1, Modell erlaubt mehr), **voll funktional im Kampf** über ein Hook-System. Team **und** Gegner rollen.
- **B) Monster-Ansicht (UI)** — Button auf der Map öffnet ein Panel mit der Party; pro Monster: generelle Stats, Pokédex-Hintergrundgeschichte, Moves + Move-Infos, Fähigkeit; plus **Freilassen** (nur bei Party > 1).

Beide in einer Spec, Umsetzung in **zwei Phasen** (Abschnitt 9).

## 2. Assembly-Architektur (keine Zyklen)

- **Map core (pure C#)** bekommt den **Identitäts-Katalog**: `AbilityInfo { Id, Name, Description, AbilityCategory (Buff|Defining), AbilityEffect Effect }` + statischer `AbilityCatalog` (110 Einträge). `AbilityEffect` ist eine **reine Daten-Struktur** (Effekt-Parameter, Abschnitt 4). `RunState` rollt daraus; beide UIs zeigen Name/Beschreibung daraus.
- **Map core** `MonsterSave` bekommt `List<string> AbilityIds` (normal 1 Eintrag).
- **Battle core** liest `AbilityEffect` (Battle→Map besteht bereits) und **wendet die Effekte an** den Hook-Punkten an (Abschnitt 3). `Pokemon` bekommt `IReadOnlyList<string> AbilityIds` + Helfer `Ability(i)`/`HasAbility(id)` und transienten Kampf-Zustand (`AbilityState`: Last-Stand verbraucht, Phoenix verbraucht, Rundenzähler, …).
- **Monster-Ansicht (UI)** lebt in **`Map.View`**, das dafür **`Battle` referenziert** (für `SpeciesData`, `MoveData`, Stat-Formel). Neue Kante `Map.View→Battle`; `Battle→Map` besteht → kein Zyklus (nichts referenziert `Map.View`).
- **Bespoke-Effekte** (wenige, die kein generischer Parameter abdeckt — z. B. Phoenix-Revive, Executioner, Reversal) werden in Battle per `Id`-Schalter behandelt. Ein **Sync-Test** stellt sicher, dass jede `Id` im Katalog vom Battle-Applier abgedeckt ist und umgekehrt.

## 3. Hook-Punkte (Battle-Engine, alles pure C# & testbar)

- `Pokemon.EffectiveStat` / `MaxHp` → Stat-Multiplikatoren.
- `DamageCalculator`: ausgehender Multiplikator (flat + bedingt), eingehender Multiplikator (flat/Typ/Kategorie/super-effektiv/voll-KP/Erst-Treffer/früh), Krit-Chance/-Schaden/-Immunität, **Tinted Lens** (Resistenz → neutral), **Tyrant** (super-eff ×1,5), **Adaptability** (immer STAB), Genauigkeit/Never-Miss.
- `BattleEngine.ExecuteMove`: Drain-auf-alles, Recoil-Immunität/-Bonus, On-Hit-Status (eigener/Angreifer), **Thorns** (Angreifer-Schaden), **Executioner** (Sicher-K.O.).
- `BattleEngine` Reihenfolge (`CompareOrder`/`MovePriority`): Prioritäts-Boni (Status-Moves, Erst-Runde, **Time Warp**, **Reversal**=immer zuletzt), Low-HP-Speed.
- `BattleEngine` End-of-Turn: Heilung pro Runde, Status-Chip-Immunität (Burn/Poison), Rundenzähler (Momentum/Fortified).
- Status-Anwendung (`TryApplyStatus`): Status-Immunität (einzeln/alle), Para-kein-Speed-Cut, Immun gegen Stat-Senkung.
- On-Entry (Ctor + Switch + Forced-Switch): eigene/Gegner-Stat-Stufen, Download.
- On-KO (nach gegnerischem Faint): Moxie. On-Hit-Taken: Steadfast/Rage/Static Body/Thorns. On-Faint (eigener): Aftermath, **Phoenix** (1× Revive), **Last Stand** (1× bei 1 KP überleben), **Second Wind** (1× Heilung <½), **Comeback** (Skalierung mit gefallenen Team-Mitgliedern).

## 4. `AbilityEffect` — Daten-Palette

Eine Struktur mit optionalen Feldern; die meisten Fähigkeiten setzen 1–2. Gruppen: **Stat-Mult** (Atk/SpAtk/Def/SpDef/Speed/HP), **Outgoing** (flat, Typ-Boost, Kategorie-Boost, Low-HP, Full-HP, Turn-1, Foe-Low-HP[+Schwelle], vs-Status, After-Foe, Super-Eff-Bonus, Per-Fainted-Ally, Ramp[+Cap]), **Incoming** (flat, Typ-Resist, Kategorie-Resist, Super-Eff, Full-HP, First-Hit, Early-Turns[+Fenster]), **Krit** (Chance-Mult, Always/Low-HP-Always, Damage-Mult, Immun), **Genauigkeit** (Mult, NeverMiss), **Status** (Immun einzeln/alle, Burn/Poison-kein-Chip, Para-kein-Speed-Cut, Immun-Stat-Senkung, On-Hit-Inflict[+Ziel/Chance]), **Sustain** (Heal/Runde, Drain-alle, Recoil-Immun/-Bonus, One-Time-Heal-<½), **Tempo** (Prio-Bonus, Status-Move-Prio, First-Turn-Prio, Garantiert-Turn-1, Always-Last[+Mult], Low-HP-Speed), **On-Entry** (Self/Foe-Stat-Stufen, Download), **Reaktiv** (On-KO-Self, On-Hit-Self, Thorns, Aftermath, Revive[+Frac], SurviveLethal), **Offensiv-Spezial** (AllMovesStab, TintedLens, ExecuteThreshold, SuppressFoeBoosts).

## 5. Die 110 Fähigkeiten

**Buffs 1–15:** Brawler +12% Atk · Mystic +12% SpAtk · Bulwark +15% Def · Warden +15% SpDef · Fleetfoot +15% Spe · Stalwart +10% HP · Keen ×3 Krit-Chance · Thickhide −12% Schaden · Bruiser +10% Schaden · Regrowth Heal 6%/Runde · Heatproof −50% Feuer · Limber immun Paralyse · Stoic immun Burn · Wide Awake immun Schlaf · Antibody immun Gift.

**Definierend 16–30:** Deadeye nie daneben · Last Stand 1× K.O. bei 1 KP überleben · Intimidate Entry Foe-Atk −1 · Battle Cry Entry Self-Atk +1 · Overclock Entry Self-SpAtk +1 · Sniper Krit ×2,25 · Berserk <⅓ KP +50% Schaden · Adrenaline <⅓ KP +50% Speed · Shellguard super-eff erhalten −25% · Cushion −30% Spezial · Anvil −30% physisch · Trickster Status-Moves +1 Prio · Reckless Recoil-frei +20% · Siphon Moves heilen 15% · Venomtouch 30% Gift bei Treffer.

**Buffs 31–45:** Vigor +8% Atk/SpAtk · Turtle +10% Def/SpDef · Hawk Eye +20% Genauigkeit · Pyromaniac Feuer +20% · Galvanize Elektro +20% · Naturalist Pflanze +20% · Moonblessed Fairy +20% · Toughness voll-KP −10% erhalten · Featherfall erster Treffer −20% · Scales −10% phys & spez · Bloom Heal 4% + immun Gift · Cozy immun Burn + −25% Feuer · Lucky Charm +50% Sekundär-Chancen · Nimble +10% Spe & +10% Genauigkeit · Second Wind 1× Heal 10% bei <½.

**Definierend 46–60:** Moxie K.O.→Atk +1 · Download Entry höhere Offensive +1 · Steadfast getroffen→Spe +1 · Rage getroffen→Atk +1 · Multiscale voll-KP −50% · Titan STAB ×2 · Unbreakable krit-immun · Momentum +10%/Runde (max +40%) · Glass Cannon +30% aus/+15% ein · Bully vs Status +30% · Avenger <¼ KP immer Krit · Thorns Angreifer −1/8 · Static Body 30% Para beim Angreifer · Phantom Step Erst-Runde +1 Prio · Aftermath Faint→Gegner −¼.

**Buffs 61–100:** Powerhouse +20% Atk · Archmage +20% SpAtk · Fortress +25% Def · Aegis +25% SpDef · Sprinter +25% Spe · Giant +20% HP · Balanced +8% alle · Twin Strike +10% Atk/SpAtk · Stonewall +12% Def/SpDef · Duelist +15% Spe/+10% Atk · Iron Fist phys +12% · Mystic Surge spez +12% · Scrappy Normal +20% · Aquatic Wasser +20% · Frostbite Eis +20% · Brawl Kampf +20% · Venomous Gift +20% · Nightfall Unlicht +20% · Waterproof −50% Wasser · Grounded −50% Elektro · Frostward −50% Eis · Shade −50% Unlicht · Plated −15% phys · Veiled −15% spez · Mending Heal 4%/Runde · Vampiric Moves heilen 8% · Opportunist Foe <½ +15% · Finisher Foe <¼ +25% · Early Bird Turn 1 +20% · Lucky Strike ×2 Krit-Chance · Focused Aim +15% Genauigkeit · Fever Ward Burn kein Chip · Shake It Off Para kein Speed-Cut · Hardy Mind immun Stat-Senkung · Warm Up Entry Spe +1 · Guard Up Entry Def +1 · Brace Entry SpDef +1 · Counterpunch nach Foe +15% · Vanguard voll-KP +10% · Resolute −10% aller Schaden.

**Definierend 101–110:** Reversal immer zuletzt, +40% · Adaptability alle Moves STAB · Tinted Lens nicht-sehr-effektiv→neutral · Tyrant super-eff zusätzlich ×1,5 · Executioner Foe <20% sicher K.O. · Comeback +15% je gefallenem Team-Mitglied · Phoenix 1× Revive 33% · Time Warp garantiert Turn-1 zuerst · Disruptor Gegner-Boosts ignoriert · Fortified erste 2 Runden −40%.

Gegner rollen aus demselben Pool (symmetrisch); bei Bedarf später auf Buffs beschränkbar (eine `Where`-Filterzeile).

## 6. Rollen, Persistenz, Entwicklung

- **Starter:** `RunState.NewRun` würfelt 1 `AbilityInfo.Id` und legt sie in `MonsterSave.AbilityIds`.
- **Gegner:** `BattleController.BuildEnemy` würfelt 1 Id (transient für den Kampf).
- **Persistenz:** bleibt im `MonsterSave` über den Run; **Entwicklung behält** die Fähigkeit.
- Deterministisch testbar über `IRng`/Seed.

## 7. Monster-Ansicht (UI)

`MapController` bekommt einen **Button** ("Monsters"). Klick öffnet ein **Overlay** (Runtime-uGUI, `MonsterView` in `Map.View`): links Party-Liste (Name + Level), rechts Detail des gewählten:

- Kopf: `FrontSprite`, Name, Typ(en) (inkl. Doppel-Typ), Level.
- **Story:** neues Feld `SpeciesData.LoreText` (`[TextArea]`); Pokédex-Stil, 1–2 Sätze für alle 12 Monster (Abschnitt 8).
- **Stats:** berechnete 6 Werte auf aktuellem Level (HP/Atk/Def/SpAtk/SpDef/Spe) via Stat-Formel.
- **Moves:** `species.MovesAtLevel(level)`; pro Move Typ, Kategorie, Stärke, Genauigkeit, Kurz-Effekt.
- **Fähigkeit:** Name + Beschreibung aus `AbilityCatalog` (mehrere möglich).
- **Freilassen:** Button, aktiv nur bei Party > 1, mit Ja/Nein-Bestätigung → `RunState.ReleaseMonster(index)` entfernt aus `PlayerRoster`. *(Hinweis: aktuell startet man mit 1 Monster; Freilassen wird relevant, sobald ein 2./Fang-System existiert — Mechanik wird trotzdem jetzt gebaut.)*

## 8. Lore-Texte (12, Pokédex-Stil)

Mossprig: "A sprout-tailed forest sprite that bonds with mossy roots to sense danger long before it arrives." · Briarstag: "Its thorned antlers brew healing sap from sunlight; herds are said to regrow whole groves overnight." · Elderthorn: "An ancient grove-guardian whose bark records centuries of seasons and whose roots quiet any storm." · Cindrop: "A living ember that never cools, hopping between dry leaves and leaving tiny scorch-prints." · Magmelt: "Molten veins glow brighter as it grows angry; it melts footholds straight into solid rock." · Vulcarion: "A walking eruption — when its core flares, distant skies turn red with falling ash." · Voltwig: "A bristly spark-pup; static clings to its fur and snaps at anything that comes too close." · Stormbark: "It gallops ahead of thunderstorms, daring the lightning to keep pace." · Tempestag: "Its antlers split the sky; old sailors named the worst gales after this beast." · Lunakit: "A moon-touched kit that slips between shadows and the soft light of dusk." · Moonlynx: "Eyes like twin moons see through any deception; it hunts only under a clouded sky." · Eclipseon: "When it crosses the moon, night swallows the land — few have met its gaze and remembered."

## 9. Phasen

- **Phase 1 — Abilities:** `AbilityCatalog`/`AbilityInfo`/`AbilityEffect` (Map core), `MonsterSave.AbilityIds`, `Pokemon.AbilityIds`+`AbilityState`, Engine-Hooks + `AbilityApplier` (Battle core), Roll in `NewRun`/`BuildEnemy`. **Tests** (s. u.).
- **Phase 2 — Monster-Ansicht:** `SpeciesData.LoreText` + 12 Lore-Texte (MCP-Modify), `RunState.ReleaseMonster`, `MonsterView` in `Map.View` (+ `Map.View→Battle`), Map-Button. **Tests** für Release + Stat/Move-Anzeige-Helfer.

## 10. Tests

- **Sync:** jede Katalog-Id hat ein definiertes Verhalten; Buff/Defining-Zählung = 70/40; 110 gesamt.
- **Effekt-Beispiele (repräsentativ, ~1 je Hook-Typ):** Stat-Mult (Brawler erhöht Effektiv-Atk), Outgoing (Bruiser), Incoming/Typ-Resist (Heatproof halbiert Feuer), Krit (Keen erhöht Rate; Unbreakable verhindert), NeverMiss (Deadeye trifft immer), Status-Immun (Limber), Heal/Runde (Regrowth), Prio (Trickster Status-Move zuerst), Survive-Lethal (Last Stand 1 KP), On-Entry (Intimidate senkt Foe-Atk), On-KO (Moxie), Multiscale (voll-KP −50%), Executioner (Foe <20% K.O.), Phoenix (1× Revive), Aftermath (Faint→Foe-Schaden).
- **Roll/Persistenz:** Starter erhält genau 1 Id; bleibt über `NextTier`/Entwicklung; Gegner erhält 1 Id.
- **Release:** entfernt nur bei Count > 1; pinnt Indizes korrekt.
- Bestehende Suite bleibt grün (aktuell 90).

## 11. Dateien (geplant)

```
Map core:    Ability.cs (AbilityInfo/AbilityEffect/AbilityCategory), AbilityCatalog.cs;
             RunState.cs (Roll + ReleaseMonster), MonsterSave.AbilityIds
Battle core: Pokemon.cs (AbilityIds + AbilityState + Helfer), AbilityApplier.cs,
             DamageCalculator.cs/BattleEngine.cs (Hooks), BattleController.cs (Enemy-Roll)
Battle data: SpeciesData.cs (LoreText)
Map.View:    MonsterView.cs (+ asmdef refs Battle), MapController.cs (Button)
Resources:   Species/*.asset (LoreText via MCP)
Tests:       Battle/Tests/AbilityTests.cs (+ Sync), Map/Tests/ReleaseTests.cs
```

## 12. Entscheidungen

- Datengetriebene `AbilityEffect`-Palette deckt die Masse; wenige Sonderfälle per Id-Schalter.
- Identität in Map core (Roll + Anzeige), Verhalten in Battle core (Hooks) — Sync-Test gegen Drift.
- Team **und** Gegner rollen; definierende Gegner-Effekte später filterbar.
- Monster-Ansicht in `Map.View` (referenziert `Battle`); Freilassen jetzt gebaut, voll nutzbar mit künftigem Fang-/2.-Monster-System.
- 110 Fähigkeiten als Roll-Pool — einige situativ/zukunftssicher (Typ-Boni/-Resists), bewusst akzeptiert.
