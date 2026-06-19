# Deterministic Diplomacy System for THEATER (dynamic player relations beyond stock ally/enemy)

**Executable now:** True

**Summary:** The engine already routes ALL combat, auto-target, force-fire, and shroud-reveal decisions through a single chokepoint — Player.RelationshipWith(other), which reads only the mutable AlliedPlayersMask/EnemyPlayersMask LongBitSets. Nothing in this branch ever mutates those masks after world load (no runtime ally order, not even in scripting), so a new player-actor trait that flips the masks via networked orders gets truce/shared-vision "for free" through existing relationship checks. The only real engine work is forcing shroud/frozen-actor re-evaluation on a relationship change (AffectsShroud only refreshes on range/disable change) and a 4-state overlay (War/Truce/Trade/Allied) on top of the binary mask; the proposal/accept UI is the genuinely hard part, everything else is executable now.

## Key decisions
- Single chokepoint reuse: mutate Player.AlliedPlayersMask/EnemyPlayersMask and let the existing RelationshipWith()-based checks in AttackBase/AutoTarget/Armament/RevealsShroud/FrozenUnderFog deliver truce + shared-vision for free — no new effect wiring
- 4 diplomatic levels (War/Truce/Trade/Allied) layered on top of the binary engine relationship: War=Enemy, Truce/Trade=Neutral, Allied=Ally
- Authority is ONE world-actor trait (DiplomacyManager) holding levels+proposals in deterministic pair-keyed dictionaries; order resolution lives on the player actor so order.Player identifies the issuer
- Propose/Accept order pair carries target player InternalName in TargetString and level in ExtraData, issued on PlayerActor like DeveloperMode — mutual consent to improve relations, cooldown-gated unilateral war
- Shared vision requires forcing AffectsShroud re-evaluation on relationship change (its Tick only refreshes on range/disable change) via a new INotifyRelationshipChanged or an explicit source refresh + FrozenActorLayer.RefreshState
- Trade = integer credit transfer on an ITick interval using PlayerResources.TakeCash/GiveCash; no float
- Ship a chat-command MVP (/ally /truce /trade /war /accept via TheaterCommands) first since the full diplomacy widget is the only hard/risky part
- AI is safe by construction: bots never issue or auto-accept diplomacy orders, so their lobby-assigned relationships are never mutated

## Files to touch
- /Users/ashishnaik/Projects/openra/OpenRA.Mods.Common/Traits/Player/Diplomacy.cs (NEW — DiplomacyManager world-actor trait: levels, proposals, mask flips, trade ITick)
- /Users/ashishnaik/Projects/openra/OpenRA.Mods.Common/Traits/Player/DiplomacyClient.cs (NEW — IResolveOrder on player actor: Propose/Accept/Cancel/DeclareWar)
- /Users/ashishnaik/Projects/openra/OpenRA.Game/Traits/TraitsInterfaces.cs (add INotifyRelationshipChanged interface near PlayerRelationship enum)
- /Users/ashishnaik/Projects/openra/OpenRA.Mods.Common/Traits/AffectsShroud.cs (implement INotifyRelationshipChanged -> UpdateShroudCells to enable shared vision on change)
- /Users/ashishnaik/Projects/openra/OpenRA.Mods.Common/Commands/TheaterCommands.cs (MVP: register /ally /truce /trade /war /accept chat commands that issue the diplomacy orders)
- /Users/ashishnaik/Projects/openra/mods/ra/rules/theater.yaml (opt-in: World: DiplomacyManager + Player: DiplomacyClient)
- /Users/ashishnaik/Projects/openra/mods/ra/fluent/theater.ftl (proposal/accept/notification + command-hint strings)
- /Users/ashishnaik/Projects/openra/OpenRA.Mods.Common/Widgets/Logic/DiplomacyPanelLogic.cs (NEW, optional full-UI phase — proposal list, level dropdown, accept/withdraw)

## Risks
- UI is the hard part: a full proposal/inbox diplomacy panel needs new chrome + a WidgetLogic class and iteration; the chat-command MVP sidesteps this entirely and is recommended as step one
- Shared-vision will silently no-op unless shroud sources are force-refreshed on a relationship change — AffectsShroud.Tick does NOT re-check relationships (only range/disabled). Must add INotifyRelationshipChanged or an explicit refresh, plus FrozenActorLayer.RefreshState/Invalidate, or allied vision won't appear
- Determinism: proposal/level dictionaries must iterate in a stable, player-index-derived order and never key on object hash or wall-clock; pair keys should be ordered (minIndex,maxIndex) to avoid divergence
- Malformed/stale orders (targeting a dead, spectating, or NonCombatant player, or accepting a non-existent proposal) must be validated in ResolveOrder and rejected, or they risk desync/exceptions
- Truce-snipe / griefing in multiplayer: declaring war immediately after a truce must be cooldown-gated (WarmupTicks) — already in design but needs tuning
- Lobby teams interaction: diplomacy should only open up relations beyond the lobby baseline and require explicit player action to break a fixed team, to avoid surprising team-game behavior
- Mutating masks at runtime is a path this branch has never exercised (masks were frozen post-load); needs a GUI playtest to confirm no cached relationship assumptions elsewhere (e.g. palette/stance colors via SetupRelationshipColors) go stale — color refresh may be desired on change

## Design

## THEATER Diplomacy — deterministic dynamic relations

### What the engine already gives us (reuse, don't rebuild)

The decisive finding: **every combat and vision decision funnels through one method**, `Player.RelationshipWith(Player other)` (`OpenRA.Game/Player.cs:249`), which returns `Ally/Enemy/Neutral` by testing two mutable fields:

```csharp
// Player.cs:91-93  (NOT readonly — safe to mutate at runtime)
public LongBitSet<PlayerBitMask> AlliedPlayersMask = default;
public LongBitSet<PlayerBitMask> EnemyPlayersMask = default;
```

Consumers that already do the right thing once masks change — **no new wiring needed**:
- **Can't fire on each other:** `AttackBase.cs:383` gates targeting on `(forceAttack ? ForceTargetRelationships : TargetRelationships).HasRelationship(self.Owner.RelationshipWith(owner))`. Armament default `TargetRelationships = Enemy` (`Armament.cs:72`). Flip a pair from Enemy→Neutral and auto-target/force-fire stop instantly.
- **Auto-target:** `AutoTarget.cs` uses the same `RelationshipWith` path (and `UnitStance` HoldFire/Defend/etc. is a *separate, per-unit* concern — leave it alone).
- **Shared vision:** `RevealsShroud.cs:52` adds cells to player *p* only if `info.ValidRelationships (=Ally) .HasRelationship(self.Owner.RelationshipWith(p))`. Make two players Ally and their vision merges automatically.
- **Frozen-under-fog reveal of allied actors:** `FrozenUnderFog.cs:129`, `RevealsMap`, `CreatesShroud` — all relationship-driven.

**Critical gotcha (the one real engine change for vision):** `AffectsShroud.Tick` (`AffectsShroud.cs:109-124`) only re-runs `UpdateShroudCells` when *range* or *disabled* changes — it does **not** re-check relationships per tick. So flipping masks alone will not move existing shroud sources. A relationship change must **force a re-evaluation of every shroud source** for the affected players (see below).

There is **no `INotifyRelationshipChanged` interface and no runtime ally order in this branch** — masks are set once in `CreateMapPlayers.SetupPlayerMasks` (`CreateMapPlayers.cs:142`) and frozen. Our trait becomes the sole authority for runtime mutation, so we don't fight existing code.

### 1. Relationship model & transitions

A **4-state diplomatic level per ordered/unordered player pair**, stored on a single world-actor trait (one authority, deterministic iteration order):

| State | Engine relationship it maps to | Meaning |
|---|---|---|
| `War` | Enemy | default vs non-team players; shoot on sight |
| `Truce` | Neutral | ceasefire — no auto-target, can't force-fire |
| `Trade` | Neutral | Truce + periodic resource transfer |
| `Allied` | Ally | Truce + shared vision (+ optionally shared control later) |

Mask mapping on apply (deterministic, integer-only):
- `War`: each side `EnemyPlayersMask = EnemyPlayersMask.Union(other.PlayerMask)` and `Except` from Allied.
- `Truce`/`Trade`: `Except` other from **both** masks → falls through to `Neutral` in `RelationshipWith`.
- `Allied`: `AlliedPlayersMask = Union(other.PlayerMask)`, `Except` from Enemy.

`LongBitSet.Union/Except/Overlaps` (`LongBitSet.cs:184/169/144`) are pure, allocation-light, integer set ops — fully deterministic.

**Transitions** are symmetric and require mutual consent to *improve*, unilateral to *worsen*:
- Improving (War→Truce→Trade→Allied): **propose + accept** (two orders).
- Declaring War (any→War): **unilateral**, but gated by a `WarmupTicks` cooldown after a truce was signed (e.g. 1500 ticks ≈ 60s) so you can't truce-snipe. Cooldown is an integer tick counter on the trait, compared against `world.WorldTick`.
- Match-light default: everyone starts at lobby-derived War/Allied; diplomacy only *opens up* what teams already fixed, it never silently breaks a lobby team unless the player explicitly declares war.

### 2. Propose / Accept — order pair + UI

Two networked orders issued on the **local player's `PlayerActor`** (mirrors `DeveloperMode` and `TheaterCommands.IssueDevOrder`, `TheaterCommands.cs:86`), carrying the *target player's `InternalName`* in `TargetString` and the desired level in `ExtraData`:

```csharp
public static class DiplomacyOrders
{
    public const string Propose = "DiploPropose"; // TargetString=target player, ExtraData=(uint)DiploLevel
    public const string Accept  = "DiploAccept";  // accept the pending proposal from TargetString
    public const string Cancel  = "DiploCancel";  // withdraw a proposal you made
    public const string DeclareWar = "DiploWar";  // unilateral downgrade to War
}
```

Resolution (in `IResolveOrder.ResolveOrder`, runs inside `world.Tick`, so deterministic):
1. **Propose:** record a `PendingProposal{from, to, level}` in a deterministic dictionary keyed by ordered pair. No state change yet. Notify the *target's* local client via `TextNotificationsManager` (UI is local-only; the recorded proposal is the synced truth).
2. **Accept:** validate a matching pending proposal exists for `(order.Player → accepting player)`; if so, call `SetLevel(a, b, level)` which performs the symmetric mask flip on **both** players + clears the proposal + stamps cooldown. Idempotent if already at that level.
3. **DeclareWar:** unilateral; checks `WarmupTicks` cooldown, then sets War both ways.

Validate everything against `world.Players` (lookup by `InternalName`); reject orders referencing spectators/`NonCombatant`/dead players so a malformed order can't desync.

**UI (the hard part):** a lightweight `DiplomacyPanelLogic` widget. Two viable scopes:
- **MVP / chat-driven (zero new chrome):** extend `TheaterCommands` with `/ally <player>`, `/truce <player>`, `/trade <player>`, `/war <player>`, `/accept <player>`. Resolves player by fluent name (the file already does fuzzy faction matching at `TheaterCommands.cs:98`). Ships in a day, fully functional, no widget risk. **Recommend this first.**
- **Full panel:** a side panel listing each opponent with their current state, a dropdown of proposable levels, and Propose/Accept/Withdraw buttons + an inbox of incoming proposals. Models on `ObserverShroudSelectorLogic`/player-list widgets under `OpenRA.Mods.Common/Widgets/Logic/`. This is where most effort/iteration goes.

### 3. Effects wired to existing systems

- **Truce (no fire):** achieved purely by the Neutral mask state — `AttackBase`/`AutoTarget`/`Armament` already respect it. **Zero new code.** (Optional polish: a one-shot order to also flip in-flight `Attack` activities to cancel, but cancellation falls out naturally since the target stops being valid.)
- **Shared vision (Allied):** flip masks to Ally, then **force shroud re-evaluation**. Cleanest deterministic hook: add a tiny public `RefreshAllShroud()` path. Two options, both integer-safe:
  - (a) Add an `INotifyRelationshipChanged` interface fired by `SetLevel`, implemented by `AffectsShroud` to call its existing `UpdateShroudCells(self)` — surgical, mirrors how `INotifyMoving` already triggers it.
  - (b) Cheaper, no interface: in `SetLevel`, iterate `world.ActorsWithTrait<AffectsShroud>()` and bump a version so their next `Tick` refreshes; plus call `FrozenActorLayer.RefreshState()` / `Invalidate()` (`FrozenActorLayer.cs:120/194`) for the two players. (a) is cleaner; either is deterministic.
- **Trade (periodic transfer):** an `ITick` on the world-actor diplomacy trait. Every `TransferInterval` ticks (integer), for each `Trade` pair move `TransferAmount` credits A→B and B→A (or net flow per design) using `PlayerResources.TakeCash` / `GiveCash` (`PlayerResources.cs:220/168`). All integer, no float. Skip if payer can't afford (graceful, deterministic).

### 4. AI handling & match-light vs campaign-heavy

- **Skirmish AI won't break:** AI never *issues* diplomacy orders, and never *accepts* by default → it simply ignores proposals, staying at its lobby-assigned War/Allied. Because we never mutate masks without an explicit accepted order, the AI's relationships are untouched. A human proposing truce to a bot gets no accept (treated as rejected after a timeout) — correct, safe behavior. Optionally a `DiplomacyBotModule` can auto-accept truce when losing badly (a later, opt-in nicety).
- **Match-light (default, this slice):** non-persistent, lobby seeds the start state, only the 4 levels above, cooldown-gated war, no tech/treaty trees. Lives entirely in the opt-in `theater.yaml` ruleset so stock RA is untouched.
- **Campaign-heavy (later):** the same trait + orders, but persisted, with treaty terms (tribute over N turns, non-aggression timers, vision-only vs full-ally), AI personalities, and Lua hooks (`scripts/`) for scripted betrayals. The order/trait core is identical; only policy/persistence grows.

### 5. C# sketch — new code vs stock reuse

**New (3 files):**

```csharp
// OpenRA.Mods.Common/Traits/Player/Diplomacy.cs  — the authority, on the World actor
[TraitLocation(SystemActors.World)]
public class DiplomacyManagerInfo : TraitInfo
{
    public readonly int TruceWarmupTicks = 1500;   // can't declare war right after truce
    public readonly int TradeInterval = 50;        // ticks between transfers
    public readonly int TradeAmount = 25;          // credits each side, per interval
    public override object Create(ActorInitializer init) => new DiplomacyManager(this);
}

public class DiplomacyManager : ITick // (proposals/levels live here; not on per-player actor)
{
    enum DiploLevel { War, Truce, Trade, Allied }
    // deterministic: pair key = ordered (minIndex, maxIndex) into world.Players
    readonly Dictionary<(int, int), DiploLevel> levels = new();
    readonly Dictionary<(int, int), (int from, DiploLevel level)> pending = new();
    readonly Dictionary<(int, int), int> truceSince = new();   // WorldTick stamp

    public void SetLevel(World w, Player a, Player b, DiploLevel lvl) { /* flip masks + refresh shroud + frozen layer */ }
    void ITick.Tick(Actor self) { /* trade transfers on interval */ }
}

// Order resolution lives on the PLAYER actor so order.Player is the issuer:
// OpenRA.Mods.Common/Traits/Player/DiplomacyClient.cs  (IResolveOrder)
//   routes Propose/Accept/Cancel/War into world.WorldActor.Trait<DiplomacyManager>()

// OpenRA.Mods.Common/Widgets/Logic/DiplomacyPanelLogic.cs  (UI; or skip for chat-MVP)
```

**Mask flip helper (the core deterministic op):**
```csharp
static void SetEnemy(Player p, Player q) {
    p.EnemyPlayersMask = p.EnemyPlayersMask.Union(q.PlayerMask);
    p.AlliedPlayersMask = p.AlliedPlayersMask.Except(q.PlayerMask);
}
static void SetNeutral(Player p, Player q) {
    p.EnemyPlayersMask = p.EnemyPlayersMask.Except(q.PlayerMask);
    p.AlliedPlayersMask = p.AlliedPlayersMask.Except(q.PlayerMask);
}
static void SetAlly(Player p, Player q) {
    p.AlliedPlayersMask = p.AlliedPlayersMask.Union(q.PlayerMask);
    p.EnemyPlayersMask = p.EnemyPlayersMask.Except(q.PlayerMask);
}
```

**One small engine change** in `OpenRA.Game`/`AffectsShroud`: add `INotifyRelationshipChanged` (to `Traits/TraitsInterfaces.cs`) and have `AffectsShroud` re-run `UpdateShroudCells` on it; `SetLevel` fires it for the two players. (Or the no-interface refresh variant in §3.)

**Stock reused as-is:** `RelationshipWith`, `AlliedPlayersMask/EnemyPlayersMask`, `LongBitSet`, `AttackBase`/`Armament`/`AutoTarget` targeting gates, `RevealsShroud`/`FrozenUnderFog` vision gates, `PlayerResources.TakeCash/GiveCash`, the `Order`→`IResolveOrder` networked pipeline, `TextNotificationsManager`, `ChatCommands` registration.

### YAML wiring (opt-in, theater.yaml)
```yaml
World:
  DiplomacyManager:
    TruceWarmupTicks: 1500
    TradeInterval: 50
    TradeAmount: 25
Player:
  DiplomacyClient:
```

### Validation path on this machine
`make` (Release, analyzer-light), then add `DiplomacyManager`/`DiplomacyClient` only to the slice map's rules and run `./utility.sh ra --check-yaml`. Determinism is satisfied: all state is integer (`LongBitSet`, tick stamps, credit ints), all mutation flows through networked orders resolved inside `world.Tick`, no float/Random/DateTime.