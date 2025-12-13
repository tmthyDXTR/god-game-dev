<!-- AUTO_NAV_START -->
<nav>
<ul style="list-style:none; padding:0; display:flex; gap:1rem;">
  <li><a href="card-brainstorm.html">Card Brainstorm</a></li>
  <li><a href="index.html">Index</a></li>
  <li><a href="localizationSetup.html">LocalizationSetup</a></li>
  <li><a href="population-design.html">Population Design</a></li>
  <li><a href="proto-brainstorm.html">Proto Brainstorm</a></li>
</ul>
</nav>
<!-- AUTO_NAV_END -->



# Emergent Population & Colony Mechanics — Design

This document sketches a practical, implementable design for emergent population mechanics (Dwarf Fortress / RimWorld style) tailored to this project and its core influences (Dark Souls, Dwarf Fortress, Darkest Dungeon, Spirit Island, Slay the Spire, The Settlers, Gods Will Be Watching, Jagged Alliance, etc.). It integrates with the existing hex-grid systems (`HexTile`, `SelectionManager`, card system) and the prototype vision in `proto-brainstorm.md` and `summary.md`.

---

## Goals
- Create many simple agents with needs, priorities and local rules so complex drama emerges from interactions and environmental constraints.
- Keep systems modular so features (reproduction, disease, factions) can be enabled progressively.
- Integrate tightly with existing tile resources, card-driven actions and the non-destructive highlighting system.
- Preserve the project’s thematic pillars: harsh consequences, persistent scars (perma effects), and emotional weight from choices.

- Treat the god / god-beast as a first-class actor (hero) in the simulation: it has health, abilities, and a new resource "Faith" that functions similarly to food/hunger but for divine power.

---

## High-level systems
1. PopulationManager — single authoritative simulation tick controller.
2. Agent component — lightweight entity representing one person/GC (traits, needs, state machine, inventory).
3. Needs & Drives — hunger, fatigue, social, mood: decay over time and drive behavior.
4. Job/Task system — WorkOrders created by world/cards/players; agents claim tasks and reserve tiles/resources.
5. HexTile extensions — tile-level storage (food, workerCapacity, reservedBy, zoneTag, productionRate).
6. Pathfinding & Reservation — A* on hex graph + tile reservations to avoid double allocation.
7. Events & Card hooks — cards create WorkOrders, modify zones, or spawn agents; highlights use owner-tracked system (`CardTarget`).
8. God / God-Beast actor — a special actor with stats (health, faithReserve, devotion) that interacts with population and tiles.

---

## Why these systems
- Central tick keeps simulation deterministic and easy to debug.
- Agents remain cheap (simple rules), emergent complexity appears from many interacting agents and resource constraints.
- Job system decouples who needs something done from who does it — enables scaling and emergent prioritization.
- Tile reservations prevent race conditions and create meaningful coordination problems (e.g., two agents try to harvest same tile).

---

## Data model sketches
These are initial fields and example APIs to implement.

  - int workerCapacity
  - Dictionary<Role,int> workers
  - float foodStored
  - float morale (0..1)
  - float disease (0..1)
  - string reservedBy (or Dictionary<string,reservationMeta>)
  - ZoneTag zoneTag (None, Stockpile, Field, Home, Workshop)
  - float productionRate
  - Settlement info (optional):
    - bool hasSettlement
    - string settlementId
    - SettlementType settlementType (None, MobileCamp, StationaryVillage)
    - int settlementLevel (affects workerCapacity and productionRate)
    - bool isMobile (true for camps that can relocate)
    - string settlementOwner (which caravan, god, or faction)
    - float constructionProgress
    - int garrisonCount

  - Notes: settlement fields let tiles host either a temporary camp that can move with the caravan, or a permanent village that provides steady production and worker slots.

- Agent (component):
  - string id, string name
  - Traits: speed, maxStamina, greed, sociability, laborSkill
  - Needs: float hunger, float fatigue, float social, float mood
  - Inventory: Dictionary<ResourceType,int>
  - State: Idle/Moving/Working/Returning/Sleeping/Fleeing
  - homeHex: Hex
  - currentTaskId
  - public void Tick(float dt)
  - public void EvaluateAndClaimWork(List<WorkOrder> available)

- WorkOrder:
  - string id
  - WorkType {Harvest, Build, Repair, Restock, BuildHome, Forage, Guard}
  - Hex tileTarget
  - requiredResources (Dictionary<ResourceType,int>)
  - reward (optional)
  - owner (player/card/AI)
  - priority
  - claimedBy
  - ticksToComplete (int)

- PopulationManager:
  - List<Agent> agents
  - Dictionary<Hex, List<Agent>> agentsByTile
  - PriorityQueue<WorkOrder> workQueue
  - TickRate (e.g. 0.5s or 1s)
  - void FixedTick()

 - GodBeast (new singleton/component):
   - float health
   - float faithReserve // accumulated faith currency
   - float devotion (global modifier from worshippers)
   - float faithDrainRate (passive decay per tick)
   - public bool SpendFaith(float amount)
   - public void GainFaith(float amount)
   - public void ApplyRitual(HexTile tile, float faithAmount)

---

## Simulation tick (example)
- PopulationManager.FixedTick() runs once per simulation tick:
  1. Decay agent needs (hunger += hungerRate)
  1b. Decay god faithReserve by `faithDrainRate` and apply passive effects; accumulate faith from worshipper attendance and rituals.
  2. Resolve per-agent short actions (consume local food if present, advance work progress)
  3. Evaluate & assign WorkOrders (agents choose highest-utility job: need reduction vs reward)
  4. Apply multi-turn job progress (reduce WorkOrder.ticksToComplete)
  5. Resolve tile turn: production, births/deaths, disease progression, migrations
  6. Emit events (starvation, birth, mass migration) and let UI/cards respond

Keep tick cheap: update a subset each frame if many agents exist (round-robin scheduling).

---

## God as Hero & Faith mechanics

- Faith is a persistent, spendable resource that represents devotion and spiritual energy. It is both a local resource (tile-level `localFaith`) and a global pool (`GodBeast.faithReserve`).
- Worshippers (agents/population) generate faith passively when they are in a tile with a shrine or when rituals are performed. Cards and actions can funnel faith into the god via `GodBeast.GainFaith(amount)`.
- Faith behaves similarly to hunger in that it decays and must be replenished. Unlike food, faith can be generated by social actions (rituals, sacrifices, festivals) and by keeping morale high.
- Faith uses:
  - Activate god abilities (heals, buffs, area effects) as `SpendFaith` operations.
  - Rituals that modify tile properties (clean blight, boost production, create pilgrimage routes).
  - Currency for card effects that require devotion (sacrifice cards, divine interventions).

Design notes:
- Faith encourages connecting population management to the god: protecting and feeding worshippers raises faith income; starving or sacrificing them creates moral and gameplay trade-offs.
- Track faith owners in highlights using `SelectionManager` owner "Faith" or "Ritual" so ritual-target highlights persist.

---

## Round / Tick hybrid design

The game can operate as a player-driven round system that resolves one or more simulation ticks per round. This matches the card-play feel while preserving an ongoing emergent simulation.

Two modes are recommended (configurable):

1) Round-Based (preferred for card systems)
- Structure: each round = 1 day. Phases per round:
  - Player Phase: player draws N cards, plays cards (move, assign, create WorkOrders, spend faith), issues orders to agents (via UI), and uses god abilities. Card and movement actions are resolved immediately as queued commands.
  - Action Resolution Phase: immediate actions (moving a few agents, starting tasks) run and set up multi-turn WorkOrders.
  - Simulation Phase: PopulationManager.FixedTick() runs once (or a small number of ticks) to resolve consumption, births, job progress, migrations, disease spread, and faith changes.
  - Event Phase: resolve events triggered by the simulation or cards (raids, plagues, festivals). Then loop to next round.

Advantages: deterministic, easy to reason about, aligns with card turns and UI.

2) Continuous Tick with Player Interrupts
- Structure: simulation ticks continuously at `simTick` (e.g., 1s), but the player can pause or trigger special god actions that consume faith. Cards are played in real-time or in a paused UI.
- Use when you want a more living world; requires careful UI for pausing and stepping.

Hybrid approach: default to Round-Based but allow a "Live Mode" in settings for tests or different gameplay styles.

API suggestions for round integration:
- `GameRoundManager.StartPlayerPhase()` — unlocks card play and movement, sets `roundTimer`.
- `GameRoundManager.EndPlayerPhase()` — locks input, commits queued actions to `ActionQueue`.
- `GameRoundManager.ResolveSimulationPhase()` — runs `PopulationManager.FixedTick()` and any queued action effects.
- `GodBeast.SpendFaith(amount)` can be called in Player Phase; consequences (immediate or deferred) are applied during Action Resolution or Simulation Phase depending on card.

Example round flow:
1. Player Phase: play ritual card on tile A (cost: 10 faith). `GodBeast.SpendFaith(10)` runs immediately and a `RitualWorkOrder` is queued with owner "CardTarget".
2. Action Resolution: agents assigned to the ritual claim the WorkOrder and begin. `SelectionManager` highlights ritual tile with owner "CardTarget" so hover won't override it.
3. Simulation Phase: `PopulationManager.FixedTick()` runs — rituals complete if ticksToComplete reaches 0, localFaith increases, `GodBeast.GainFaith()` accrues from localFaith conversion.

---

## Sample APIs & god-faith integration
- `GodBeast.Instance.GainFaith(float amount, HexTile source=null)`
- `GodBeast.Instance.SpendFaith(float amount)` returns bool success
- `PopulationManager.RegisterRitual(HexTile tile, float faithAmount, string owner)` creates a timed ritual WorkOrder that converts worshipper attendance into `GodBeast.faithReserve`.
- `HexTile.ReserveForRitual(string owner, int ticks)` // visual + reservation

---

## UX considerations
- Expose `GodBeast.faithReserve` and `localFaith` in UI; show faith income sources (worshippers, rituals, events).
- Ritual/faith highlights should use owner layers so card-level highlights persist when hovering.
- Make faith costs and consequences explicit on cards (e.g., "Spend 5 Faith: Heal Beast 10hp; Cost: 1 worshipper permanently").

---

## Next steps
- Finish integrating `GodBeast` component in code and wire `PopulationManager` to report faith income per tile.
- Add ritual WorkOrder types and tile-level `localFaith` accumulation.
- Implement the Round Manager APIs and wire Player Phase -> Simulation Phase flow.

If you want, I can start implementing the `GodBeast` component and the `GameRoundManager` skeleton next.

---

## Basic agent decision policy (utility-based)
- Compute utilities for candidate actions: forage, return home, sleep, claim job, wander.
- Utility = weight_by_need * (estimatedBenefit - costOfTravel - risk)
- Agents pick the action with highest positive utility; fallback to Idle/Wander.
- Jobs include reservations and timeouts to avoid deadlocks.

---

## Tile turn resolution (per-turn, uses your proto rules)
- Consumption: required = population * baseConsumption; consume from tile.foodStored; morale/damage if shortage.
- Production: farmers produce food & add to tile.foodStored.
- Disease & healing: disease grows with density; healers reduce disease.
- Births & deaths: births from birthRate*adults; deaths from disease/starvation.
- Overflow: spawn migrating band if population > capacity

These rules closely mirror the pseudocode in your `proto-brainstorm.md` for familiarity and easy reuse.

---

## Jobs & Reservation semantics
- WorkOrders are created by player (cards/SelectionManager), tile systems (plant/grow), or events.
- Agents see available WorkOrders via PopulationManager and can claim them.
- Claiming sets `WorkOrder.claimedBy` and calls `HexTile.Reserve(agentId)` on tileTarget.
- If an agent fails to reach tile in N turns, the claim expires (auto-unclaim) and high priority jobs can steal.

---

## Mobile vs Stationary Settlements (camps, villages, caravans)

This design distinguishes between temporary/mobile camps (that travel with the caravan/god-beast) and permanent stationary villages. Both are strategic choices with clear trade-offs.

Core concepts
- Mobile Camp (MobileCamp): small, light, low-cost, relocatable. Moves with the caravan or by explicit `MoveSettlement` action. Provides a small number of worker slots and short-term storage. Fast to construct and quick to dismantle.
- Stationary Village (StationaryVillage): larger settlement built on a tile. Higher worker capacity, production boosts, and persistent infrastructure (workshops, mills, shrines). Harder and slower to construct, expensive to dismantle, provides more long-term benefits and local defense.

Tile + settlement interactions
- A tile with `hasSettlement == true` gains worker slots and production modifiers based on `settlementLevel`.
- Mobile camps set `isMobile = true` and carry a small stockpile (subset of tile's `resourceAmounts`) that moves with the camp.
- Stationary villages anchor production and faith accrual (pilgrimages, festivals).

Mechanics & gameplay trade-offs
- Mobility vs Productivity: Mobile camps enable exploration and safety for the caravan (you can keep resources close to the beast), but they generate less food/faith than stationary villages. Stationary villages produce more but require defending and reduce caravan speed if the player wants to guard them.
- Defense vs Growth: Stationary villages can recruit guards and build defenses, reducing raiding/disease impact, but garrisons cost workers and reduce production for a time.
- Investment & Irreversibility: Building a village consumes resources and time (multi-turn build WorkOrder). Dismantling returns only a portion of materials and may cost faith or morale.
- Strategic Placement: Certain tiles provide bonuses to specific settlement types (river tiles boost farms, ruins boost salvage production, sacred tiles increase faith gains when a shrine is built).

Settlement lifecycle & actions
- `BuildSettlement(HexTile tile, SettlementType type, int level)` — creates a WorkOrder; completion sets `hasSettlement`, `settlementType`, `settlementLevel` and modifies tile stats.
- `UpgradeSettlement(string settlementId)` — increases `settlementLevel` to add worker slots/production.
- `DismantleSettlement(string settlementId)` — removes settlement, returns partial resources, may create refugees/migration.
- `MoveSettlement(string settlementId, HexTile destination)` — only valid for `isMobile == true` camps; moves camp and its carried stockpile to destination tile. Movement may consume turns and resource cost; moving into dangerous tiles risks damage or loss.
- `GarrisonSettlement(string settlementId, int numGuards)` — assign agents to garrison role, reducing local productivity but increasing defense.

Integration with God & Caravan movement
- Caravan with GodBeast: mobile camps can move together with the god-beast caravan. When the beast moves, mobile camp positions are updated (instant or with delay depending on design) — this models a traveling community living on the beast's back or in its shade.
- Leave-behind villages: players may choose to found a stationary settlement and leave part of the population there (garrison/workers). This creates a long-term holding that can produce resources while the caravan explores, but it exposes the village to raids and reduces immediate manpower.
- Pilgrimage & Faith flow: stationary villages attract pilgrims, increasing `localFaith` and `GodBeast.faithReserve` over time. Mobile camps generate less faith unless a ritual is performed.

Card & WorkOrder hooks
- Cards can create `BuildSettlement` or `SummonCamp` WorkOrders and/or instantly spend faith to accelerate builds.
- Example card: "Found Wayside Shrine" — instant creates a small shrine in tile (small faith income), costs resources and faith. Highlight uses owner "CardTarget" or "Ritual".

User experience and UI
- Map markers: settlements show an icon indicating MobileCamp vs StationaryVillage, level, garrison, and current stockpile.
- Quick actions on selected settlement: Upgrade, Dismantle, Move (if mobile), Assign Garrison, Prioritize Production, Set Trade Route.
- Alerts: if a left-behind village is under attack, the player receives an alert during the Player Phase so they can respond or accept the consequences.

Balance tips and tuning
- Make mobile camps cheap and fast but narrowly useful (short-term storage, scout safety). Stationary villages should be powerful but costly; early-game the player will rely on camps.
- Use travel-time, danger risk, and garrison costs to force real trade-offs rather than trivial optimal choices.

Emergent outcomes to expect
- Strategic spread: players may found scattered villages to secure resource nodes, then escort caravans between them — this creates logistics choices and emergent failure modes (raids on remote villages).
- Refugee dynamics: dismantling or abandoning settlements may spawn migrants or rogue bands that later cause trouble.

---

## Multi-Tile Cities & Urban Growth

When settlements become strategically important and well-defended, they should be able to expand beyond one tile into multi-tile towns and, later, cities. Multi-tile growth introduces logistics, infrastructure, specialization, and new strategic choices.

Why multi-tile makes sense here
- The overworld hex grid already treats each tile as an actionable location (resources, terrain, events). Growing cities across multiple tiles lets players translate local micro-decisions into regional strategy, and enables persistent goals (defend a city vs keep moving).
- Multi-tile cities enable long-term resource sinks and strategic targets for enemies, raising stakes and emergent drama.

Core mechanics
- City footprint: a `Settlement` gains a `footprint` set — a list of adjacent `HexTile`s that belong to the settlement. Each tile in the footprint contributes slots, production, and infrastructure.
- Districts & specialization: tiles inside the footprint can be specialized (Farm District, Workshop, Shrine, Market, Walls). Specialization modifies production and unlocks building types.
- Infrastructure links: roads, irrigation, and walls are multi-tile constructs that provide bonuses (movement speed, yield multipliers, defense). Roads create trade efficiency between tiles in a footprint and between settlements.
- Population distribution: population and workers are allocated across the footprint. Overcrowding triggers migration, unrest, or disease.

Growth model
- Growth triggers: surplus food/resources, high morale, trade, and faith-driven pilgrimages increase a settlement's `growthPoints` per turn. Once `growthPoints` exceed a threshold, the settlement can expand to an adjacent tile (if free) or upgrade an existing district.
- Expansion rules: prefer high-value tiles (river, fertile land) — expansion consumes resources and time (BuildSettlement-like WorkOrder). Random events (raids, blight) can delay expansion.
- Urban density: as settlements upgrade, they gain population caps, production multipliers, and increased maintenance (tax/food upkeep). Higher-level cities attract enemies and create strategic choke points.

Logistics & trade
- Internal logistics: implement a simple transport system where goods move along roads between tiles in the footprint. If no road exists, costs are higher and throughput lower.
- External trade: cities can set up trade routes with other settlements (player-owned or NPC). Trade consumes caravans (agents) or uses abstracted trade ticks that transfer resource bundles periodically.

Defense & politics
- Walls & garrisons: multi-tile cities can build walls around the footprint or specific districts. Walls increase defense and provide gates that affect pathing.
- Political choices: as cities grow, players decide tax rates, conscription, and public works. Higher taxes lower morale but fund defenses and infrastructure.

Performance considerations
- Track city-level aggregates (total population, stored resources, effective production) to avoid iterating every tile every tick for simple queries. Only update detailed per-tile state when necessary.

APIs & data structures
- Settlement.footprint : List<HexTile>
- Settlement.districts : Dictionary<HexTile, DistrictType>
- Settlement.growthPoints : float
- Settlement.roadNetwork : Graph of tiles (used for path cost modifiers)
- Settlement.ExpandToTile(HexTile tile)
- Settlement.BuildDistrict(HexTile tile, DistrictType dt)

UI & player feedback
- Map overlays to show city footprint, district types, road links, and trade routes.
- City panel: shows aggregated stats (population, faith income, food production, maintenance), growth progress, and available upgrades.

Emergent gameplay hooks
- Siege mechanics: an enemy attack can target a tile in the footprint; if walls/garrisons are insufficient, cities can lose districts or population.
- Political unrest and faction influence: large cities can develop factions or cults that interact with the god-beast (e.g., rival shrines, false prophets).
- Late-game strategic depth: cities enable advanced mechanics (specialized production chains, market economy, tech buildings) while preserving the early-game feel of camps and villages.

Balancing guidance
- Make expansion deliberate and costly; early-game players should focus on camps/villages. Cities should be mid/late-game goals that change the player's preferred playstyle.
- Keep per-tile calculations capped — use aggregated city stats for most gameplay effects.


## Integration with your codebase
- `HexTile` already has `resourceAmounts`, `isExplored`, and `InfestationLevel`. Reuse `resourceAmounts` for `food`, `sap`, etc.
- `SelectionManager` already manages layered highlighting (owners like "CardTarget", "Range", "Hover"). When cards create WorkOrders, use owner "CardTarget" or "Job" so card highlights persist when hovering.
- Card system: cards create WorkOrders via `WorkOrder.CreateFromCard(card, tile)` and push to `PopulationManager.workQueue`. Cards can also adjust tile productionRate or spawn agents directly.
- `PopulationManager` should be accessible as a singleton service (like `SelectionManager.Instance`) so UI/cards can create jobs and query summary state.

---

## Sample APIs & pseudo-code
- PopulationManager.RegisterWorkOrder(WorkOrder w)
- Agent.TryClaim(WorkOrder w)
- HexTile.Reserve(string reserverId, int ticks = 30)
- HexTile.ClearReservation(string reserverId)
- WorkOrder.TickProgress(int dt) // returns completed?

Example: card plays "Summon Harvesters"
1. Card creates N temporary agent prefabs or N WorkOrders of type Harvest with owner "Card:123".
2. WorkOrders appear on map; highlights use owner "CardTarget".
3. PopulationManager advertises work to agents; they claim & perform them.
4. On completion, owner gets notified and card effect resolves.

---

## Performance & scaling
- Throttle heavy operations (pathfinding, A*) across frames with a job scheduler.
- Partition agents by tile and only evaluate agents active in the visible area more often (LOD).
- Use batched tile resolution (resolve many tiles in one loop) and avoid per-agent heavy allocations.
- Keep agents cheap: avoid deep planners or heavy per-agent coroutines.

---

## Emergent features & extensions (later)
- Social bonds: track pairwise affinity; create family/home groups; loyalty and betrayal emergent stories.
- Reproduction & genetics: give children slightly mutated traits; long-term lineages.
- Disease & quarantine: integrate `HexTile.InfestationLevel` with agent disease states and spread mechanics.
- Factions & rivals: spawn rival caravans or colonies; trading, raiding, diplomacy.
- Rituals & faith: local faith computed from worshipper counts; rituals consume worshippers to buff the beast or tiles.

---

## UX & debug tools
- Agent panel: shows all agents, needs bars and current job (filter by tile, by name).
- Tile overlays: resource heatmap, reservation markers, job owner highlights (use `SelectionManager` owners).
- Event log: births, deaths, migrations, major job completions.
- Sim controls: tick speed, step-tick, and show/hide sleeping agents.

---

## MVP Implementation roadmap (first sprint)
1. `PopulationManager` skeleton and tick loop.
2. `Agent` component with hunger, simple foraging behavior, movement and inventory.
3. `WorkOrder` type: Harvest and a basic claiming/reserve system.
4. Hooks for `SelectionManager` and cards to create WorkOrders and highlight tiles (use owner "Job" or "CardTarget").
5. Small debug UI and visual overlays for reservations and agent positions.

Deliverable for sprint: agents that forage nearby tiles, return food to home tile (or beast), and be visible on map; card that creates a harvest WorkOrder that agents respond to.

---

## Example data constants (tuning)
- baseConsumptionPerPop = 1 food/turn
- foodPerFarmer = 1.5 food/turn
- birthRatePerAdult = 0.01 - 0.03 per turn
- workerCapacity per tile = 5-12
- reservationTimeout = 30 ticks
- simTick = 1s (initial), adjustable

---

## Next steps I can implement for you
- Prototype `PopulationManager` + `Agent` (MVP item 1–3) and add example agent prefabs.
- Add `HexTile` reservation fields and helper methods (`Reserve`/`ClearReservation`).
- Implement small debug UI (list of agents, toggles for overlays).

Tell me which of the above you'd like me to start implementing (I can begin with the `PopulationManager` prototype), or whether you want the doc adjusted to emphasize particular influences or constraints before I commit code.