# THEATER — The Command Layer ("feel like the president")

*Vision captured 2026-06-19. Source: user direction — the Theater Command (and every structure)
should become a polished, character-driven panel where you set grand strategy and buy doctrine
upgrades, with an animated character (a general for Command, a scientist for tech, …), speech
bubbles, and a "neo-retro" look that replaces the current dated UI.*

## The vision, broken into features

1. **Army targeting doctrine** — at the Theater Command, choose what your whole army prioritizes:
   structures over units, anti-armor (rocketeers focus tanks), etc. Unlocked/deepened via upgrades.
2. **Formation / anti-pileup** — an upgrade that fixes tanks/subs/infantry single-filing behind each
   other and blindly following the leader into death. Real group movement / spacing.
3. **Character-driven structure panels ("president feel")** — selecting a structure opens a rich panel:
   an **animated character** (general / scientist / …), **speech bubbles**, and a tree of **strategy
   upgrades** you buy. Clicking in should feel like running a war room.
4. **Roll the panel framework out to every structure**, each with its own character + theme.
5. **Neo-retro visual overhaul** — eventually re-skin the whole game to a cohesive modern-but-retro look.

## What the research found (feasibility)

| Feature | Mechanism | Difficulty | New C#? |
|---|---|---|---|
| Targeting doctrine | `AutoTargetPriority` is a ConditionalTrait (RA already stance-gates it). Add `cmd-*` condition-gated priority profiles to shared templates; a player order toggles the condition army-wide (mirrors the doctrine pattern). Higher `Priority` wins in `AutoTarget.ChooseTarget`. | **Easy** | tiny (one order handler) — mostly YAML |
| President panel | `INotifySelection` → show a custom Widget+Logic panel when the Theater Command is selected. `Animation` class renders an animated portrait; a Label+balloon = speech bubble; buttons issue a "buy upgrade" order that spends `PlayerResources` cash and grants a condition/prerequisite. | **Medium** | yes (widget + logic + order) + chrome yaml + character art |
| Formation / anti-pileup | No group-move/formation exists — units path independently to the same cell; Nudge is only reactive. Needs a new movement activity (spread destinations / formation offsets). | **Hard** | yes (new Activity, ~300-500 LoC) |
| All-structure panels | Reuse the president-panel framework, one config + character per structure. | Medium (after panel exists) | reuse |
| Neo-retro overhaul | New chrome art + palette + widget restyle across the whole UI. | Very large | art-heavy |

Key files the build will touch: `OpenRA.Mods.Common/Traits/AutoTarget*.cs` (read), shared templates in
`theater-doctrines.yaml`, new `OpenRA.Mods.Common/Widgets/TheaterCommandWidget.cs` +
`Widgets/Logic/Ingame/TheaterCommandPanelLogic.cs` + an upgrade-purchase order, a new
`theater|chrome/command-panel.yaml`, `theater|sequences/ui.yaml`, character art under `theater/bits`.

## Phased plan (each phase ships + is testable)

- **Phase 1 — Targeting doctrine (the brain).** Add `cmd-prioritize-structures`, `cmd-focus-antiarmor`,
  `cmd-defend` (and a default) as condition-gated `AutoTargetPriority` profiles on the shared templates;
  a player order toggles them. Ship first with a chat command + a temporary sidebar button so it's
  testable before the fancy panel exists. *Easy, high-impact, the functional core of "command your army."*
- **Phase 2 — The President Panel (the feel).** Build the selection-driven command panel: animated
  **general portrait** (GPT-image character art) + **speech bubbles** + **upgrade buttons**. The Phase-1
  targeting doctrines become the first purchasable upgrades. This is where it starts to feel like the
  war room. *Medium; the headline experience.*
- **Phase 3 — Formation / anti-pileup upgrade.** New movement activity (spread-destination + spacing),
  bought from the Command panel. *Hard; the deepest engineering.*
- **Phase 4 — Every structure gets a character.** Generalize the panel framework; scientist for the tech
  lab, etc. Each structure's upgrades themed to it.
- **Phase 5 — Neo-retro overhaul.** Cohesive restyle of chrome/palette/widgets game-wide.

**Sequencing note:** Phases 1→2 deliver the core loop (set doctrine → feel like the president buying it)
fastest. Formation (3) is the hardest and is best done after the panel exists to host its upgrade.
Character/UI art (Phase 2+) uses the proven GPT-image pipeline; the animated portrait can be a few frames.

## Decisions (user, 2026-06-19)
- **Build order: MECHANICS FIRST** — Phase 1 targeting doctrine (so the army actually obeys), then
  Phase 2 the panel that controls it. Nothing ships as a stub.
- **Panel placement: BIG CENTERED "SITUATION ROOM" OVERLAY** — a large cinematic modal (the war room),
  not a side panel. Most screen real estate, most "feel like the president."

## Still-open (decide while building)
- How many targeting doctrines at launch, and which are free vs. upgrade-gated.
- Character style for the general/scientist (matches the cameo art style → neo-retro target).
