# THEATER — Playtest Guide (what to check & decide)

This session built out the full **"Civilization but real-time"** layer across many small, verified passes
(all on the `theater` branch, pushed to GitHub). Everything passed the headless gate — build, analyzer,
unit tests, `--check-yaml`, and a clean boot — but a handful of things **only a real match can judge**:
feel, balance numbers, and exact in-world sprite alignment. This is the checklist for your first playtest.

## How to run

```bash
./theater-play.sh                 # launches the THEATER mod
```
Fastest way to inspect everything: the **sandbox** (Skirmish → map "theater-sandbox") starts with dev mode
on — instant build, build anywhere, unlimited cash — so you can place every building and rush the whole tech
tree in seconds. For win-condition / AI behaviour, play a normal skirmish vs AI instead (the sandbox is solo).

## 1. Buildings render correctly  ← the original complaint

Place each and confirm it shows its **bespoke body**, not a stock OpenRA building:
- **Bank** → a columned bank (not a silo). **Alloy Extractor** → an industrial extractor.
- **Research Lab**, **Fusion Reactor**, **Theater Command** → their own models (not dome/power-plant).

Verified at the rules level (each renders its own sprite) and right-sized to its footprint (~27px/cell). The
one thing to eyeball: **vertical alignment** — does each sit flush on the ground, or float/sink a few pixels?
If any is off, it's a small per-building sprite-offset tweak.

## 2. The living world

- A few **towns** (clusters of wandering civilians) seed across the map; **~4 of them have a capturable
  Hospital** (a "city-state" — capture with an engineer for healing + vision). Watch the world **population
  grow** over the match (shown top-center: `Pop N`).
- Park buildings near a town → you should see a steady **tax-base** cash trickle from the nearby civilians.
- **Decide:** are 4 capturable hospitals about right, too many, or too few? (One YAML number:
  `SettlementStructureCount` in `theater-livingworld.yaml`.)

## 3. Time & escalation

- Top-center shows the **era + timer + population**: Mobilization → Escalation → Open Conflict → Total War →
  Final Hour (~6 min each), announced as it advances.
- Late-game combat should feel **deadlier** (symmetric +4% firepower/era). **Decide:** is the escalation
  noticeable but not oppressive? Does it help break late-game stalemates?

## 4. Space program & science victory

- In the Theater Command's **Command** queue, build **Launch Complex → Recon Satellites → Orbital Command**
  (gated by the eras, so it unfolds over real time). Confirm the **Orbital Lance** strike appears on the
  Theater Command after Orbital Command, and that it hits hard.
- **Science victory:** after completing the program, a 5-minute countdown runs while a Theater Command stands.
  It's **broadcast to everyone** (warning + 1-min/10-s calls). Confirm an opponent can **stall it by
  destroying your Theater Command** (countdown resets). **Decide:** is 5 minutes the right hold, and does the
  program complete at a fair time (~Total War era)?

## 5. AI

- The AI builds the full economy + space program. **Watch for:** does it ever go for a science win (you'll
  see the broadcast)? Does it pose a real threat, or is the science win too easy/hard for it?

## Quick-tune knobs (all data, no rebuild needed for the YAML ones)

| Want to change | Where |
|---|---|
| Hospitals per map / town count / population size | `mods/theater/rules/theater-livingworld.yaml` |
| Science-victory hold time | `theater-spaceprogram.yaml` → `SpaceVictoryConditions: HoldDuration` |
| Escalation rate | `theater-escalation.yaml` → `FirepowerMultiplier` modifiers |
| Tax income | `theater-livingworld.yaml` → `CivilianTaxBase` |
| Building vertical alignment | the body's sequence in `mods/theater/sequences/theater.yaml` (add an `Offset`) |

See `theater-how-to-play.md` for the player-facing guide and `theater-balance-notes.md` for the multiplier model.
