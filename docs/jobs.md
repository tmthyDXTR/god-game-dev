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


JOBS
enum JobType { GatherWood, ForageFood, Haul, Build, Scout }

class Job {
    public int id;
    public JobType type;
    public HexTile targetTile; // location
    public int amountWanted; // optional
    public PopulationAgent assignedAgent;
    public bool isAssigned => assignedAgent != null;
    public float createdAt;
}

Settlement:

List<Job> jobQueue;
Job RequestJob(JobType type, HexTile tile, int amount) { create & enqueue }
void OnJobComplete(Job job, int producedAmount) { deposit into storage / ResourceManager }
PopulationManager:

Periodically (or on job enqueue) scan pending jobs → assign nearest idle agent via Agent.IsIdle() → agent.StartJob(job)
PopulationAgent:

field Job currentJob
AgentState.PerformingJob
StartJob(Job job): set currentJob, SetTarget(job.targetTile), agentState=PerformingJob
When ArriveAtTarget: start work timer (chopTime = baseChopTime / skill)
Work loop: accumulate workProgress; on completion call CompleteJob()
HexTile:

Use existing resourceAmounts for wood; optionally add helper Harvest(resource, amount) to remove exact amount and return actual removed.




Wood gather loop specifics

1. Player or world creates Job(GatherWood, targetTile, amount=1..N). For prototyping, you can auto-create jobs for every forest tile with >0 wood.

2. PopulationManager finds an idle agent (or nearest) and assigns the job.

3. Agent moves to tile. On arrival:
    starts a work timer (e.g., chopDuration = 3s).
    optionally play chop animation / sound.
    when timer completes: call tile.RemoveResource(GameResource.Materials, 1) (or Harvest).
    deposit: either agent adds to its carriedAmount and returns to nearest settlement to deposit (logistics) OR for proto, immediately call Settlement.OnJobComplete(job, 1) to add to storage / ResourceManager.

4. Mark job as complete; remove from queue.