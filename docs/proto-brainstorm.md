<link rel="stylesheet" href="assets/site.css">

<nav>
<ul style="list-style:none; padding:0; display:flex; gap:1rem;">
  <li><a href="index.html">Home</a></li>
  <li><a href="card-brainstorm.html">Card Ideas</a></li>
  <li><a href="proto-brainstorm.html">Prototype Ideas</a></li>
  <li><a href="localizationSetup.html">Localization Setup</a></li>
</ul>
</nav>

TODO:



2. 

3. card play targeting


Disable other input while targeting (e.g., prevent moving units).
Show card tooltip/cost while targeting.
Only highlight tiles in a limited radius or meeting additional card-specific conditions (override CanTarget in card subclasses).

4. Move n amount population to another tile implementation


# proto brainstorm

## POPULATION
normal population both a visible part of the map (small pawn sprites/bars) and a turn-resolved resource with worker‑slots + lifecycle, while cards act as fast, local modifiers or multi-turn programs that consume/produce population, skills, or upkeep.

- worshippers -/- heretics

pop == worshippers = power for god char
more wohrshippers == more faith == stat for spell power/hp?
Purge or sacrifices remove worshippers or convert them into faith/HP.
starvation

## FAITH
Faith: a derived scalar (global and/or local) computed from worshipper counts, modifiers (morale, cultural tags, special characters). Faith feeds two systems:

Passive scaling: God stats (HP, Spell Strength, Mana) scale with FaithFactor (diminishing returns).

Active currency: Some powerful god actions/cards require spending worshippers or faith (sacrifice = instant HP, ritual = temporary buff).

Tension: recruiting/protecting worshippers vs using them as currency or sacrificing them for survival.

Local faith matters: make certain god abilities only usable in tiles where localFaith > X (encourages moving to/keeping followers).

Faith as resource + reputation: allow factions (rival groups) to attract worshippers if certain conditions occur — adds competitive dynamics early.

Ritual chain: allow combining seeds/managed grove with shrine to create pilgrimage routes that steadily convert migrating bands.



Data model (per tile)

HexTile fields:
int populationCount
int workerCapacity (max slots)
Dictionary<Role,int> workers (Farmer, Builder, Guard, Healer)
float foodStored
float morale (0..1)
float disease (0..1)
bool quarantineFlag
List<SpecialCharacter> residents (the special cards/characters)
Population meta:
float birthRatePerAdult (applied per turn)
float baseConsumptionPerPop
float migrationThreshold (when pop > capacity * X)
World-turn resolution (high level)

For each tile, in order:
Resource consumption: consume food = population * baseConsumption * roleModifiers. If shortage, morale -= penalty.
Production: farmers produce food = farmers * yield; builders progress task queues; healers reduce disease.
Lifecycle: births = floor(adultPop * birthRate * stabilityModifier); deaths/mortality from disease & starvation.
Overcapacity/spillover: if population > capacity -> create migrating band or move to best neighbor(s).
Events & card effects: apply card changes scheduled or played this turn.


1. Food Supply & Rationing

Concept: Each tile produces/consumes food; shortages force rationing decisions.
Hardcore/fun: Choosing who eats matters; underfeed repeatedly → permanent population loss or lower birth rates.
Turn-based fit: Each turn the player allocates rations per region (auto / manual sliders).
Card hooks: Emergency Grain (one-turn huge boost), Selective Ration (sacrifice morale in one tile, feed others).
- Dark Souls / Gods Will Be Watching: make famine choices narratively impactful — who eats and who doesn't can have long-term consequences.
- Dwarf Fortress / The Settlers: logistics chains and stockpiles feel tangible; losing stores creates cascading challenges.
- Slay the Spire / Gloomhaven: resource cards that force difficult, strategic decisions (one-use life-saving cards).

2. Professions — Worker Assignment

Concept: Assign portions of population to roles (Farmer, Builder, Healer). Roles change output and vulnerability.
Hardcore/fun: Sacrificing workforce to industry speeds growth but leaves shortages; losing a profession can cascade (no food → migration).
Turn-based fit: Reassign at map turns, bottleneck decisions are palpable each turn.
Card hooks: Inspire Labor (temporarily doubles builder efficiency), Strike (force a profession strike for cost/benefit).
- Dwarf Fortress: specialists are rare and valuable — losing a skill can significantly impact your strategy.
- Jagged Alliance 2: assign characters to tasks with unique talents and risks; specialists can change tactical dynamics.
- Knights in Tight Spaces / Gloomhaven: short, tactical choices each turn about who performs which role.

3. Births, Aging & Generational Debt

Concept: Population has simple cohort state: children, adults, elders; births require food + stability.
Hardcore/fun: Rapid growth can create unsustainable booms leading to famine; aging means losing specialists if you neglect health.
Turn-based fit: Birth/mortality evaluated per turn; long-term planning rewarded.
Card hooks: Fertility Rite (boost births next turn), Conscription (turn children into soldiers at a cost).
- Darkest Dungeon / Dark Souls: generational challenges and losses accumulate, making each playthrough feel significant.
- Dwarf Fortress: emergent lineage effects — plan for heirs and consider the long-term implications.
- Gods Will Be Watching: tough policy choices with moral implications for survival.

4. Professions Upskill / Apprenticeship

Concept: Turn-based investment turns generic population into specialists over multiple turns.
Hardcore/fun: Investing time creates powerful long-term tools but leaves immediate capacity low — cold hard trade-offs.
Turn-based fit: Start an apprenticeship task (2–5 turns) per tile.
Card hooks: Master Craftsman (instant finish of one apprenticeship), Cheap Labor (speed ups but lowers morale).
- Dwarf Fortress / Settlers boardgame: investing time to create craftsmen yields significant long-term benefits but can be risky early on.
- Slay the Spire: choose between long-term upgrades versus short-term gains (decks versus professions analogy).

5. Resource Chains & Fragile Logistics

Concept: Supply chains: e.g., grain → mill → bread; if a node fails, downstream collapses.
Hardcore/fun: Cascading failures; protecting logistics becomes strategy.
Turn-based fit: Repair/redirect supply nodes as actions per turn.
Card hooks: Route Reroute (reroute supplies for a turn), Sabotage (damage an enemy supply node).
- 4X / The Settlers 2: supply nodes as strategic objectives — protect the chain or face collapse.
- Jagged Alliance 2: supply denial and sabotage as tactical strategies with lasting impacts.
- VERMIS / early FromSoftware: environmental choke points that challenge careless routing.

6. Mortality Events & Lasting Scars

Concept: Events (plague, winter) cause deaths scaled by population density and healthcare profession level. Some dead are permanent and reduce future growth.
Hardcore/fun: Random or triggered events force triage and painful choices.
Turn-based fit: Event declares, player has N turns to respond before full effect.
Card hooks: Quarantine Protocol (reduce deaths in target tile for X turns), Mass Burn (removes disease at large population cost).
- Darkest Dungeon: stress and scars that persist across turns, making resource management critical.
- Gods Will Be Watching: event-driven moral dilemmas with complex outcomes.
- Dwarf Fortress: catastrophic events that leave lasting impacts on the world.

7. Migration & Diaspora Decisions

Concept: People move between regions by decision or desperation; migrating groups can be directed or become rogue bands.
Hardcore/fun: Offloading population is cheap short-term, but you give up future manpower and strategic control.
Turn-based fit: Each turn you approve or block migration paths and receive migrant requests.
Card hooks: Open Borders (absorbs migrants with morale boost), Closed Gates (blocks migrants but reduces trade).
- 4X / Dwarf Fortress: migration as a strategic tool — accept refugees or let them roam and create instability.
- Slay the Spire: gamble-or-reward style choices (accept migrants now for unpredictable long-term benefits).
- Spirit Island: population movements alter map dynamics and escalate challenges.

8. Skill Loss & Irreversibility

Concept: Lost specialists (e.g., one-of skill) may take many turns to re-produce; losing them is painful.
Hardcore/fun: Stakes in defending a single master or workshop.
Turn-based fit: Protect assets across turns or suffer long rebuild timers.
Card hooks: Protect the Master (guard action for one turn), Forced Apprenticeship (instantly create low-quality specialists).
- Dark Souls / Darkest Dungeon: permanent consequences and challenges — choices feel impactful and significant.
- Jagged Alliance 2: losing a specialist reshapes tactics and requires adaptation.

9. Seasonal Cycles & Planning

Concept: Seasons change production/consumption; poor planning before winter causes famine next turn. !Maybe more /and or different than the usually known 4 seasons?
Hardcore/fun: Makes turn-based planning strategic and tense.
Turn-based fit: Each season lasts several turns; player prepares with stockpiles or migration.
Card hooks: Harvest Festival (double food this season), Blight (reduce production next season).
- The Settlers 2 / Dwarf Fortress: seasons necessitate stockpiling and strategic planning.
- Dark Souls: environmental cycles that alter threat levels and require careful timing.
- Gloomhaven: seasonal pacing where preparation matters across multiple turns.

10. Professions as Asymmetric Defenses

Concept: Soldiers/Guards are a profession that can be assigned to defend tiles—losing them can lead to looting or uprisings.
Hardcore/fun: Decisions between building industry or security.
Turn-based fit: Move/conscript guards or reassign per tile per turn.
Card hooks: Conscription (convert workers to guards for X turns), Peace Treaty (reduce need for guards temporarily).
- Jagged Alliance 2: tactical value of trained defenders versus resource producers.
- Knights in Tight Spaces: positioning and micro-tactics make defender roles crucial each turn.
- Darkest Dungeon: protect specialists or risk losing them to hold the line.

11. Cultural Memory & Hereditary Effects

Concept: Decisions accumulate culture tags on tiles (e.g., “sacrificed”, “sanctified”) that alter long-term behavior (trust, birthrate, resistance).
Hardcore/fun: Creates a moral ledger; players cannot ignore previous choices.
Turn-based fit: Tags applied at event resolution, affect later turn outcomes.
Card hooks: Atone (remove a negative tag), Remembered Sacrifice (grant short-term benefits but permanent tag).
- Dwarf Fortress: history and legends influence behavior; place-based memories alter outcomes.
- Dark Souls: lore and persistent world weight choices with atmosphere and consequence.
- Spirit Island: long-term modifiers that shift player strategy over many turns.

12. Production Queues and Bottleneck Micro-decisions

Concept: Each tile can queue building and production tasks that consume workforce over turns; queues fill/empty.
Hardcore/fun: Micro-decisions about which queue to prioritize create emergent regional weaknesses.
Turn-based fit: Each turn allocate tasks or let queues progress; pause/resume.
Card hooks: Boost Factory (speed one queue), Strike (block a queue).
- The Settlers 2 / Settlers boardgame: production chains and queue management as engaging optimization challenges.
- Slay the Spire: deck/queue planning — prepare now for future turns' needs.

13. Special Character Cards — Population Heroes

Concept: Certain population members are represented by special cards/characters — they count as population but also grant unique worldmap abilities and powerful combat skills.
Hardcore/fun: Losing a named specialist is painful (permanent scars, lost cards), but they enable tactical options and risky strategies.
Turn-based fit: Deploy or assign special character cards each turn for tasks (lead a harvest, perform a ritual, scout dangerous hexes) and bring them into micro-combat with unique abilities.
Card hooks: Recruit Hero (add a special card with one-time boost), Bind to Beast (increase beast synergy but raises upkeep/cost).

- Darkest Dungeon / Dark Souls: named characters have personalities and potential permanent consequences when lost.
- Jagged Alliance 2: characters as tactical assets with unique skills and hire/replace costs.
- Slay the Spire / Gloomhaven: cards that double as abilities and persistent characters, forcing deck/roster trade-offs.

14. Trade-Off Cards That Reshape Strategy

- Slay the Spire / Gloomhaven: cards as persistent strategic commitments or risky plays that alter priorities.
- Gods Will Be Watching: cards that create moral trade-offs with gameplay consequences.
- Spirit Island: choose timing and area for powerful one-use effects that reshape the game.



---

"Forestjumper" character, fast in forest / scouting / more visibility




Make losses visible and persistent (tags, lost specialists) so choices sting.
Favor multi-turn consequences — this deepens planning and gives cards weight.
Keep early-game mechanics simple (1–2 resources, 2 professions) and scale complexity later.
Let cards create dilemmas (short-term relief vs long-term damage) rather than trivial buffs.
Use scarcity (limited card draws or resource points per turn) to force hard trade-offs.




baseConsumptionPerPop = 1 food per day turn (tune to map density).
foodPerFarmer = 1.5 food/turn — makes 1 farmer roughly sustain 1–2 pop.
birthRatePerAdult = 0.01 - 0.03 per turn (slow).
workerCapacity initially = 5-12 per tile. Overflow threshold for migration = capacity * 1.1.

public class HexTile {
  public int populationCount;
  public int workerCapacity;
  public Dictionary<Role,int> workers = new();
  public float foodStored;
  public float morale = 1f;
  public float disease = 0f;
  public List<SpecialCharacter> residents = new();
  public bool quarantine;
}

void ResolveTileTurn(HexTile t, float dt=1f) {
  float consumption = t.populationCount * baseConsumptionPerPop;
  float consumed = Mathf.Min(consumption, t.foodStored);
  t.foodStored -= consumed;
  if(consumed < consumption) {
    float shortageRatio = 1f - consumed/consumption;
    t.morale -= shortageRatio * 0.3f;
    int deaths = Mathf.FloorToInt(shortageRatio * t.populationCount * starvationMortality);
    t.populationCount = Mathf.Max(0, t.populationCount - deaths);
  }

  // production
  int farmers = t.workers.Get(Role.Farmer);
  t.foodStored += farmers * foodPerFarmer;

  // births
  int adults = t.populationCount; // optionally exclude children
  int births = Mathf.FloorToInt(adults * t.birthRatePerAdult);
  t.populationCount += births;

  // disease and healers
  int healers = t.workers.Get(Role.Healer);
  t.disease = Mathf.Clamp01(t.disease + diseaseGrowthRate * (t.populationCount/ (t.workerCapacity+1f)) - healers * healerEffect);

  // overcapacity
  if(t.populationCount > t.workerCapacity) SpawnMigrants(t, t.populationCount - t.workerCapacity);
}