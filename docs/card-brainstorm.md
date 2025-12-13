<link rel="stylesheet" href="assets/site.css">

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

Prototype numeric rules (simple starting defaults)

Hand size = 5, draw 3 per turn. AP = 2 actions per turn.
Cards: cost in AP (0–2) + resource costs (food/specialist/pop).
Multi-turn building: uses builder slot(s) for X turns; builders unavailable while busy.
Card rarity: Common / Uncommon / Rare / Unique (drop/chance structure).

---

# Combinable cards

# FOOD 

1. Seed (Common) [Leave room for Rare cards later]
World effect: Play onto a tile to mark “seeded” (consumes card). Behavior depends on tile type and follow-up:
- +Plain
- +Plow(Upgrade)
- +Forest
- +Grove(Upgrade)
Combat effect:

Get by: Scouting%, Foraging%, Vendor, Harvest of Crops


2. Forage [Simple resource gather card]
World effect: 
Combat effect:

# Forage variants

Forage Party (Instant, deterministic-ish) — simplest
Effect: Instant +N food to tile (or global stash). N = base + random(0..bonus) * workers. Reduces tile.forageStock by taken amount.
Cost: play card (AP) and/or consume 1 forager/pop.
Pros: immediate, tactile, easy to UI and test; good for early-game.
Cons: limited depth; predictable.
Tuning: base=2, bonus=4 per forager; regrowth 0.5–1/turn.

Forage Party (Probabilistic Finds) — more spice
Effect: Instant food plus a chance to find rare items (herb, seed, small card). Yield has variance; also chance of injury (small).
Pros: fun risk/reward; produces card economy and seed finds.
Cons: randomness; need to show probabilities to players.
Tuning: 15% rare find, 10% injury.

Forage Patch (Create Temporary Repeatable Source)
Effect: transforms tile into a "Forage Patch" state for M turns (3–5). Each turn it gives small food automatically or increases regrowth. Card consumed to create patch; needs small upkeep or builder to maintain.
Pros: introduces planning, area control, combo with managed grove.
Cons: multi-turn bookkeeping and visuals.
Tuning: +1.5 food/turn for 4 turns; cost high (2 builders or 4 resources).

Foraging Expedition (Multi-turn party, bigger reward, risk)
Effect: send X foragers for T turns (they are unavailable while out). After T turns return with large yield or possibly losses if encounter predators/events.
Pros: meaningful commitment and tension, ties to world events.
Cons: requires more state (tracking expedition).
Tuning: 3 foragers for 2 turns -> expected yield 12 food ± 30%, 20% predator chance causing casualties.

Specialist-Boosted Forage (Skill checks)
Effect: yield scales with presence of special character (Woodwarden, Hunter). If special present, guaranteed bonus and reduced injury chance.
Pros: gives value to recruiting special characters and cards that bind them.
Cons: dependency on other systems (special chars).
Tuning: +50% yield if Woodwarden present; injury chance reduced to 2%.



# Other ideas

Blessing of the Grove — Cost: 1 Food / CD 3

Overworld: +2 forage yield this turn on target Forest; tile morale +0.05.
Combat: Small heal to GC or beast (+10 HP).
Note: safe, predictable support.
Forage Party — Cost: 0 Action + 1 Stamina per forager / Instant

Overworld: Instant +(2–6) food from target Forest; 10% chance of minor injury.
Combat: Converts to "Quick Strike" — light damage + chance to stun.
Note: high variance, low commitment.
Managed Grove (Build) — Cost: 3 Builders + 6 Wood / Multi-turn (2)

Overworld: Turn tile into Managed Grove: +regrowth, small steady yield each turn.
Combat: Beast aura while on tile: enemies slowed.
Note: investment → safer long-term food.
Clear-Cut — Cost: 0 Action + Morale -0.15 / Instant, permanent tile flag

Overworld: Instant large food + timber; permanently reduces forest regrowth and lowers beast affinity.
Combat: Beast enraged — temporary heavy damage buff, but reduced control.
Note: big short-term reward, long-term cost.
Plow & Seed — Cost: 1 Builder + 1 Seed / Multi-turn (1–2)

Overworld: Plant plain; harvest in N turns yields fertility * farmers.
Combat: Grants a “field trap” that can immobilize a small enemy (if used in encounter near tile).
Note: introduces investment timing tension.
Irrigation — Cost: 4 Resources / CD 10

Overworld: Permanently +fertility on a Plain.
Combat: Drop water barrier — reduces enemy fire or poison damage for one turn.
Note: long-term upgrade.
Scrounge (Desperate) — Cost: 1 Card Discard / Instant

Overworld: Random small cache: food/medicine/rare component.
Combat: Bargain — convert card discard into immediate temporary stat buff.
Note: risk vs reward when hand is low.
Rally Song — Cost: 1 Morale / CD 3

Overworld: +0.15 morale for tile and neighbors this turn.
Combat: Debuff enemies’ accuracy for 1 round.
Note: soft support that saves lives but costs cultural capital.
Quarantine Protocol — Cost: 2 Healers or 2 Supplies / CD 8

Overworld: Quarantine tile for 3 turns: disease spread halted; productivity -20%.
Combat: If used in a fight at quarantined tile, reduces disease damage and grants heal-over-time.
Note: defensive, preserves specialists at resource cost.
Purge — Cost: 1 Specialist + 3 Morale / Instant, permanent tag

Overworld: Remove infection/heresy from tile but -30% population and permanent cultural scar.
Combat: Removes debuffs from allies, but inflicts a morale penalty after battle.
Note: heavy moral choice card.
Relocate Band — Cost: 2 Action Points + 1 Food / Instant

Overworld: Move up to N population from source tile to adjacent tile (spawns migrating band animation).
Combat: Create a diversion — spawn temporary ally (band) with weak attacks.
Note: mobility & triage tool.
Apprenticeship — Cost: 3 Population assigned + 2 Turns / Multi-turn

Overworld: After T turns, produce 1 specialist (Builder/Healer) on tile; assigned population unavailable while training.
Combat: Trained specialist grants a one‑time team ability if present in fight.
Note: long-term power vs short-term workforce loss.
Bind the Woodwarden — Cost: 4 Food + 1 Special Resource / Unique

Overworld: Recruit special character who permanently buffs forest regrowth and reduces predator risk on bound tile.
Combat: Woodwarden is a named unit with a unique combat skill (root snare / call of beasts).
Note: high-value durable hire.
Mass Ration — Cost: 0 Action + long-term morale cost / Instant

Overworld: Feed all population this turn, preventing starvation deaths but reduce morale (cultural debt tag).
Combat: Increases GC stamina for a fight but causes long-term unrest if overused.
Note: emergency save with lingering cost.
Trap & Ambush — Cost: 1 Builder + 1 Action / CD 5

Overworld: Lay trap on tile: if enemy band passes, they take heavy damage and drop loot.
Combat: Convert to an ambush bonus (first strike + damage).
Note: preplanning pays off.
Holy Offering — Cost: 2 Population Sacrifice / Instant + narrative tag

Overworld: Large immediate healing to the god-beast or remove a massive corruption stack; costs population and adds heavy guilt tag.
Combat: Massive buff for one combat, but permanently reduces future morale in region.
Note: dramatic and thematic.
Scout Run — Cost: 1 Scout Specialist or 1 Card / Instant

Overworld: Reveal adjacent tiles in a radius; small chance to find a card/treasure.
Combat: Grants first-turn initiative and increased hit chance.
Note: information/value trade.
Sabotage — Cost: 1 Specialist + risk / Instant

Overworld: Damage a rival node or delay enemy supply; risk of retaliation (counter-event).
Combat: Debuff enemy defenses or disable one enemy ability for a number of rounds.
Note: high-impact, political risk.
Herb Tonic — Cost: 1 Medicine (craft) / Instant

Overworld: Cure one disease per tile or reduce sickness level; small pop cost.
Combat: Heals a target and grants temporary disease resistance.
Note: reliable medical card.