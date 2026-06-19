# THEATER deterministic fast-research / tech-era 4X system

**Executable now:** True

**Summary:** A snappy, deterministic research system for THEATER built almost entirely on OpenRA's existing prerequisite plumbing: a single player-actor trait (ResearchManager) accumulates integer research points each tick, spends them to run one tech at a time on an integer countdown, and on completion emits `research.<id>` strings through the universal ITechTreePrerequisite.ProvidesPrerequisites hook. Because TechTree already drives Buildable gating, GrantConditionOnPrerequisite, and SupportPowerManager off prerequisites, a completed research instantly unlocks units, upgrades, and abilities with zero new consumer-side code. v1 is buildable now with one new C# trait + a thin UI logic class reusing the support-power palette look, gated behind a lobby checkbox for match-light vs campaign-heavy.

## Key decisions
- Integrate via the existing ITechTreePrerequisite.ProvidesPrerequisites hook — a completed research just adds a `research.<id>` string to the player's TechTree, which already gates Buildable, GrantConditionOnPrerequisite, and SupportPowerManager with zero consumer-side code.
- Single player-actor trait (ResearchManager) like TechTree/SupportPowerManager/DeveloperMode, holding all state; income and spend are integer-only following the CashTrickler/DevelopsWhileHeld/SupplyNetwork determinism pattern.
- Research points come from a passive integer per-interval rate PLUS a tiny ProvidesResearch companion trait on structures/capturable nodes — 'lose the lab, lose the income', making research a real economy, not a passive aura.
- One active research at a time in v1 with an integer countdown (mirrors SupportPowerInstance sub-tick accounting) for a snappy, readable feel; QueueCapacity field reserved for parallel research later.
- UI: reuse the support-power command-palette look (icon + integer charge ring + click), NOT a real ProductionQueue (which produces actors with exits/placement) and NOT a bespoke panel. v1 ships playable via a /research chat command in TheaterCommands.cs (mirroring /dev); v1.1 adds a copied ProductionPalette-style widget.
- Match-light vs campaign-heavy is a LobbyPrerequisiteCheckbox toggle plus pure-data ruleset swaps (tech count/cost/income, RequiresResearch tier chains); same C#, different YAML; campaign persistence is an additive map-Lua concern, no engine change.
- Completion is sticky and never resets on owner-change, avoiding the capture-tech edge cases explicitly deferred in theater.yaml.

## Files to touch
- OpenRA.Mods.Common/Traits/Player/ResearchManager.cs (new — ITick income + IResolveOrder StartResearch + ITechTreePrerequisite unlock + ISync)
- OpenRA.Mods.Common/Traits/ProvidesResearch.cs (new — trivial ConditionalTrait with int Points; structures/nodes feed research income)
- mods/ra/rules/theater-research.yaml (new — opt-in: ResearchManager Techs, ProvidesResearch on a Research Lab, Buildable/GrantConditionOnPrerequisite/SupportPower prerequisite wiring, LobbyPrerequisiteCheckbox@RESEARCH)
- mods/ra/fluent/theater.ftl (add checkbox-research + research tech name/description fluent strings)
- OpenRA.Mods.Common/Commands/TheaterCommands.cs (add /research <id> chat command issuing the StartResearch order — v1 playable UI, mirrors the existing /dev order at line 86)
- OpenRA.Mods.Common/Widgets/Logic/Ingame/ResearchPaletteLogic.cs + ProductionPaletteWidget-derived ResearchPalette (new, v1.1 — model after Logic/Ingame/ProductionTabsLogic.cs)
- mods/ra/chrome/ingame-player.yaml (v1.1 — host the research palette widget behind the THEATER opt-in)
- mods/ra/mod.yaml (add theater-research.yaml to Rules when validating with --check-yaml; revert after, per CLAUDE.md opt-in workflow)

## Risks
- TechTree.Update() is O(watchers x owned-prereqs) and re-gathers all player prerequisites; calling it on every research completion is fine (rare event), but do NOT call it every tick — only on Complete(), matching how SupportPowerManager/ProvidesPrerequisite nudge it.
- IncomePerInterval iterates world.ActorsWithTrait<ProvidesResearch>() every interval; cheap at THEATER node counts but should stay interval-gated (not per-tick) and avoid LINQ allocations in the hot path, like SupplyNetwork's parallel-list approach, if scaled up.
- Determinism: the StartResearch order MUST be a synced order resolved in ResolveOrder (like DevMode orders), never applied directly from UI/unsynced code; points/activeCost/interval must be [Sync] so desyncs surface. Chat-command path in TheaterCommands must issue an Order, not mutate the trait directly.
- ProvidesResearch reading IsTraitDisabled / power state must use synced state only (no UI/wall-clock); model the disabled check on ConditionalTrait, not on render or local-power UI.
- Build-palette refresh: confirm the production palette re-queries Buildable prerequisites live on TechTree change (it does, via the watcher callbacks) — but verify newly-unlocked buildables actually appear mid-match in a GUI run; the fun/snappiness gate needs an interactive playtest, not headless.
- UI scope creep: the v1.1 copied palette widget is the largest single chunk of work; v1 should ship on the /research chat command first to de-risk and validate the loop before investing in chrome.
- Campaign persistence (writing completed across missions) is out of scope for the engine trait and must be handled in map Lua / mission state; don't bake save/load into ResearchManager in v1.

## Design

## THEATER Research / Tech-Era System — Design v1

### Core thesis
Reuse, don't reinvent. OpenRA already has a battle-tested gate: **any trait that implements `ITechTreePrerequisite` and returns prerequisite strings from `ProvidesPrerequisites` feeds the player's `TechTree`** (`OpenRA.Mods.Common/Traits/Player/TechTree.cs:80`). Every consumer in the engine — `Buildable.Prerequisites`, `GrantConditionOnPrerequisite`, `SupportPowerManager` (`SupportPowers/SupportPowerManager.cs:66`) — already re-checks prerequisites live and reacts to changes. So **"a research completes" just needs to flip a string from absent to present on the player.** That is the entire integration surface. No changes to ProductionQueue, Buildable, or any consumer.

This matches the THEATER design rule already validated in `mods/ra/rules/theater.yaml`: research grants VERBS (new buildables/abilities), implemented as prerequisites, not passive stat auras.

---

### 1. The mechanism — where points come from (deterministic, integer-only)

A single player-actor trait `ResearchManager` (one per player, like `TechTree`, `SupportPowerManager`, `DeveloperMode`). It is `ITick` and accumulates **integer** research points. Determinism is automatic: it mirrors the exact pattern of `CashTrickler`/`DevelopsWhileHeld`/`SupplyNetwork` (all integer counters, `[Sync]` on mutable state, no float/Random/DateTime).

Two stacking, data-driven income sources, both integer:

- **Passive base rate** — `PointsPerInterval` every `Interval` ticks (e.g. 1 point / 25 ticks). Set to 0 to make research purely structure-driven.
- **Structure-driven rate** — actors carrying a tiny companion trait `ProvidesResearch` (Info field `Points`) contribute their `Points` to the owner's per-interval income while alive/owned/powered. The manager sums them each interval via `world.ActorsWithTrait<ProvidesResearch>()` filtered to the owner. This makes a **Research Lab** structure (and capturable territory nodes — reuse the THEATER `SupplyNode`/Forward-Command idea) the economy of research. Lose the lab, lose the income; deterministic because it reads the synced actor list, not wall-clock.

A research is **queued** by player order, then **progressed** by spending accumulated points on an integer countdown (`remaining -= incomePerTick`-style, exactly like `SupportPowerInstance` sub-tick accounting at `SupportPowerManager.cs:208`). One active research at a time in v1 (snappy, readable); `QueueCapacity` field allows N parallel later.

Income/spend is all `int`. The only synced mutable fields are `points`, the active research id, and `remainingCost` — mark them `[Sync]`/`[VerifySync]`.

---

### 2. How a completed research grants an unlock (maps to existing prerequisite/condition system)

`ResearchManager` implements `ITechTreePrerequisite`:

```csharp
public IEnumerable<string> ProvidesPrerequisites => completed;   // e.g. {"research.drone_swarm", "research.reactive_armor"}
```

When a research finishes, its id is added to a `completed` set and the manager calls `self.Owner.PlayerActor.Trait<TechTree>().Update()` (the same nudge `SupportPowerManager` and `ProvidesPrerequisite` use). TechTree re-gathers owned prerequisites (`TechTree.cs:73`) and fires `PrerequisitesAvailable` to every watcher. From there, **all three unlock styles work with no new code:**

- **Unlock a unit/building** → put `research.<id>` in that actor's `Buildable.Prerequisites` (identical to the existing `4TNK: Buildable: Prerequisites: theater.techlab` at `theater.yaml:85`). The build palette enables it the instant research completes.
- **Unlock an upgrade (stat/behavior buff)** → `GrantConditionOnPrerequisite` on the target actors with `Prerequisites: research.<id>`, `Condition: <id>` (`Conditions/GrantConditionOnPrerequisite.cs`). Existing condition consumers (`RequiresCondition` on Armament, Mobile speed mods, etc.) light up automatically. This is exactly how `CTRLCASH` gates its developed-tier CashTrickler in `theater.yaml:106`.
- **Unlock an ability/support power** → add `research.<id>` to the support power's `Prerequisites`; `SupportPowerManager` already gates charge on it (`SupportPowerManager.cs:66`, `SupportPowerInstance.Disabled` at line 153).

Because completion is *sticky* and *non-persistent within the match* (no owner-change reset), it's safe — no capture-tech edge cases like the deferred ones noted in `theater.yaml:13`.

---

### 3. The UI — lowest-effort path that looks good

**Recommendation: a Research palette that reuses the support-power widget look, NOT a new from-scratch panel and NOT a real ProductionQueue.**

Rationale from the code:
- A real `ProductionQueue` is the wrong fit — it produces *actors* with exits/placement and carries ~40 notification/limit fields (`ProductionQueue.cs:24-120`). Research produces a *prerequisite string*, not an actor. Bending the queue to "produce nothing" fights the trait.
- The **support-power command panel is the perfect analog**: it already renders a grid of icons with a circular charge/cooldown sweep, a ready flash, and click-to-activate, all driven by an integer `RemainingTicks` (`SupportPowerManager.cs:151`). Research = "icon + integer progress + click to start" — the same shape.

**Lowest-effort plan:**
1. v1 ships **functional with near-zero new chrome**: drive research entirely via a chat/console command in `TheaterCommands.cs` (`/research <id>`) that issues the `StartResearch` order on the player actor — mirroring how `/dev` issues `Orders.Sandbox` (`TheaterCommands.cs:86`). This is fully playable and testable headlessly-adjacent without any widget work.
2. v1.1 adds a small **ResearchPalette** by copying `ProductionPaletteWidget` + a thin `ResearchPaletteLogic` (model after `Logic/Ingame/ProductionTabsLogic.cs`): one row of research icons, a progress ring from `remainingCost/totalCost`, click issues the order. Drop it into `mods/ra/chrome/ingame-player.yaml` behind the same THEATER opt-in. Icons reuse the cameo art pipeline already in `design/art/`.

Use the `MakeKey`/`Register` watcher pattern (`GrantConditionOnPrerequisiteManager.cs:43`) if the palette needs to grey-out researches whose *own* prerequisites (a prior tech) aren't met yet — gives Civ-style tech tiers for free.

---

### 4. Small example tech tree (3-4 researches, wired end-to-end)

**Tech tree (THEATER flavor):**
- `research.drone_swarm` (250 pts) → unlocks Wing Loong Swarm Drone build.
- `research.reactive_armor` (300 pts) → upgrade: +HP/armor condition on all your tanks.
- `research.cruise_strike` (400 pts, **requires** `research.drone_swarm`) → unlocks a support power.

**C# trait sketch (new file `OpenRA.Mods.Common/Traits/Player/ResearchManager.cs`):**

```csharp
[TraitLocation(SystemActors.Player)]
[Desc("THEATER deterministic research. Accumulates integer points, runs one tech at a time,",
      "and emits research.<id> prerequisites on completion. Attach to the player actor.")]
public class ResearchManagerInfo : TraitInfo, Requires<TechTreeInfo>, ITechTreePrerequisiteInfo
{
    [Desc("Ticks between point payouts.")] public readonly int Interval = 25;
    [Desc("Passive points granted every Interval (0 = structure-driven only).")] public readonly int PointsPerInterval = 1;

    [FieldLoader.Require]
    [Desc("Tech definitions: Id, Cost (points), and optional RequiresResearch ids.")]
    public readonly Dictionary<string, ResearchDef> Techs = new();

    // ITechTreePrerequisiteInfo lets lint/UI know the full set of strings we can ever provide.
    IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
        => Techs.Keys.Select(k => "research." + k);

    public override object Create(ActorInitializer init) { return new ResearchManager(init, this); }
}

public class ResearchManager : ITick, IResolveOrder, ITechTreePrerequisite, INotifyCreated, ISync
{
    readonly ResearchManagerInfo info;
    readonly List<string> completed = new();           // -> research.<id> strings
    TechTree techTree;

    [Sync] int points;
    [Sync] int interval;
    [Sync] int activeCost;                              // 0 = nothing in progress
    string active;                                      // tech id being researched

    public ResearchManager(ActorInitializer init, ResearchManagerInfo info) { this.info = info; }

    void INotifyCreated.Created(Actor self) { techTree = self.Trait<TechTree>(); }

    // The universal gateway: TechTree gathers these and gates everything off them.
    public IEnumerable<string> ProvidesPrerequisites => completed;

    int IncomePerInterval(Actor self)
    {
        var sum = info.PointsPerInterval;
        // PERF/determinism: synced actor list, integer sum, no LINQ allocs in hot path if needed.
        foreach (var pr in self.World.ActorsWithTrait<ProvidesResearch>())
            if (pr.Actor.Owner == self.Owner && pr.Actor.IsInWorld && !pr.Actor.IsDead && !pr.Trait.IsTraitDisabled)
                sum += pr.Trait.Points;
        return sum;
    }

    void ITick.Tick(Actor self)
    {
        if (--interval > 0) return;
        interval = info.Interval;

        points += IncomePerInterval(self);

        if (active == null || activeCost == 0) return;

        // Spend accumulated points into the active research (integer countdown).
        if (points >= activeCost) { points -= activeCost; Complete(self); }
        else { activeCost -= points; points = 0; }
    }

    void Complete(Actor self)
    {
        completed.Add("research." + active);
        active = null; activeCost = 0;
        techTree.Update();                              // nudge -> unlocks fire instantly
    }

    public void ResolveOrder(Actor self, Order order)
    {
        if (order.OrderString != "StartResearch") return;
        var id = order.TargetString;
        if (active != null || completed.Contains("research." + id)) return;
        if (!info.Techs.TryGetValue(id, out var def)) return;

        // Tier gate: required prior techs must be done (reuse TechTree string check).
        foreach (var req in def.RequiresResearch)
            if (!completed.Contains("research." + req)) return;

        active = id; activeCost = def.Cost;
    }
}
```

(Companion trait — `ProvidesResearch : ConditionalTrait` with one `int Points` field, attached to the Research Lab / capturable node; no logic, the manager reads `.Points`.)

**YAML — opt-in ruleset `mods/ra/rules/theater-research.yaml`:**

```yaml
Player:
  ResearchManager:
    Interval: 25
    PointsPerInterval: 0          # structure-driven economy
    Techs:
      drone_swarm:    { Cost: 250 }
      reactive_armor: { Cost: 300 }
      cruise_strike:  { Cost: 400, RequiresResearch: [drone_swarm] }
  # Match-light vs campaign-heavy toggle (see §5):
  LobbyPrerequisiteCheckbox@RESEARCH:
    ID: research
    Label: checkbox-research
    Prerequisites: research.enabled

# Research Lab structure feeds the economy (reskin an existing building).
RESEARCHLAB:
  Inherits: ATEK
  ProvidesResearch:
    Points: 2

# Unlock styles — all existing plumbing:
WINGLOONG:                         # (1) buildable unit
  Buildable:
    Prerequisites: research.drone_swarm

3TNK:                              # (2) upgrade via condition
  GrantConditionOnPrerequisite@REACTIVE:
    Condition: reactive-armor
    Prerequisites: research.reactive_armor
  Armor:                           # consumes the condition with RequiresCondition (existing field)
    # ...tier-2 armor variant gated on reactive-armor

# (3) ability — add research.cruise_strike to a SupportPower's Prerequisites (existing field).
```

End to end: lab grants +2 pts/interval → player issues `StartResearch drone_swarm` (chat or palette) → 250 pts later `ProvidesPrerequisites` gains `research.drone_swarm` → TechTree fires → Wing Loong appears in the War Factory palette. No consumer code touched.

---

### 5. Match-light vs campaign-heavy toggle

Two complementary switches, both already supported:

- **On/off per match:** the `LobbyPrerequisiteCheckbox@RESEARCH` above. When unchecked, `research.enabled` is never granted, so you can additionally gate the whole subsystem (e.g. make Techs themselves require `research.enabled`, or just gate the palette's visibility). Matches default OFF → vanilla-fast THEATER; flip ON for the 4X layer. This is the same mechanism THEATER already uses for `@DEVUNLOCK` (`theater.yaml:30`).
- **Light vs heavy is pure data:** match-light = small `Techs` set, low `Cost`, high `PointsPerInterval` → 1-2 minute techs, "snappy." Campaign-heavy = a fat tech tree with `RequiresResearch` chains (tiers), higher costs, structure-only income, and (campaign only) persistence by writing `completed` into the mission Lua / map state. Because the trait reads everything from `*Info`, the *same C#* serves both; only the ruleset differs (overlay an alternate `theater-research-campaign.yaml`).

The non-persistence within a match (`completed` never resets on owner-change, lives only on the player actor) keeps the match layer light exactly as the validated THEATER principle demands; campaign persistence is an additive map-script concern, not an engine change.

---

### Why this is the good parts of Civ/RoN/Stellaris without the slow parts
- **Snappy:** one active research, visible integer progress ring, completion = immediate new VERB. No multi-turn waiting; payoff is a build-menu/ability change you act on this fight.
- **Deterministic:** integer points, integer countdown, synced actor reads — identical to CashTrickler/SupplyNetwork which already run in lock-step.
- **Cheap to build:** ~1 real new trait (+1 trivial companion). Zero changes to ProductionQueue/Buildable/SupportPower — the unlock side is 100% existing, proven plumbing.