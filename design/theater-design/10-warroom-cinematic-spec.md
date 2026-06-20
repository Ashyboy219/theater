# THEATER — National Command / War Room Dashboard — BUILD SPEC

Single source of truth for replacing the maroon `WARROOM_ROOT` modal with the full-screen "NATIONAL COMMAND / EMERGENCY WAR ROOM" cinematic dashboard. Implement top-to-bottom; nothing here needs re-deriving.

**Files touched**
- `mods/theater/chrome/ingame-warroom.yaml` — full rewrite (layout).
- `OpenRA.Mods.Common/Widgets/Logic/Ingame/WarRoomLogic.cs` — full rewrite (bindings).
- `OpenRA.Mods.Common/Traits/Player/StrategicUpgrades.cs` — **NEW** trait (cash sink + condition broadcast).
- `mods/theater/chrome.yaml` — add 5 art collections.
- `mods/theater/fluent/theater.ftl` — add ~60 fluent keys (append).
- `mods/theater/rules/theater-command.yaml` — add `StrategicUpgrades:` to `Player:`; add `cmd-defensive` / `cmd-assault` doctrine conditions + gated multipliers.
- `mods/theater/rules/theater-structures.yaml` — add consumers + support powers + effect multipliers to THCOM/FACT/PROC/units.
- `design/art/generate_warroom_art.py` — **NEW** batch art driver.

**Hard invariants (do not violate)**
- Every chrome `Image:` PNG is **power-of-two** (256/512/1024). Sub-icons are POT-atlas Regions.
- Every widget `@Id` is **globally unique across all ChromeLayout files** → prefix everything `WR_`.
- The window opens via `Ui.OpenWindow("WARROOM_ROOT", new WidgetArgs { { "world", self.World } })` — ctor gets **World only**, no player. Resolve `world.LocalPlayer`; it is **null** for spectators — guard everywhere.
- **No sim-state mutation from ChromeLogic.** Cash spends go through a synced `Order` → `IResolveOrder` trait that calls `PlayerResources.TakeCash`. ChromeLogic may only READ.
- Layout substitution vars: `WINDOW_WIDTH`, `WINDOW_HEIGHT`, `PARENT_WIDTH`, `PARENT_HEIGHT`, self `WIDTH`/`HEIGHT`. No `PARENT_RIGHT`/`PARENT_BOTTOM`. Anchor right/bottom arithmetically.
- `Background:` on a `BackgroundWidget`/`ButtonWidget` must name a collection with a `PanelRegion:` (stretchable 9-slice). `Image`/icon lookups need a `Regions:` entry. Different code paths.

---

## 1. SCOPE & FIDELITY

Legend — **REAL**: bound to live engine data. **PROXY**: derived from real data, honestly labelled (not a fabricated constant). **FLAVOR**: static/cosmetic content, no live source, intentionally fixed. **CUT**: drop from v1.

| Element | Decision | Data source / reason | Widget | Art asset |
|---|---|---|---|---|
| "THEATER COMMAND" label | FLAVOR | static string | `Label@WR_BRAND` | `warroom-crest:crest` |
| 4-star rating | PROXY | `stats.Experience` thresholds → star count (8000/4000/1500). If no `PlayerStatistics`, fixed 4. | `Label@WR_STARS` (text "★★★★") | — |
| Title "NATIONAL COMMAND" | FLAVOR | static | `Label@WR_TITLE` Font BigBold | — |
| Subtitle "EMERGENCY WAR ROOM" | FLAVOR | static | `Label@WR_SUBTITLE` | — |
| "UNDER ATTACK" badge | **REAL** | `UnderAttack()` = `FindActorsInCircle` around `BaseBuilding` actors, enemy + `AttackBaseInfo` + `CanBeViewedByPlayer`. Badge `IsVisible`. | `ColorBlock@WR_ALERT_BG` + `Label@WR_ALERT` | — |
| "DEFCON LEVEL n" badge | PROXY | `ThreatCount()` → DEFCON map (0→5,≤2→4,≤5→3,≤10→2,>10→1). Honest derived metric. | `ColorBlock@WR_DEFCON_BG` + `Label@WR_DEFCON` | — |
| Credits readout ($12,450) | **REAL** | `res.GetCashAndResources()` | `Label@WR_TOP_CREDITS` | `warroom-icons:res-credits` |
| Power readout (1,850) | **REAL** | `power.PowerDrained + " / " + power.PowerProvided` | `Label@WR_TOP_POWER` | `warroom-icons:res-power` |
| Supplies/storage (72/100) | PROXY | `res.Resources + " / " + res.ResourceCapacity` (silo storage; div-zero guard) | `Label@WR_TOP_SUPPLIES` | `warroom-icons:res-supplies` |
| Hamburger menu | FLAVOR→reuse | opens in-game menu | `Button@WR_MENU` → `Ui.OpenWindow("INGAME_MENU"...)` or CUT to bottom bar | — |
| BATTLEFIELD STATUS minimap | **REAL** | `RadarWidget`, `radar.IsEnabled = () => true` | `Radar@WR_MINIMAP` | — |
| Threat arrows on minimap | CUT (v1) | `RadarWidget` can't draw custom markers; RadarPings is heavy. Defer. | — | — |
| ENEMY FORCE — vector (NORTHWEST) | PROXY | `ThreatVector()` = compass octant of mean enemy `CenterPosition` delta vs base centroid | `Label@WR_ENEMY_VECTOR` | — |
| ENEMY FORCE — ETA (08:35) | CUT (v1) | no global unit ETA; faking it is dishonest. Replace row with threat **count** ("N hostiles sighted"). | `Label@WR_ENEMY_ETA` (repurposed) | — |
| ENEMY FORCE — composition list | PROXY | group detected enemies by domain bucket (Armor/Mech/Air/Naval) | `Label@WR_ENEMY_COMP_*` | — |
| FRONTLINE sectors (NW/W/S/E status) | PROXY | bucket enemy actors by octant around base centroid → CRITICAL/CONTESTED/STABLE/SECURE thresholds | `Label@WR_SECTOR_*` + `_STATUS` | — |
| "VIEW DETAILED MAP" button | FLAVOR | `Ui.CloseWindow` (return to battlefield + open full radar) — same as RETURN | `Button@WR_VIEWMAP` | — |
| Center president portrait | FLAVOR | full-bleed art | `Image@WR_PORTRAIT` | `warroom-president:background` |
| Speech bubble | PROXY | `SpeechLine()` switches on `UnderAttack()`/`ThreatVector()`/doctrine | `Background@WR_SPEECH_BG` + `Label@WR_SPEECH` | — |
| Nameplate name | FLAVOR | "PRESIDENT ANDREW HAWKINS" static (or `player.ResolvedPlayerName`) | `Label@WR_NAME` | — |
| Nameplate role | FLAVOR | "COMMANDER IN CHIEF" static | `Label@WR_ROLE` | — |
| Faction line | **REAL** | `FluentProvider.GetMessage(player.Faction.Name)` ← **bug fix** | `Label@WR_FACTION` | — |
| "IMMEDIATE DECISIONS REQUIRED" banner | FLAVOR | static | `Label@WR_DECIDE_HDR` | — |
| Action card: DEPLOY RESERVES | **REAL** | issues `ParatroopersPower` order `TheaterParadrop` (already on THCOM) | `Button@WR_ACT_RESERVES` | `warroom-cards`* / reuse |
| Action card: AIR SUPPORT | **REAL** (needs YAML) | new `AirstrikePower@AIRSUPPORT` on THCOM, order `AuthorizeAirSupport` | `Button@WR_ACT_AIR` | — |
| Action card: ECONOMIC SHIFT | **REAL** (needs trait) | `StrategicUpgrades` timed row `upg-econshift` → `CashTricklerMultiplier@ECON` on PROC | `Button@WR_ACT_ECON` | — |
| Action card: NATIONAL ADDRESS | **REAL** (needs trait) | `StrategicUpgrades` timed row `upg-address` → army-wide `Firepower`+`Speed` morale | `Button@WR_ACT_ADDRESS` | — |
| NATIONAL RESOURCES — Credits value | **REAL** | `res.GetCashAndResources()` | `Label@WR_RES_CREDITS` | `warroom-icons:res-credits` |
| Credits delta (+2,350/min) | **REAL/PROXY** | `stats.DisplayIncome` (rolling ~60s window ≈ /min). If no stats: Earned-delta tracker in Tick. | `Label@WR_RES_CREDITS_DELTA` | — |
| Power value + delta | **REAL** / FLAVOR | value real; per-min delta has no source → show `ExcessPower` "headroom" instead, no fake delta | `Label@WR_RES_POWER` (+`_DELTA` as headroom) | `warroom-icons:res-power` |
| Supplies value + delta | PROXY | `res.Resources`; delta CUT (no source) | `Label@WR_RES_SUPPLIES` | `warroom-icons:res-supplies` |
| Population value + delta | PROXY | live owned-unit count (Mobile+Selectable). Drop the `/100` cap (no supply system). | `Label@WR_RES_POP` | `warroom-icons:res-pop` |
| MILITARY — Vehicles (142) | **REAL** | `ActorsHavingTrait<Mobile>` + `GetEnabledTargetTypes().Contains("Vehicle")` & not "Ship" | `Label@WR_MIL_VEH` | `warroom-domains:vehicle` |
| MILITARY — Infantry (1,248) | **REAL** | `Mobile` + target type `"Infantry"` | `Label@WR_MIL_INF` | `warroom-domains:infantry` |
| MILITARY — Aircraft (64) | **REAL** | `ActorsHavingTrait<Aircraft>` count | `Label@WR_MIL_AIR` | `warroom-domains:aircraft` |
| MILITARY — Naval (18) | **REAL** | `Mobile` + target type `"Ship"` (also "WaterActor" for subs) | `Label@WR_MIL_NAV` | `warroom-domains:naval` |
| ACTIVE EFFECTS — names + state | **REAL** | `army.Doctrine` condition + `StrategicUpgrades` active mask → ACTIVE/INACTIVE | `Label@WR_FX_*` + `_STATE` | — |
| ACTIVE EFFECTS — countdown timers | **REAL** (only timed rows) | `StrategicUpgrades` exposes `RemainingTicks(i)`; format `mm:ss` via `world.Timestep`. Permanent rows show "ACTIVE", no timer. | `Label@WR_FX_*_TIMER` driven by `LogicTicker@WR_TICKER` | — |
| WAR DOCTRINES — Focus Armor | **REAL** | doctrine 2 (`cmd-focus-armor`) | `Button@WR_DOC_ARMOR` | — |
| WAR DOCTRINES — Focus Structures | **REAL** | doctrine 1 | `Button@WR_DOC_STRUCT` | — |
| WAR DOCTRINES — Defensive Posture | **REAL** (needs YAML) | NEW doctrine index 4 `cmd-defensive` (Speed 80 + Damage-taken 80) | `Button@WR_DOC_DEF` | — |
| WAR DOCTRINES — Rapid Assault | **REAL** (needs YAML) | NEW doctrine index 5 `cmd-assault` (Speed 118 + Firepower 112) | `Button@WR_DOC_ASSAULT` | — |
| STRATEGIC UPGRADES — Satellite Recon $3,000 | **REAL** (needs trait) | `BuyStrategicUpgrade` idx0 → `upg-satrecon` → `RevealsMap` on THCOM | `Button@WR_UPG_SAT` + `_BUY` | `warroom-cards:satellite` |
| STRATEGIC UPGRADES — Advanced EW $4,500 | **REAL** (needs trait) | idx1 → `upg-ew` → `Range`+`Inaccuracy` multipliers | `Button@WR_UPG_EW` + `_BUY` | `warroom-cards:ew` |
| STRATEGIC UPGRADES — Precision Strikes $4,600 | **REAL** (needs trait) | idx2 → `upg-precision` → `FirepowerMultiplier` | `Button@WR_UPG_PREC` + `_BUY` | `warroom-cards:precision` |
| STRATEGIC UPGRADES — Black Projects $8,000 | **REAL** (needs trait) | idx3 permanent → `upg-blackproj` → `ProvidesPrerequisite` on FACT | `Button@WR_UPG_BLACK` + `_BUY` | `warroom-cards:black` |
| Upgrade costs ($-strings) | FLAVOR display | display strings; the **affordability gate** is real (`res.GetCashAndResources() >= cost`) | cost `Label` + `IsDisabled` | — |
| COMMAND STAFF — 4 advisors | FLAVOR | portraits + quotes static (quote can switch on doctrine/threat for a live feel — PROXY-lite) | `Image@WR_STAFF_*` + `Label@..._QUOTE` | `warroom-staff:{general,admiral,scientist,intel}` |
| BOTTOM — SETTINGS | FLAVOR | `Ui.OpenWindow("SETTINGS_PANEL")` | `Button@WR_SETTINGS` | — |
| BOTTOM — OPEN FULL WAR ROOM | CUT (v1) | this IS the full war room; hide or alias to no-op | `Button@WR_FULL` (optional) | — |
| BOTTOM — RETURN TO BATTLEFIELD | **REAL** | `Ui.CloseWindow` | `Button@WR_RETURN` | — |

**Honesty summary:** Credits, Power, all 4 military counts, income/min, faction, under-attack, doctrines, and every upgrade/action effect are live. DEFCON, attack vector, sector status, population, and active-effect timers are honest derived proxies. President name, advisor content, stars, brand crest are intentional flavor. Enemy ETA and minimap threat-arrows are CUT (no honest source). No fabricated constant is presented as live.

---

## 2. LAYOUT

**Target logical resolution:** design at **1280×720** and let it scale up (everything expressed in `WINDOW_WIDTH/HEIGHT` arithmetic so it fills any res). Root fills the viewport. All inner panels are children positioned in root-local pixels assuming a 1280×720 working canvas; for true responsiveness, the three columns are anchored with arithmetic (left fixed-width, right fixed-width via `WINDOW_WIDTH - W - margin`, center fills the gap), and the two horizontal bands are anchored to top/bottom.

**Root.** `Background@WARROOM_ROOT` — `X:0 Y:0 Width:WINDOW_WIDTH Height:WINDOW_HEIGHT`, `Background: dialog5` (solid black backdrop that blocks the battlefield and eats clicks). `Logic: WarRoomLogic`.

**Grid (at 1280×720; M = 8px margin):**

| Region | X | Y | Width | Height |
|---|---|---|---|---|
| TOP BAR `WR_TOPBAR` | 0 | 0 | `WINDOW_WIDTH` | 72 |
| LEFT COLUMN `WR_LEFT` | 8 | 80 | 300 | 452 |
| CENTER COLUMN `WR_CENTER` | 316 | 80 | `WINDOW_WIDTH - 316 - 332` | 452 |
| RIGHT COLUMN `WR_RIGHT` | `WINDOW_WIDTH - 324` | 80 | 316 | 452 |
| BOTTOM BAND `WR_BAND` | 8 | 540 | `WINDOW_WIDTH - 16` | 140 |
| BOTTOM BAR `WR_BOTTOMBAR` | 0 | `WINDOW_HEIGHT - 36` | `WINDOW_WIDTH` | 36 |

**TOP BAR internal (Y relative to `WR_TOPBAR`):**
- `WR_CREST` x8 y8 56×56 (Image); `WR_BRAND` x70 y10 200×20; `WR_STARS` x70 y34 200×20.
- `WR_TITLE` Align:Center x0 y8 `PARENT_WIDTH` 32 (BigBold); `WR_SUBTITLE` Align:Center x0 y44 `PARENT_WIDTH` 18.
- Badges centered-left of readouts: `WR_ALERT_BG` (ColorBlock red) + `WR_ALERT` at x:`PARENT_WIDTH/2 - 280` y20 w200 h28; `WR_DEFCON_BG`+`WR_DEFCON` at x:`PARENT_WIDTH/2 - 72` y20 w200 h28.
- Right readouts (each icon 18×18 + value label), right-anchored: `WR_TOP_SUPPLIES` ends at `PARENT_WIDTH-12`, `WR_TOP_POWER` to its left, `WR_TOP_CREDITS` further left, `WR_MENU` at far right (or move to bottom bar).

**LEFT COLUMN (3 stacked inset panels, `dialog3`):**
- `WR_PNL_BATTLE` y0 h160: header `WR_BATTLE_HDR`; `Radar@WR_MINIMAP` x8 y28 w284 h124.
- `WR_PNL_ENEMY` y168 h150: header; `WR_ENEMY_VECTOR`, `WR_ENEMY_ETA` (repurposed count), `WR_ENEMY_COMP_ARMOR/INF/AIR/NAVAL` rows.
- `WR_PNL_FRONT` y326 h126: header; 4 rows `WR_SECTOR_NW/W/S/E` (label) + `_STATUS` (colored).
- `WR_VIEWMAP` button at bottom of left column, y436 h24.

**CENTER COLUMN:**
- `WR_PORTRAIT` fills the column top region: x0 y0 `PARENT_WIDTH` 300 (Image, full-bleed background art region).
- `WR_SPEECH_BG` (dialog3) overlaid lower-center x40 y150 `PARENT_WIDTH-80` 80; `WR_SPEECH` inside, WordWrap.
- `WR_NAME` x0 y256 `PARENT_WIDTH` 18 (Bold); `WR_ROLE` x0 y276 `PARENT_WIDTH` 14 (Small); `WR_FACTION` x0 y292 `PARENT_WIDTH` 14.
- `WR_DECIDE_HDR` x0 y312 `PARENT_WIDTH` 18 (MediumBold, Align:Center).
- 4 action cards in a row y336 h112, each `(PARENT_WIDTH-24)/4` wide: `WR_CARD_RESERVES/AIR/ECON/ADDRESS`, each a `Background` containing title `Label` + verb `Button@WR_ACT_*`.

**RIGHT COLUMN (3 stacked `dialog3`):**
- `WR_PNL_RES` y0 h150: header; 4 rows (icon + value + delta): `WR_RES_CREDITS(+_DELTA)`, `WR_RES_POWER(+_DELTA)`, `WR_RES_SUPPLIES`, `WR_RES_POP`.
- `WR_PNL_MIL` y158 h130: header; 4 rows icon+count: `WR_MIL_VEH/INF/AIR/NAV`.
- `WR_PNL_FX` y296 h156: header; 4 rows name+state+timer: `WR_FX_AIR/INTEL/PROD/MORALE` (+`_STATE`,`_TIMER`).

**BOTTOM BAND (3 side-by-side `dialog3`, each ~1/3 width):**
- `WR_PNL_DOCTRINES` x0 w `(BAND_W-16)/3`: header; 4 radios `WR_DOC_STRUCT/ARMOR/DEF/ASSAULT` each + one-line `Label`.
- `WR_PNL_UPGRADES` mid third: header; 4 cards `WR_UPG_SAT/EW/PREC/BLACK` each = art `Image` + name + desc + cost `Label` + `_BUY` `Button`.
- `WR_PNL_STAFF` right third: header; 4 cells `WR_STAFF_STEEL/JAMESON/KOVALENKO/MARSHALL` = portrait `Image` + name + quote.

**BOTTOM BAR:** `WR_SETTINGS` left; `WR_RETURN` right (`X: WINDOW_WIDTH - 220`); optional `WR_FULL` center (or omit).

Hidden panels still tick (children of an always-present root), so `LogicTicker@WR_TICKER` placed directly under root fires every tick.

---

## 3. CHROME YAML PLAN (`mods/theater/chrome/ingame-warroom.yaml`)

Full rewrite. This file is **merged into the global widget dictionary** — keep the single top-level `Background@WARROOM_ROOT`; every child `@Id` prefixed `WR_`. Tabs for indent (MiniYaml). Pattern, panel by panel (field-complete enough to translate directly):

```
Background@WARROOM_ROOT:
	Logic: WarRoomLogic
	X: 0
	Y: 0
	Width: WINDOW_WIDTH
	Height: WINDOW_HEIGHT
	Background: dialog5
	Children:
		LogicTicker@WR_TICKER:
		# ---------- TOP BAR ----------
		Container@WR_TOPBAR:
			X: 0
			Y: 0
			Width: WINDOW_WIDTH
			Height: 72
			Children:
				Image@WR_CREST:
					X: 8
					Y: 8
					Width: 56
					Height: 56
					ImageCollection: warroom-crest
					ImageName: crest
				Label@WR_BRAND:
					X: 70
					Y: 10
					Width: 220
					Height: 20
					Font: Bold
					Contrast: True
					Text: wr-brand
				Label@WR_STARS:
					X: 70
					Y: 34
					Width: 220
					Height: 20
					Font: MediumBold
				Label@WR_TITLE:
					X: 0
					Y: 8
					Width: PARENT_WIDTH
					Height: 32
					Align: Center
					Font: BigBold
					Contrast: True
					Text: wr-title
				Label@WR_SUBTITLE:
					X: 0
					Y: 44
					Width: PARENT_WIDTH
					Height: 18
					Align: Center
					Font: Bold
					Contrast: True
					Text: wr-subtitle
				ColorBlock@WR_ALERT_BG:
					X: (PARENT_WIDTH / 2) - 280
					Y: 20
					Width: 200
					Height: 28
				Label@WR_ALERT:
					X: (PARENT_WIDTH / 2) - 280
					Y: 24
					Width: 200
					Height: 20
					Align: Center
					Font: Bold
					Contrast: True
					Text: wr-under-attack
				ColorBlock@WR_DEFCON_BG:
					X: (PARENT_WIDTH / 2) - 72
					Y: 20
					Width: 200
					Height: 28
				Label@WR_DEFCON:
					X: (PARENT_WIDTH / 2) - 72
					Y: 24
					Width: 200
					Height: 20
					Align: Center
					Font: Bold
					Contrast: True
				# right-anchored readouts (icon + value), computed in logic-friendly fixed X:
				Image@WR_ICON_CREDITS:
					X: WINDOW_WIDTH - 470
					Y: 26
					Width: 18
					Height: 18
					ImageCollection: warroom-icons
					ImageName: res-credits
				Label@WR_TOP_CREDITS:
					X: WINDOW_WIDTH - 448
					Y: 24
					Width: 120
					Height: 22
					Font: Bold
					Contrast: True
				Image@WR_ICON_POWER:
					X: WINDOW_WIDTH - 320
					Y: 26
					Width: 18
					Height: 18
					ImageCollection: warroom-icons
					ImageName: res-power
				Label@WR_TOP_POWER:
					X: WINDOW_WIDTH - 298
					Y: 24
					Width: 130
					Height: 22
					Font: Bold
					Contrast: True
				Image@WR_ICON_SUP:
					X: WINDOW_WIDTH - 160
					Y: 26
					Width: 18
					Height: 18
					ImageCollection: warroom-icons
					ImageName: res-supplies
				Label@WR_TOP_SUPPLIES:
					X: WINDOW_WIDTH - 138
					Y: 24
					Width: 130
					Height: 22
					Font: Bold
					Contrast: True
		# ---------- LEFT COLUMN ----------
		Background@WR_PNL_BATTLE:
			X: 8
			Y: 80
			Width: 300
			Height: 160
			Background: dialog3
			Children:
				Label@WR_BATTLE_HDR:
					X: 8
					Y: 6
					Width: 284
					Height: 20
					Font: MediumBold
					Contrast: True
					Text: wr-battlefield-status
				Radar@WR_MINIMAP:
					X: 8
					Y: 28
					Width: 284
					Height: 124
		Background@WR_PNL_ENEMY:
			X: 8
			Y: 248
			Width: 300
			Height: 150
			Background: dialog3
			Children:
				Label@WR_ENEMY_HDR:  { ... Text: wr-enemy-detected, Font: MediumBold }
				Label@WR_ENEMY_VECTOR: { ... Font: Bold }      # logic GetText
				Label@WR_ENEMY_ETA:    { ... Font: Small }      # repurposed: "N hostiles sighted"
				Label@WR_ENEMY_COMP_ARMOR:  { ... Font: Small } # logic GetText
				Label@WR_ENEMY_COMP_INF:    { ... Font: Small }
				Label@WR_ENEMY_COMP_AIR:    { ... Font: Small }
				Label@WR_ENEMY_COMP_NAVAL:  { ... Font: Small }
		Background@WR_PNL_FRONT:
			X: 8
			Y: 406
			Width: 300
			Height: 100
			Background: dialog3
			Children:
				Label@WR_FRONT_HDR: { ... Text: wr-frontline-reports, Font: MediumBold }
				Label@WR_SECTOR_NW:        { ... Font: Bold, Text: wr-sector-nw }
				Label@WR_SECTOR_NW_STATUS: { ... Font: Bold }   # GetText+GetColor
				Label@WR_SECTOR_W:         { ... Text: wr-sector-w }
				Label@WR_SECTOR_W_STATUS:  { ... }
				Label@WR_SECTOR_S:         { ... Text: wr-sector-s }
				Label@WR_SECTOR_S_STATUS:  { ... }
				Label@WR_SECTOR_E:         { ... Text: wr-sector-e }
				Label@WR_SECTOR_E_STATUS:  { ... }
		Button@WR_VIEWMAP:
			X: 8
			Y: 514
			Width: 300
			Height: 22
			Font: Bold
			Text: wr-view-map
		# ---------- CENTER ----------
		Container@WR_CENTER:
			X: 316
			Y: 80
			Width: WINDOW_WIDTH - 648
			Height: 452
			Children:
				Image@WR_PORTRAIT:
					X: 0
					Y: 0
					Width: PARENT_WIDTH
					Height: 300
					ImageCollection: warroom-president
					ImageName: background
				Background@WR_SPEECH_BG:
					X: 40
					Y: 150
					Width: PARENT_WIDTH - 80
					Height: 80
					Background: dialog3
					Children:
						Label@WR_SPEECH:
							X: 12
							Y: 8
							Width: PARENT_WIDTH - 24
							Height: 64
							Font: Regular
							VAlign: Top
							WordWrap: True
							Contrast: True
				Label@WR_NAME:   { Y:256, Align:Center, Font:Bold,  Text: wr-president-name }
				Label@WR_ROLE:   { Y:276, Align:Center, Font:Small, Text: wr-president-role }
				Label@WR_FACTION:{ Y:292, Align:Center, Font:Small }   # logic GetText (faction)
				Label@WR_DECIDE_HDR: { Y:312, Align:Center, Font:MediumBold, Text: wr-immediate-decisions }
				# 4 action cards (repeat for AIR/ECON/ADDRESS at X offsets):
				Background@WR_CARD_RESERVES:
					X: 0
					Y: 336
					Width: (PARENT_WIDTH - 24) / 4
					Height: 112
					Background: dialog2
					Children:
						Label@WR_CARD_RESERVES_T: { ... Font:Small, Text: wr-card-reserves }
						Button@WR_ACT_RESERVES:   { Y:84, Font:Bold, Text: wr-verb-deploy }
		# ---------- RIGHT COLUMN ----------
		Background@WR_PNL_RES:
			X: WINDOW_WIDTH - 324
			Y: 80
			Width: 316
			Height: 150
			Background: dialog3
			Children:
				Label@WR_RES_HDR: { Text: wr-national-resources, Font: MediumBold }
				# row template (repeat CREDITS/POWER/SUPPLIES/POP):
				Image@WR_RES_CREDITS_IC: { ImageCollection: warroom-icons, ImageName: res-credits, 18x18 }
				Label@WR_RES_CREDITS:       { Font: Bold }   # logic
				Label@WR_RES_CREDITS_DELTA: { Font: Small }  # logic GetText+GetColor
		Background@WR_PNL_MIL:
			X: WINDOW_WIDTH - 324
			Y: 238
			Width: 316
			Height: 130
			Background: dialog3
			Children:
				Label@WR_MIL_HDR: { Text: wr-military-overview, Font: MediumBold }
				Image@WR_MIL_VEH_IC: { ImageCollection: warroom-domains, ImageName: vehicle }
				Label@WR_MIL_VEH: { Font: Bold }   # +INF/AIR/NAV
		Background@WR_PNL_FX:
			X: WINDOW_WIDTH - 324
			Y: 376
			Width: 316
			Height: 156
			Background: dialog3
			Children:
				Label@WR_FX_HDR: { Text: wr-active-effects, Font: MediumBold }
				# rows AIR/INTEL/PROD/MORALE:
				Label@WR_FX_AIR:       { Font: Bold }                 # name (static or fluent)
				Label@WR_FX_AIR_STATE: { Font: Small }                # logic ACTIVE/INACTIVE +color
				Label@WR_FX_AIR_TIMER: { Font: Small }                # logic mm:ss (LogicTicker-driven)
		# ---------- BOTTOM BAND ----------
		Background@WR_PNL_DOCTRINES:
			X: 8
			Y: 540
			Width: (WINDOW_WIDTH - 32) / 3
			Height: 140
			Background: dialog3
			Children:
				Label@WR_DOC_HDR: { Text: wr-war-doctrines, Font: MediumBold }
				Button@WR_DOC_STRUCT:  { Font:Bold, Text: button-doctrine-structures }
				Label@WR_DOC_STRUCT_D: { Font:Small, Text: wr-doc-structures-desc }
				Button@WR_DOC_ARMOR:   { Text: button-doctrine-armor }
				Label@WR_DOC_ARMOR_D:  { Text: wr-doc-armor-desc }
				Button@WR_DOC_DEF:     { Text: wr-doc-defensive }
				Label@WR_DOC_DEF_D:    { Text: wr-doc-defensive-desc }
				Button@WR_DOC_ASSAULT: { Text: wr-doc-assault }
				Label@WR_DOC_ASSAULT_D:{ Text: wr-doc-assault-desc }
		Background@WR_PNL_UPGRADES:
			X: (WINDOW_WIDTH - 32) / 3 + 16
			Y: 540
			Width: (WINDOW_WIDTH - 32) / 3
			Height: 140
			Background: dialog3
			Children:
				Label@WR_UPG_HDR: { Text: wr-strategic-upgrades, Font: MediumBold }
				# card template (repeat SAT/EW/PREC/BLACK):
				Image@WR_UPG_SAT_IC:   { ImageCollection: warroom-cards, ImageName: satellite }
				Label@WR_UPG_SAT_NAME: { Font:Bold, Text: wr-upg-satellite }
				Label@WR_UPG_SAT_DESC: { Font:Tiny, Text: wr-upg-satellite-desc, WordWrap: True }
				Label@WR_UPG_SAT_COST: { Font:Small, Text: wr-cost-3000 }
				Button@WR_UPG_SAT_BUY: { Font:Bold, Text: wr-purchase }   # IsDisabled+OnClick
		Background@WR_PNL_STAFF:
			X: ((WINDOW_WIDTH - 32) / 3) * 2 + 24
			Y: 540
			Width: (WINDOW_WIDTH - 32) / 3
			Height: 140
			Background: dialog3
			Children:
				Label@WR_STAFF_HDR: { Text: wr-command-staff, Font: MediumBold }
				Image@WR_STAFF_STEEL:       { ImageCollection: warroom-staff, ImageName: general }
				Label@WR_STAFF_STEEL_NAME:  { Font:Bold, Text: wr-staff-steel }
				Label@WR_STAFF_STEEL_QUOTE: { Font:Tiny, WordWrap: True }  # logic GetText
		# ---------- BOTTOM BAR ----------
		Button@WR_SETTINGS:
			X: 8
			Y: WINDOW_HEIGHT - 34
			Width: 140
			Height: 28
			Font: Bold
			Text: wr-settings
		Button@WR_RETURN:
			X: WINDOW_WIDTH - 220
			Y: WINDOW_HEIGHT - 34
			Width: 212
			Height: 28
			Font: Bold
			Text: wr-return-battlefield
```

**Constraints reflected above:** `dialog5` solid-black root; `dialog3` inset panels; `dialog2` cards (all are stock `PanelRegion` collections in `mods/ra/chrome.yaml`, so no new panel art needed). `ColorBlock` (not `Background`) for the red badges so logic can `GetColor` them. `Radar` key = `RadarWidget`. `LogicTicker` directly under root so it ticks. All `Image` references point at the 5 new POT collections. No gradient/shadow/rounded-corner/tail assumed.

---

## 4. LOGIC PLAN (`WarRoomLogic.cs`)

Full rewrite. Add `using OpenRA.Mods.Common.Traits;` and `using OpenRA.Traits;` (PlayerRelationship) and `using OpenRA.Primitives;` (Color) and `using System.Linq;` and `using System.Globalization;`. Cache traits in ctor; bind all dynamics via `GetText`/`GetColor`/`OnClick`/`IsHighlighted`/`IsDisabled`/`IsVisible` Func fields. Cache the per-tick threat scan.

### 4.1 ctor + fields

```csharp
readonly World world;
readonly Player player;
readonly PlayerResources res;
readonly PowerManager power;
readonly PlayerStatistics stats;
readonly ArmyCommand army;
readonly StrategicUpgrades upgrades;   // new trait, TraitOrDefault

static readonly NumberFormatInfo NF = NumberFormatInfo.CurrentInfo;
static readonly WDist ThreatRange = WDist.FromCells(12);

// per-tick caches (refreshed in CacheTick via LogicTicker)
int cachedThreat;
string cachedVector = "—";
int vehCount, infCount, airCount, navCount, popCount;
int cacheCounter;

[ObjectCreator.UseCtor]
public WarRoomLogic(Widget widget, World world)
{
    this.world = world;
    player = world.LocalPlayer;
    res      = player?.PlayerActor.Trait<PlayerResources>();
    power    = player?.PlayerActor.TraitOrDefault<PowerManager>();
    stats    = player?.PlayerActor.TraitOrDefault<PlayerStatistics>();
    army     = player?.PlayerActor.TraitOrDefault<ArmyCommand>();
    upgrades = player?.PlayerActor.TraitOrDefault<StrategicUpgrades>();

    var radar = widget.Get<RadarWidget>("WR_MINIMAP");
    radar.IsEnabled = () => true;

    var ticker = widget.Get<LogicTickerWidget>("WR_TICKER");
    ticker.OnTick = () => { if (++cacheCounter % 15 == 0) Recache(); };
    Recache();

    BindTopBar(widget);
    BindEnemyPanel(widget);
    BindResources(widget);
    BindMilitary(widget);
    BindEffects(widget);
    BindDoctrines(widget);
    BindUpgrades(widget);
    BindActions(widget);
    BindCenter(widget);
    BindStaff(widget);

    widget.Get<ButtonWidget>("WR_RETURN").OnClick = Ui.CloseWindow;
    widget.Get<ButtonWidget>("WR_VIEWMAP").OnClick = Ui.CloseWindow;
    widget.Get<ButtonWidget>("WR_SETTINGS").OnClick = () => Ui.OpenWindow("SETTINGS_PANEL", new WidgetArgs());
}
```

### 4.2 Top bar

```csharp
void BindTopBar(Widget w)
{
    w.Get<LabelWidget>("WR_TOP_CREDITS").GetText = () =>
        res == null ? "--" : "$" + res.GetCashAndResources().ToString("N0", NF);

    var pw = w.Get<LabelWidget>("WR_TOP_POWER");
    pw.GetText = () => power == null ? "--"
        : power.PowerDrained.ToString("N0", NF) + " / " + power.PowerProvided.ToString("N0", NF);
    pw.GetColor = () => power != null && power.ExcessPower < 0 ? Color.Red : Color.White;

    w.Get<LabelWidget>("WR_TOP_SUPPLIES").GetText = () =>
        res == null ? "--" : res.Resources.ToString("N0", NF) + " / " + res.ResourceCapacity.ToString("N0", NF);

    // stars from experience (proxy)
    w.Get<LabelWidget>("WR_STARS").GetText = () =>
    {
        var xp = stats?.Experience ?? 0;
        var n = xp >= 8000 ? 4 : xp >= 4000 ? 3 : xp >= 1500 ? 2 : 1;
        return new string('★', n) + new string('☆', 4 - n);
    };

    // UNDER ATTACK badge
    var alertBg = w.Get<ColorBlockWidget>("WR_ALERT_BG");
    var alert   = w.Get<LabelWidget>("WR_ALERT");
    alertBg.GetColor = () => Color.FromArgb(200, 180, 30, 30);
    alertBg.IsVisible = () => cachedThreat > 0;
    alert.IsVisible   = () => cachedThreat > 0;

    // DEFCON badge
    var defBg = w.Get<ColorBlockWidget>("WR_DEFCON_BG");
    var def   = w.Get<LabelWidget>("WR_DEFCON");
    defBg.GetColor = () => DefconColor(Defcon());
    def.GetText    = () => "DEFCON " + Defcon();
}

int Defcon()
{
    var n = cachedThreat;
    return n == 0 ? 5 : n <= 2 ? 4 : n <= 5 ? 3 : n <= 10 ? 2 : 1;
}
static Color DefconColor(int d) => d >= 5 ? Color.LimeGreen : d == 4 ? Color.YellowGreen
    : d == 3 ? Color.Orange : d == 2 ? Color.OrangeRed : Color.Red;
```

### 4.3 Threat scan (single real computation, cached)

```csharp
void Recache()
{
    cachedThreat = 0;
    cachedVector = "—";
    vehCount = infCount = airCount = navCount = popCount = 0;
    if (player == null) return;

    // domain + population counts
    foreach (var a in world.ActorsHavingTrait<Mobile>())
    {
        if (a.Owner != player || !a.IsInWorld || a.IsDead) continue;
        var t = a.GetEnabledTargetTypes();
        if (t.Contains("Ship") || t.Contains("WaterActor")) navCount++;
        else if (t.Contains("Infantry")) infCount++;
        else if (t.Contains("Vehicle")) vehCount++;
        if (a.Info.HasTraitInfo<SelectableInfo>()) popCount++;
    }
    foreach (var a in world.ActorsHavingTrait<Aircraft>())
        if (a.Owner == player && a.IsInWorld && !a.IsDead) { airCount++; popCount++; }

    // threat scan around base buildings
    var enemies = new HashSet<Actor>();
    var baseSum = WVec.Zero; var baseN = 0;
    foreach (var b in world.ActorsHavingTrait<BaseBuilding>().Where(a => a.Owner == player && a.IsInWorld))
    {
        baseSum += new WVec(b.CenterPosition.X, b.CenterPosition.Y, 0); baseN++;
        foreach (var e in world.FindActorsInCircle(b.CenterPosition, ThreatRange))
            if (!e.IsDead && e.IsInWorld
                && player.RelationshipWith(e.Owner) == PlayerRelationship.Enemy
                && e.Info.HasTraitInfo<AttackBaseInfo>()
                && e.CanBeViewedByPlayer(player))
                enemies.Add(e);
    }
    cachedThreat = enemies.Count;
    if (baseN > 0 && enemies.Count > 0)
    {
        var centroid = new WPos(baseSum.X / baseN, baseSum.Y / baseN, 0);
        var sum = WVec.Zero;
        foreach (var e in enemies) sum += e.CenterPosition - centroid;
        cachedVector = Compass(sum);   // octant label
    }
}

static string Compass(WVec v)
{
    if (v == WVec.Zero) return "—";
    // y is south-positive in world space; map to 8 octants
    var ang = new WAngle((int)(System.Math.Atan2(-v.Y, v.X) * 512 / System.Math.PI)).Angle; // 0..1023
    // NOTE: keep this deterministic-safe — Recache runs on the render/ticker path (RunUnsynced), so Atan2 is OK here.
    string[] o = { "EAST","NORTHEAST","NORTH","NORTHWEST","WEST","SOUTHWEST","SOUTH","SOUTHEAST" };
    return o[((ang + 64) / 128) % 8];
}
```
*(Compass runs only in `Recache`, which is invoked from the unsynced `LogicTicker` path — `Atan2` is acceptable here because it never feeds sim state. If you prefer zero floats, replace with sign-of-X/Y octant bucketing.)*

### 4.4 Enemy / frontline / center text

```csharp
void BindEnemyPanel(Widget w)
{
    w.Get<LabelWidget>("WR_ENEMY_VECTOR").GetText = () => "Main vector: " + cachedVector;
    w.Get<LabelWidget>("WR_ENEMY_ETA").GetText = () => cachedThreat + " hostiles sighted";
    w.Get<LabelWidget>("WR_ENEMY_COMP_ARMOR").GetText = () => "Armor: " + EnemyDomain("Vehicle");
    w.Get<LabelWidget>("WR_ENEMY_COMP_INF").GetText   = () => "Infantry: " + EnemyDomain("Infantry");
    w.Get<LabelWidget>("WR_ENEMY_COMP_AIR").GetText   = () => "Air: " + AircraftEnemies();
    w.Get<LabelWidget>("WR_ENEMY_COMP_NAVAL").GetText = () => "Naval: " + EnemyDomain("Ship");

    BindSector(w, "WR_SECTOR_NW_STATUS", "NORTHWEST");
    BindSector(w, "WR_SECTOR_W_STATUS",  "WEST");
    BindSector(w, "WR_SECTOR_S_STATUS",  "SOUTH");
    BindSector(w, "WR_SECTOR_E_STATUS",  "EAST");
}

void BindSector(Widget w, string id, string compass)
{
    var l = w.Get<LabelWidget>(id);
    l.GetText  = () => cachedThreat == 0 ? "SECURE"
        : cachedVector == compass ? "CRITICAL"
        : cachedVector.Contains(compass[..3]) ? "CONTESTED" : "STABLE";
    l.GetColor = () => l.GetText() switch
    {
        "CRITICAL"  => Color.Red,
        "CONTESTED" => Color.Orange,
        "STABLE"    => Color.Yellow,
        _           => Color.LimeGreen,
    };
}
```
*(`EnemyDomain`/`AircraftEnemies` mirror the owned-unit filters but with `RelationshipWith == Enemy` + `CanBeViewedByPlayer`; cache inside `Recache` if perf matters.)*

### 4.5 Resources / military / center

```csharp
void BindResources(Widget w)
{
    w.Get<LabelWidget>("WR_RES_CREDITS").GetText = () =>
        res == null ? "--" : "$" + res.GetCashAndResources().ToString("N0", NF);

    var d = w.Get<LabelWidget>("WR_RES_CREDITS_DELTA");
    d.GetText  = () => stats == null ? "" : "+" + stats.DisplayIncome.ToString("N0", NF) + "/min";
    d.GetColor = () => Color.LimeGreen;

    w.Get<LabelWidget>("WR_RES_POWER").GetText = () =>
        power == null ? "--" : power.PowerProvided.ToString("N0", NF);
    var pd = w.Get<LabelWidget>("WR_RES_POWER_DELTA");
    pd.GetText  = () => power == null ? "" : (power.ExcessPower >= 0 ? "+" : "") + power.ExcessPower.ToString("N0", NF);
    pd.GetColor = () => power != null && power.ExcessPower < 0 ? Color.Red : Color.LimeGreen;

    w.Get<LabelWidget>("WR_RES_SUPPLIES").GetText = () =>
        res == null ? "--" : res.Resources.ToString("N0", NF);
    w.Get<LabelWidget>("WR_RES_POP").GetText = () => popCount.ToString("N0", NF);
}

void BindMilitary(Widget w)
{
    w.Get<LabelWidget>("WR_MIL_VEH").GetText = () => vehCount.ToString("N0", NF);
    w.Get<LabelWidget>("WR_MIL_INF").GetText = () => infCount.ToString("N0", NF);
    w.Get<LabelWidget>("WR_MIL_AIR").GetText = () => airCount.ToString("N0", NF);
    w.Get<LabelWidget>("WR_MIL_NAV").GetText = () => navCount.ToString("N0", NF);
}

void BindCenter(Widget w)
{
    // FACTION-NAME FIX: Faction.Name is a [FluentReference] KEY — must resolve through FluentProvider.
    w.Get<LabelWidget>("WR_FACTION").GetText = () =>
        player == null ? "--" : FluentProvider.GetMessage(player.Faction.Name);

    w.Get<LabelWidget>("WR_SPEECH").GetText = () =>
        cachedThreat > 0
            ? $"Mr. President, enemy forces are massing on the {cachedVector} front. They will strike soon. We recommend immediate action."
            : "All sectors holding, Commander. Standing by for orders.";
}
```

### 4.6 Doctrines (4 radios) + Effects state

```csharp
void BindDoctrines(Widget w)
{
    BindDoctrine(w, "WR_DOC_STRUCT",  1);
    BindDoctrine(w, "WR_DOC_ARMOR",   2);
    BindDoctrine(w, "WR_DOC_DEF",     4);   // new index (see §5)
    BindDoctrine(w, "WR_DOC_ASSAULT", 5);   // new index
}
void BindDoctrine(Widget w, string id, int index)
{
    var b = w.Get<ButtonWidget>(id);
    b.IsHighlighted = () => (army?.Doctrine ?? 0) == index;
    b.OnClick = () => { if (player != null)
        world.IssueOrder(new Order(ArmyCommand.OrderName, player.PlayerActor, false) { ExtraData = (uint)index }); };
}

void BindEffects(Widget w)
{
    BindEffect(w, "WR_FX_AIR",    0);   // indices into StrategicUpgrades / a small effect map
    BindEffect(w, "WR_FX_INTEL",  1);
    BindEffect(w, "WR_FX_PROD",   2);
    BindEffect(w, "WR_FX_MORALE", 3);
}
void BindEffect(Widget w, string baseId, int i)
{
    var state = w.Get<LabelWidget>(baseId + "_STATE");
    var timer = w.Get<LabelWidget>(baseId + "_TIMER");
    state.GetText  = () => upgrades != null && upgrades.IsActive(i) ? "ACTIVE" : "INACTIVE";
    state.GetColor = () => upgrades != null && upgrades.IsActive(i) ? Color.LimeGreen : Color.Gray;
    timer.GetText  = () =>
    {
        if (upgrades == null || !upgrades.IsActive(i)) return "";
        var t = upgrades.RemainingTicks(i);
        if (t <= 0) return "ACTIVE";       // permanent
        var s = t * world.Timestep / 1000;
        return $"{s / 60:00}:{s % 60:00}";
    };
}
```

### 4.7 Upgrades + action cards (synced orders only)

```csharp
void BindUpgrades(Widget w)
{
    BindBuy(w, "WR_UPG_SAT_BUY",   0, 3000);
    BindBuy(w, "WR_UPG_EW_BUY",    1, 4500);
    BindBuy(w, "WR_UPG_PREC_BUY",  2, 4600);
    BindBuy(w, "WR_UPG_BLACK_BUY", 3, 8000);
}
void BindBuy(Widget w, string id, int index, int cost)
{
    var b = w.Get<ButtonWidget>(id);
    b.IsDisabled = () => res == null || res.GetCashAndResources() < cost
                         || (upgrades != null && upgrades.IsActive(index));   // already owned/active
    b.OnClick = () => { if (player != null)
        world.IssueOrder(new Order(StrategicUpgrades.OrderName, player.PlayerActor, false) { ExtraData = (uint)index }); };
}

void BindActions(Widget w)
{
    // Deploy Reserves / Air Support = existing SupportPowers: issue their PrepareOrder via SupportPowerManager,
    // OR (simplest) close the war room and let the player target on the battlefield.
    // Economic Shift / National Address = StrategicUpgrades timed rows (indices 4,5 in the upgrade table).
    BindAction(w, "WR_ACT_ECON",    4, 0);     // free toggle (or cost) timed
    BindAction(w, "WR_ACT_ADDRESS", 5, 0);
    // Reserves/Air: see §5 for the support-power order names.
}
```

### 4.8 Command staff quotes (proxy-lite flavor)

```csharp
void BindStaff(Widget w)
{
    w.Get<LabelWidget>("WR_STAFF_STEEL_QUOTE").GetText = () => cachedThreat > 0
        ? "\"They're at our gates, Mr. President. Give the word.\""
        : "\"The line holds. Keep building.\"";
    // ... three more, switching on army?.Doctrine / cachedThreat for a live feel.
}
```

---

## 5. UPGRADES / ACTIONS PLAN

### 5.1 NEW trait — `StrategicUpgrades` (REQUIRED)

No stock trait both charges cash on an order AND broadcasts a player condition with a timer. Clone `ArmyCommand.cs` + `ArmyCommandConsumer`. Two classes:

**`StrategicUpgrades`** (player trait: `IResolveOrder, ITick, ISync`):
- `public const string OrderName = "BuyStrategicUpgrade";`
- Info field: `Upgrade[] Upgrades` where `Upgrade = { string Condition; int Cost; int Duration; }` (Duration 0 = permanent).
- `[Sync] long[] expires;` (init to `0` = inactive). Use `self.World.WorldTick` — **never** `DateTime`/`Game.RunTime`.
- `ResolveOrder`: if `order.OrderString != OrderName` bail; `i = (int)order.ExtraData`; bounds + already-active guard; `var pr = self.Owner.PlayerActor.Trait<PlayerResources>(); if (!pr.TakeCash(Upgrades[i].Cost, true)) return;` then `expires[i] = Upgrades[i].Duration > 0 ? self.World.WorldTick + Upgrades[i].Duration : long.MaxValue;` then `Apply()` to consumers.
- `Tick`: revoke any `expires[i] != 0 && expires[i] != long.MaxValue && self.World.WorldTick >= expires[i]` → set 0, re-`Apply`.
- Public read API for ChromeLogic: `bool IsActive(int i) => expires[i] != 0;` and `int RemainingTicks(int i) => expires[i] == long.MaxValue ? 0 : (int)(expires[i] - world.WorldTick);`.
- `Apply()` builds an `ActiveMask()` and pushes the set of active condition tokens to every registered `StrategicUpgradeConsumer`.

**`StrategicUpgradeConsumer`** (per-actor, self-registers with the player trait on `INotifyCreated`, like `ArmyCommandConsumer`): grants/revokes each upgrade's `Condition` token via the actor's `IConditionTimerWatcher`/`GrantCondition` mechanism (use the `ExternalCondition`/condition-token pattern from `ArmyCommandConsumer`). Add `StrategicUpgradeConsumer:` to every actor that needs to receive the condition (THCOM, FACT, PROC, and the `^AutoTargetGround/^AutoTargetAll/^Plane` templates so multipliers apply army-wide).

**Upgrade table (Info):**

| idx | Condition | Cost | Duration (ticks) | Notes |
|---|---|---|---|---|
| 0 | `upg-satrecon` | 3000 | 1500 (~60s) | timed |
| 1 | `upg-ew` | 4500 | 2000 | timed |
| 2 | `upg-precision` | 4600 | 1500 | timed |
| 3 | `upg-blackproj` | 8000 | 0 | **permanent** |
| 4 | `upg-econshift` | 0 (or cost) | 2000 | action card |
| 5 | `upg-address` | 0 | 1250 | action card |

### 5.2 Effect YAML (pure data once condition exists) — `theater-structures.yaml` / `theater-command.yaml`

```yaml
# Player trait — add to theater-command.yaml Player: block
Player:
	ArmyCommand:
	StrategicUpgrades:
		Upgrades: upg-satrecon,3000,1500 ; upg-ew,4500,2000 ; upg-precision,4600,1500 ; upg-blackproj,8000,0 ; upg-econshift,0,2000 ; upg-address,0,1250
		# (exact MiniYaml encoding per your Info parser; one row per upgrade)

THCOM:
	StrategicUpgradeConsumer:
	RevealsMap@SATRECON:
		RequiresCondition: upg-satrecon
		ValidRelationships: Ally
	AirstrikePower@AIRSUPPORT:
		OrderName: AuthorizeAirSupport
		Icon: airstrike
		ChargeInterval: 2500
		Name: actor-thcom.air-name
		Description: actor-thcom.air-description
	# DEPLOY RESERVES already exists: ParatroopersPower@coord OrderName: TheaterParadrop

FACT:
	StrategicUpgradeConsumer:
	ProvidesPrerequisite@blackproj:
		Prerequisite: blackproj.unlock
		RequiresCondition: upg-blackproj

PROC:
	StrategicUpgradeConsumer:
	CashTricklerMultiplier@ECON:
		RequiresCondition: upg-econshift
		Modifier: 140

# army-wide multipliers — add to the existing ^AutoTargetGround / ^AutoTargetAll / ^Plane templates
^AutoTargetGround:
	StrategicUpgradeConsumer:
	RangeMultiplier@EW:        { RequiresCondition: upg-ew,        Modifier: 118 }
	InaccuracyMultiplier@EW:   { RequiresCondition: upg-ew,        Modifier: 80 }
	FirepowerMultiplier@PRECISION: { RequiresCondition: upg-precision, Modifier: 125 }
	FirepowerMultiplier@ADDRESS:   { RequiresCondition: upg-address,   Modifier: 112 }
	SpeedMultiplier@ADDRESS:       { RequiresCondition: upg-address,   Modifier: 110 }
```

### 5.3 Two NEW doctrines (`theater-command.yaml`)

Extend `ArmyCommand.DoctrineConditions` so indices 4/5 exist. Since the array is on `ArmyCommandConsumerInfo`/`ArmyCommandInfo` defaults, set it in YAML on the `Player: ArmyCommand:` node:
```yaml
Player:
	ArmyCommand:
		DoctrineConditions: , cmd-focus-structures, cmd-focus-armor, cmd-focus-infantry, cmd-defensive, cmd-assault
```
Then add condition-gated multipliers to the combat templates:
```yaml
^AutoTargetGround:   # (and ^AutoTargetAll)
	SpeedMultiplier@DEF:     { RequiresCondition: cmd-defensive, Modifier: 80 }
	DamageMultiplier@DEF:    { RequiresCondition: cmd-defensive, Modifier: 80 }   # takes less damage
	SpeedMultiplier@ASSAULT: { RequiresCondition: cmd-assault,   Modifier: 118 }
	FirepowerMultiplier@ASSAULT: { RequiresCondition: cmd-assault, Modifier: 112 }
```

### 5.4 Action cards → orders

| Card | Verb | Mechanism |
|---|---|---|
| DEPLOY RESERVES | Deploy | existing `ParatroopersPower@coord` (`TheaterParadrop`). Button: close war room, then the support-power cursor is already armed via `SupportPowerManager` (or issue its prepare order). |
| AIR SUPPORT | Authorize | new `AirstrikePower@AIRSUPPORT` (`AuthorizeAirSupport`), targeted — same close-then-target flow. |
| ECONOMIC SHIFT | Activate | `StrategicUpgrades` idx4 `upg-econshift` → `CashTricklerMultiplier@ECON` on PROC. Untargeted → fires immediately on `OnClick`. |
| NATIONAL ADDRESS | Broadcast | `StrategicUpgrades` idx5 `upg-address` → army-wide Firepower/Speed morale. Untargeted. |

**Risk note (from reports):** the bought condition only reaches the **buyer's own** units. Advanced EW's *enemy* accuracy debuff would need a separate targeted `GrantExternalConditionPower` with `ValidRelationships: Enemy` — ship v1 as the friendly buff only (range/inaccuracy on own units); add the enemy debuff later if desired. `ProductionCostMultiplier` is prerequisite-gated not condition-gated, which is why Economic Shift uses `CashTricklerMultiplier` (condition-gated) instead of a build discount.

---

## 6. ART MANIFEST

All under `mods/theater/bits/warroom/` (mounted via `theater|bits`). Raws under `design/art/theater/warroom/`. Register each in `mods/theater/chrome.yaml`. **Generation must run through `zsh -ic '...'`** so `OPENAI_API_KEY` is present; PIL postprocess/atlas/crop steps run in any shell.

| Final file | POT size | Magenta-keyed? | Collection:Region |
|---|---|---|---|
| `president-warroom-bg.png` | 1024×1024 | **NO** (full scene) | `warroom-president:background 0,0,1024,1024` |
| `staff.png` (2×2 of 256) | 512×512 | yes (per-cell) | `warroom-staff: general 0,0,256,256 / admiral 256,0 / scientist 0,256 / intel 256,256` |
| `domains.png` (2×2 of 128) | 256×256 | yes | `warroom-domains: vehicle 0,0,128,128 / infantry 128,0 / aircraft 0,128 / naval 128,128` |
| `icons.png` (2×2 of 128) | 256×256 | yes | `warroom-icons: res-credits 0,0,128,128 / res-power 128,0 / res-supplies 0,128 / res-pop 128,128` |
| `cards.png` (2×2 of 128) | 256×256 | yes | `warroom-cards: satellite 0,0,128,128 / ew 128,0 / precision 0,128 / black 128,128` |
| `theater-crest.png` | 256×256 | yes | `warroom-crest: crest 0,0,256,256` |

**chrome.yaml additions** (append; tabs):
```yaml
warroom-president:
	Image: warroom/president-warroom-bg.png
	Regions:
		background: 0, 0, 1024, 1024
warroom-staff:
	Image: warroom/staff.png
	Regions:
		general: 0, 0, 256, 256
		admiral: 256, 0, 256, 256
		scientist: 0, 256, 256, 256
		intel: 256, 256, 256, 256
warroom-domains:
	Image: warroom/domains.png
	Regions:
		vehicle: 0, 0, 128, 128
		infantry: 128, 0, 128, 128
		aircraft: 0, 128, 128, 128
		naval: 128, 128, 128, 128
warroom-icons:
	Image: warroom/icons.png
	Regions:
		res-credits: 0, 0, 128, 128
		res-power: 128, 0, 128, 128
		res-supplies: 0, 128, 128, 128
		res-pop: 128, 128, 128, 128
warroom-cards:
	Image: warroom/cards.png
	Regions:
		satellite: 0, 0, 128, 128
		ew: 128, 0, 128, 128
		precision: 0, 128, 128, 128
		black: 128, 128, 128, 128
warroom-crest:
	Image: warroom/theater-crest.png
	Regions:
		crest: 0, 0, 256, 256
```

**Generate commands.** Build `design/art/generate_warroom_art.py` by copying `generate_theater_cameos.py` structure (STYLE consts + ASSETS dict + `gen()`/`post()` + `--force` skip), then:
```bash
zsh -ic 'python3 design/art/generate_warroom_art.py'          # full batch
zsh -ic 'python3 design/art/generate_warroom_art.py --force'  # regenerate raws
```

**Prompts (verbatim where load-bearing):**

President hero (NO magenta, scene; the one exception):
```
Cinematic wide establishing shot of a dark high-tech national emergency war room at night, photoreal, moody. A composed middle-aged male president in a dark suit sits at a sweeping command desk positioned slightly LEFT of center, seen from a respectful three-quarter front angle, lit from below by the cold blue glow of tactical monitors. Behind and to the RIGHT, floor-to-ceiling panoramic windows reveal a distant burning city skyline at night — orange fires, smoke columns, silhouettes of military drones and fighter jets streaking across a bruised sky. The UPPER-CENTER of the frame is deliberately dark, empty and uncluttered (negative space for an overlaid caption). Rich teal-and-amber cinematic color grade, volumetric haze, shallow depth of field, subtle film grain, contemporary military techno-thriller aesthetic, de-blurred crisp HD detail. No text, no logos, no captions, no UI, no watermark.
```
```bash
zsh -ic 'python3 design/art/generate_image.py --out design/art/theater/warroom/president-warroom-bg_raw.png --prompt "<PROMPT ABOVE>" --size 1536x1024 --quality high --background opaque'
python3 -c "from PIL import Image;im=Image.open('design/art/theater/warroom/president-warroom-bg_raw.png').convert('RGB');w,h=im.size;s=min(w,h);im=im.crop(((w-s)//2,0,(w-s)//2+s,s)).resize((1024,1024),Image.LANCZOS);im.save('mods/theater/bits/warroom/president-warroom-bg.png')"
```

Every other asset ends with the EXACT magenta clause:
> `The ENTIRE background is one flat solid pure magenta color (hex #FF00FF / rgb 255,0,255), evenly filled, so it can be keyed out.`

Advisor headshots — `STYLE_HEAD` + per-subject (General Steel = older four-star army general, close-cropped grey hair, square jaw, olive dress uniform with campaign ribbons; Admiral Jameson = navy admiral, Black male, white dress uniform with gold shoulder boards; Dr. Kovalenko = female defense scientist, late 30s, Eastern-European, lab coat over turtleneck, glasses; Director Marshall = lean intelligence director, dark suit no tie, earpiece). Generate 1024×1024 each → `postprocess_cameo.py --width 256 --height 256 --tol 80`.

Domain icons (`STYLE_ICON`, light steel-grey) — tank / helmeted-soldier / top-down jet / destroyer. Resource icons (same, color-substituted) — gold coin (credits), cyan lightning bolt (power), amber crates (supplies), green group-of-three (pop). Upgrade cards (`STYLE_CARD`) — recon satellite over Earth / EW dish with signal arcs / cruise missile with crosshair / stealth prototype in dark hangar. Crest — front-facing eagle in circular seal, brushed-steel + muted-gold, no text. Each: generate 1024 → `postprocess_cameo.py --width N --height N --tol 80`.

Atlas pack (after all per-icon postprocess):
```bash
python3 -c "
from PIL import Image
def atlas(out,size,cells):
    a=Image.new('RGBA',(size,size),(0,0,0,0))
    for p,(x,y) in cells:
        im=Image.open(p); a.paste(im,(x,y),im)
    a.save(out)
B='mods/theater/bits/warroom/'
atlas(B+'staff.png',512,[(B+'staff-general.png',(0,0)),(B+'staff-admiral.png',(256,0)),(B+'staff-scientist.png',(0,256)),(B+'staff-intel.png',(256,256))])
atlas(B+'domains.png',256,[(B+'icon-vehicle.png',(0,0)),(B+'icon-infantry.png',(128,0)),(B+'icon-aircraft.png',(0,128)),(B+'icon-naval.png',(128,128))])
atlas(B+'icons.png',256,[(B+'res-credits.png',(0,0)),(B+'res-power.png',(128,0)),(B+'res-supplies.png',(0,128)),(B+'res-pop.png',(128,128))])
atlas(B+'cards.png',256,[(B+'card-satellite.png',(0,0)),(B+'card-ew.png',(128,0)),(B+'card-precision.png',(0,128)),(B+'card-black.png',(128,128))])
"
```

---

## 7. FLUENT STRINGS (append to `mods/theater/fluent/theater.ftl`)

Keep existing `warroom-title`, `button-doctrine-*`, `button-warroom-close` (still referenced as fallbacks). Add:

```
# War Room — top bar
wr-brand = THEATER COMMAND
wr-title = NATIONAL COMMAND
wr-subtitle = EMERGENCY WAR ROOM
wr-under-attack = UNDER ATTACK
# (DEFCON text built in logic)

# Left column
wr-battlefield-status = BATTLEFIELD STATUS
wr-enemy-detected = ENEMY FORCE DETECTED
wr-frontline-reports = FRONTLINE REPORTS
wr-sector-nw = Northwest
wr-sector-w = Western
wr-sector-s = Southern
wr-sector-e = Eastern
wr-view-map = VIEW DETAILED MAP

# Center
wr-president-name = PRESIDENT ANDREW HAWKINS
wr-president-role = COMMANDER IN CHIEF
wr-immediate-decisions = IMMEDIATE DECISIONS REQUIRED
wr-card-reserves = DEPLOY RESERVES
wr-card-air = AIR SUPPORT
wr-card-econ = ECONOMIC SHIFT
wr-card-address = NATIONAL ADDRESS
wr-verb-deploy = Deploy
wr-verb-authorize = Authorize
wr-verb-activate = Activate
wr-verb-broadcast = Broadcast

# Right column
wr-national-resources = NATIONAL RESOURCES
wr-military-overview = MILITARY OVERVIEW
wr-active-effects = ACTIVE EFFECTS
wr-fx-air = Air Superiority
wr-fx-intel = Advanced Intel
wr-fx-prod = War Production Boost
wr-fx-morale = National Morale

# Bottom band — doctrines
wr-war-doctrines = WAR DOCTRINES
wr-doc-structures-desc = Crush enemy buildings first.
wr-doc-armor-desc = Hunt enemy tanks first.
wr-doc-defensive = Defensive Posture
wr-doc-defensive-desc = Slower, hardened, takes less damage.
wr-doc-assault = Rapid Assault
wr-doc-assault-desc = Faster advance, heavier firepower.

# Bottom band — upgrades
wr-strategic-upgrades = STRATEGIC UPGRADES
wr-upg-satellite = Satellite Recon
wr-upg-satellite-desc = Reveals the full map for 60s.
wr-upg-ew = Advanced EW
wr-upg-ew-desc = +range, tighter aim army-wide.
wr-upg-precision = Precision Strikes
wr-upg-precision-desc = +25% firepower army-wide.
wr-upg-black = Black Projects
wr-upg-black-desc = Permanently unlocks classified tech.
wr-cost-3000 = $3,000
wr-cost-4500 = $4,500
wr-cost-4600 = $4,600
wr-cost-8000 = $8,000
wr-purchase = PURCHASE

# Bottom band — staff
wr-command-staff = COMMAND STAFF
wr-staff-steel = General Steel
wr-staff-jameson = Admiral Jameson
wr-staff-kovalenko = Dr. Kovalenko
wr-staff-marshall = Director Marshall

# Bottom bar
wr-settings = SETTINGS
wr-return-battlefield = RETURN TO BATTLEFIELD

# THCOM air support power (rules)
actor-thcom.air-name = Air Support
actor-thcom.air-description = Call in a targeted airstrike.
```

---

## 8. BUILD ORDER & VERIFICATION

### Sequence
1. **Trait first.** Add `OpenRA.Mods.Common/Traits/Player/StrategicUpgrades.cs` (+ `StrategicUpgradeConsumer`) cloned from `ArmyCommand.cs`/`ArmyCommandConsumer`. Compile early — it's referenced by both logic and YAML. `dotnet build OpenRA.sln -c Debug` and grep warnings for `StrategicUpgrades`.
2. **Rules.** Add `StrategicUpgrades:` + extended `DoctrineConditions` to `theater-command.yaml`; add consumers/powers/multipliers to `theater-structures.yaml` and the combat templates. Run `./utility.sh theater --check-yaml`.
3. **Art.** Run the batch (`zsh -ic 'python3 design/art/generate_warroom_art.py'`), then the atlas pack. Verify each final PNG is POT with `python3 -c "from PIL import Image;import glob;[print(p,Image.open(p).size) for p in glob.glob('mods/theater/bits/warroom/*.png')]"`.
4. **Chrome collections.** Append the 6 collections to `mods/theater/chrome.yaml`. `./utility.sh theater --check-yaml`.
5. **Fluent.** Append §7 keys to `theater.ftl`.
6. **Layout.** Rewrite `ingame-warroom.yaml` per §3. `./utility.sh theater --check-yaml` (catches duplicate `@Id`, missing fluent refs, unknown widget types).
7. **Logic.** Rewrite `WarRoomLogic.cs` per §4. `dotnet build OpenRA.sln -c Debug`, grep warnings for `WarRoomLogic`.
8. **Run.** `./theater-play.sh`, start a skirmish, build a Theater Command, select it (or `/warroom`). Watch the exception log.

### Verification checklist
- Build: `make` (Release) succeeds → `bin/` updated. Debug build clean for the touched files (env note: `make check`'s ~3,300 IDE0055 errors are pre-existing stock-file noise; grep Debug warnings for YOUR filenames only).
- YAML: `./utility.sh theater --check-yaml` exits clean (validates fluent refs, widget ids, target types).
- Runtime: open the war room as the **local player** AND as a **spectator** (no LocalPlayer) — must not NPE. Confirm Credits/Power/military counts move as you build/lose units; trigger an enemy approach and watch UNDER ATTACK + DEFCON + sector status change; click each doctrine radio (highlight follows `army.Doctrine`); buy an upgrade with enough cash (button enables, cash drops, effect condition shows ACTIVE) and with too little (button disabled).
- Exceptions: tail `~/Library/Application Support/OpenRA/Logs/exception.log` (and `debug.log`) after opening — any chrome/trait error lands there.

### Top crash risks & how each is avoided
| Risk | Symptom | Avoidance |
|---|---|---|
| **Non-POT art** | sprite-sheet packer garbles/throws at load | every `Image:` PNG is 256/512/1024 (§6); icons live in POT atlases; verify with the glob size check before launch |
| **`Background:` pointing at a non-PanelRegion collection** | panel renders blank / throws in `DrawPanel` | only use stock panel names `dialog5`/`dialog3`/`dialog2` (all have `PanelRegion`); new collections are `Image`-only and never used as `Background` |
| **Duplicate `@Id`** | `InvalidDataException` hard crash at mod load | every id prefixed `WR_`; no bare ids; `Radar@WR_MINIMAP` not `RADAR_MINIMAP` |
| **Hidden widget doesn't tick → stale countdowns** | timers freeze | `LogicTicker@WR_TICKER` is a direct child of the always-present root; effect/threat caches refresh in its `OnTick` |
| **Chrome merge / wrong file edited** | layout not picked up, or old modal still shows | `ingame-warroom.yaml` is already registered at `mod.yaml:123`-area ChromeLayout; rewrite in place, keep single `WARROOM_ROOT` root |
| **`world.LocalPlayer == null` (spectator)** | NPE on open | ctor guards `player == null`; every binding short-circuits; traits resolved with `?.` + `TraitOrDefault` |
| **Faction shows raw key** | label reads `faction-federation` not "Federation" | `FluentProvider.GetMessage(player.Faction.Name)` (the §4.5 fix; matches `LobbyUtils.cs:593`) |
| **Cash mutated from ChromeLogic** | lock-step desync in multiplayer | spends go through `Order(StrategicUpgrades.OrderName,...)` → `IResolveOrder.TakeCash`; ChromeLogic only READS `GetCashAndResources()` |
| **`FindActorsInCircle`/4 domain scans every frame** | frame stalls on large armies | all scans run only in `Recache()`, called every 15 ticks from the LogicTicker, results cached |
| **Target-type strings renamed in theater** | military/enemy counts silently read 0 | theater inherits ra `defaults.yaml` (`Vehicle`/`Infantry`/`Ship`/`WaterActor`); verified present — do not rename |
| **Missing `PlayerStatistics`** | income `/min` and stars blank | ra-derived player has it; if absent, income falls back to Earned-delta tracker, stars fixed at 4 |
