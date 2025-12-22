<!-- AUTO_NAV_START -->
<nav>
<ul style="list-style:none; padding:0; display:flex; gap:1rem;">
  <li><a href="card-brainstorm.html">Card Brainstorm</a></li>
  <li><a href="index.html">Index</a></li>
  <li><a href="jobs.html">Jobs</a></li>
  <li><a href="localizationSetup.html">LocalizationSetup</a></li>
  <li><a href="population-design.html">Population Design</a></li>
  <li><a href="proto-brainstorm.html">Proto Brainstorm</a></li>
  <li><a href="todo.html">Todo</a></li>
</ul>
</nav>
<!-- AUTO_NAV_END -->

TODO:

<!-- 1. Define a Job concept (type, target tile, desired amount, assignee, completed flag). -->

<!-- 2. Add a job queue and storage to each Settlement with enqueue/complete APIs. -->

<!-- 3. Add a Harvest(resource,want) helper on HexTile that returns how much was actually taken. -->

<!-- 4. Extend PopulationAgent with job fields, carriedAmount/type, and states (TravelToSource, Harvesting, CarryToSettlement, Depositing). -->

<!-- 5. Implement StartJob(job,home) to claim the job, set target to the job tile and switch to TravelToSource. -->

<!-- 6. On arrival at source, run a timed harvest action (work timer), call Harvest(1), set carriedAmount, switch to CarryToSettlement. -->

<!-- 7. On arrival at settlement, deposit carriedAmount into settlement storage or ResourceManager, mark job complete or requeue leftover. -->

<!-- 8. Add a scheduler in PopulationManager that finds nearest idle agents and assigns queued jobs (run on a timer or when jobs enqueued). -->

9. Handle edge cases: reassign if agent dies, partially fulfill jobs and re-enqueue remaining amount, and claim job before moving (race safety).

10. Test small: spawn a few forest tiles, enqueue 1-unit GatherWood jobs, verify agents travel, chop, carry, deposit, and become idle.

11. Tune parameters: chop time, carry capacity, job granularity (1 unit per job recommended initially).

12. Iterate: add priorities, batching, hauling jobs, and spatial job partitioning for scale.


Next improvements: global JobManager for cross-settlement priorities, max assignment range, agent role filtering, and job batching.






- roles (Forager, Woodcutter, Hauler)
- Carrying & Return Trips
- Dynamic Job Assignment / Job Queue:

- Learning / Skill Growth

- Settlement.specialization set based on accumulated resource types.

- agent follow god if no camp/settlement
- settlement neighbors
- select agent
- give agent job
- agent gather resource: food

- settlement placeholder sprite disappears z behind on hover


- Social Behavior & Group Actions:
  What: Groups of agents can perform tasks together (fell a big tree, mole raid); group actions unlock resources otherwise inaccessible to individuals.


