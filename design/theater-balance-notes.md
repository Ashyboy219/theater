# THEATER — Balance Notes: the multiplier-stacking model

THEATER layers several systems that all apply *multiplicative* stat modifiers to the same units —
national faction profiles, mid-game faction doctrine branches, the research tree, age/era advancement,
and the three universal decision-forks. Because they multiply, it's worth writing down how they compose
and confirming the worst-case stacks don't produce a degenerate "max-everything" unit. This is the
reference for anyone tuning or extending the systems.

## What stacks (and what's mutually exclusive)

A single **vehicle** can be affected by, at most:

| Layer | How many apply | File |
|---|---|---|
| National profile (`doc-<faction>`) | exactly **1** (your faction, always on) | `theater-doctrines.yaml` |
| Faction doctrine branch (`doc-<f>-<branch>`) | at most **1** (your faction, and the two branches are mutually exclusive) | `theater-doctrine-*.yaml` |
| Research Tier 1 — ballistics/armor/optics/propulsion/logistics | **all** (they accumulate) | `theater-research.yaml` |
| Mass **vs** Precision | **1** (exclusive) | `theater-research.yaml` |
| Research Tier 2 — net-centric/reactive-armor/autoloaders | **all** (accumulate) | `theater-research.yaml` |
| Ages — Information + Autonomous + Orbital | **all 3** cumulative (at the top of the ladder) | `theater-ages.yaml` |
| War Economy **vs** Industrial Base | **1** (exclusive; cost only) | `theater-research-economy.yaml` |
| Maneuver **vs** Fortress | **1** (exclusive; Maneuver→vehicles, Fortress→`^Defense`) | `theater-research-posture.yaml` |
| Command & Control aura | **situational** — +10% firepower only while within 6 cells of your own Theater Command | `theater-command-aura.yaml` |

No individual modifier is larger than **±25%** (e.g. +25% recon, −20% defense damage).

> The strategic resource (**alloys**, P6) is a *counted second currency*, not a stat multiplier — it gates and
> is spent on the apex tier. It does not enter the multiplier stack below and so does not affect these bounds.

## Worst-case stacks on a fully-teched vehicle (computed)

- **Max firepower** (glass-cannon, e.g. Russia Artillery branch + Precision + Ballistics + all ages): **~2.1×**
- **Max effective HP** (armor build: Russia Armor branch + Armor + Reactive-Armor research + ages): **~2.1×**
- **Max speed** (Armor branch + Propulsion + Maneuver fork): **~1.3×**
- A *coherent* armor build reaches **~1.9× firepower × ~2.1× EHP ≈ 4× base combat power.**
- The **C2 aura** adds a *situational* +10% firepower while near your Theater Command, so a fully-teched
  vehicle fighting on home ground peaks at **~2.3× firepower** — still bounded, still symmetric, and it
  costs you the positioning (you only get it defending/rallying at the command post, not on the attack).

## Why this is sound (not degenerate)

1. **Mutual exclusivity prevents max-everything.** The highest firepower comes from the *Artillery* branch +
   *Precision* research; the highest toughness comes from the *Armor* branch + *Armor* research. The branches
   are mutually exclusive, so **no single unit gets both maxima** — you commit to a profile.
2. **It's symmetric escalation.** The ~4× ceiling requires the entire faction tree + all three ages + the
   right forks. Both players can reach it; it is the intended "longer, escalating game" curve, not an
   asymmetric exploit.
3. **The age bump is earned, not free.** Each age is a real cost+time+prerequisite gate; the cumulative
   ~1.2× firepower / ~1.23× EHP it grants is the reward for advancing, available to anyone who invests.
4. **Tradeoffs are real.** Frailty stacks too (e.g. UK Intel national + Intel branch + Mass research ≈ 1.5×
   damage taken before ages pull it back to ~1.24×) — glass-cannon builds are genuinely fragile, by choice.

## Tuning guidance

- Keep new per-source modifiers within roughly **±25%** so the multiplicative stack stays bounded.
- New *accumulating* upgrades compound with everything — prefer **mutually-exclusive forks** (`~!other`) for
  anything strong, so it's a decision rather than a flat power creep.
- If the endgame ever feels too swingy, the **ages** are the broadest lever (they hit every unit); dial the
  age firepower/damage modifiers before touching per-unit research.

*Method: extracted every `*Multiplier` on `^Vehicle` across the rule files and multiplied the largest
co-applicable set per axis (respecting exclusivity). No degenerate combination was found.*
