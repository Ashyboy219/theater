# THEATER — Balance Notes: the post-redesign model

> **Updated 2026-06-23** to match the tech-tree redesign (commit f5f9462cc0). The research tree, the
> three ages, escalation, and the universal decision-forks **no longer apply stat multipliers** — they
> were converted to *content unlocks* (they enable new units / structures / support powers, and the
> ages are pure prerequisite gates). The only systems that still multiply unit stats are the **national
> faction profile** and the **faction doctrine branch** (plus the situational **C2 aura**). This roughly
> **halved** the worst-case stat stack (≈4× → ≈2×). The older "every layer multiplies" model is gone.

## What actually multiplies stats now (verified against the rule files)

| Layer | How many apply | Effect | File |
|---|---|---|---|
| National profile (`doc-<faction>`) | exactly **1** (your faction, always on) | a 2–3-axis profile, each within **±20%** (Firepower / Damage-taken / Speed / Range / Vision / Inaccuracy) | `theater-doctrines.yaml` |
| Faction doctrine branch (`doc-<f>-<branch>`) | at most **1** (the two branches are mutually exclusive) | a small per-branch multiplier set, **85–118** (~±18%) | `theater-doctrine-*.yaml` (usa/rus/chn/uk/ind; tur is content-only) |
| Command & Control aura | **situational** — `FirepowerMultiplier@c2: 110` (+10%) on `^Vehicle`/`^Infantry` only while within range of your own Theater Command | `theater-command-aura.yaml` |
| Economy Network | **income only** — `CashTricklerMultiplier@econ: 130` (+30%) on banks/extractors; NOT combat | `theater-economy.yaml` |

Everything else is now **content, not numbers**:

| Former multiplier layer | What it does now |
|---|---|
| Research tiers (ballistics/armor/optics/…) | **deleted** — replaced by tech-tree nodes that *unlock* units/structures/powers |
| Mass vs Precision / War Economy vs Industrial / Maneuver vs Fortress | **content forks** — each unlocks a distinct power/structure; mutually exclusive; **no stat numbers** |
| Ages (Information / Autonomous / Orbital) | **pure prerequisite gates** — they unlock the next tier of content; no per-age stat bump |
| Total-War escalation | **deleted entirely** |

> The strategic resource (**alloys**) is a *counted second currency*, not a multiplier — it gates and is
> spent on the apex tier. It does not enter the stack below.

## Worst-case combat stack on a fully-teched vehicle (recomputed)

Only the national profile (×1) and one doctrine branch (×1) co-apply, plus the situational aura:

- **Max firepower:** national `FirepowerMultiplier` (≤112) × doctrine-branch firepower (≤~118) × C2 aura (110)
  ≈ **~1.45× firepower** on home ground (≈ **1.32×** away from the command post).
- **Max effective HP:** national `DamageMultiplier` (≥82 = takes less) × doctrine `DamageMultiplier` (≥85)
  ≈ **~1.43× EHP**.
- A *coherent* build commits to one profile, so a single unit rarely gets both maxima at once; realistic
  peak combat power is **~1.5–2.1×** base (situational), **down from the old ~4×**.

## Why this is sound (not degenerate)

1. **Escalation is now mostly horizontal (content), not vertical (stats).** Advancing the tree unlocks new
   *options*, so the power curve comes from *what* you can field, not flat stat creep on what you already have.
2. **Only faction identity multiplies, and it's bounded + symmetric.** Each profile is ±~20% on a couple of
   axes, the doctrine branches are mutually exclusive (you commit to a profile, you don't stack both), and
   both players can reach the same ceiling.
3. **The C2 aura costs positioning.** +10% firepower only while hugging your Theater Command — a defensive/
   rallying bonus, not free attack power.
4. **Tradeoffs are real.** Glass-cannon profiles take more damage (`DamageMultiplier > 100`); toughness
   profiles give up firepower/speed. No single faction maxes every axis.

## Tuning guidance

- The broadest combat lever is now the **national profiles** (`theater-doctrines.yaml`) — they hit every unit
  of a faction. The ages are no longer a stat lever (they gate content), so don't reach for them to tune power.
- Keep any *new* per-source modifier within roughly **±20%** so the (now much shorter) multiplicative stack
  stays bounded.
- Anything strong should be a **content unlock or a mutually-exclusive fork**, not an accumulating multiplier —
  that's the redesign's whole point (a decision, not power creep).
- The **C2 aura** Range/Modifier and the **bank/alloy-extractor income rates** are the most playtest-sensitive
  numbers (passive economy strength and home-ground firepower); tune those from real games, not on paper.

## Items still requiring a GUI playtest (headless-unverifiable)

- Faction firepower/EHP feel at the new ~2× ceiling — is the escalation now too *flat* (boring) or right?
- Bank / Alloy Extractor passive income strength (build-and-forget economy) vs harvesting.
- Apex units (BuildLimit 2, 5000–6000 cr + 100 alloys): 200k–500k HP — godlike-but-limited, or too tanky?
- The re-bodied static air-defenses (AKASH/AEGIS): firing behaviour now that the *visible* turret is gone
  (logical Turreted/AttackTurreted retained); and turret-on-hull alignment on the DONGFENG.

*Method: enumerated every `*Multiplier`/`*Multiplier@suffix` trait across `mods/theater/rules/` (2026-06-23).
Only the faction-profile, faction-doctrine, C2-aura, and econ-network files contain any; the research/age/
fork/escalation files contain none. Test suite: 487 pass / 0 fail. check-yaml: 0 errors.*
