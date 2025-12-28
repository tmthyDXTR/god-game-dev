using System.Collections.Generic;
using UnityEngine;
using HexGrid;
using Prototype.Traits;
using System.Linq;

public class PopulationAgent : MonoBehaviour
{
    [Tooltip("Movement speed in tiles per tick (for tick-based) or units per second (for frame-based)")]
    public float moveSpeed = .25f;
    [Header("Movement")]
    [Tooltip("If true, agents move in discrete steps on the global TickManager ticks instead of per-frame.")]
    public bool useTickMovement = true;
    public HexTile currentTile;
    HexTile targetTile;
    bool stayOnTile = false;



    public enum BaseStat {
        startingTraitsAmount,
        Health,
        Strength,
    }

    public enum AgentState {
        Idle,
        SeekingFood,
        Eating,
        TravelingToSource,
        Harvesting,
        PickingUp,          // Picking up dropped resources from ground
        CarryingToSettlement,
        Depositing
    }
    
    [Header("Traits")]
    public List<TraitSO> traits = new List<TraitSO>();


    [Header("Base Stats")]
    // Concrete sampled stat values (set at Initialize)
    [Tooltip("Number of starting traits assigned to this agent after sampling the distribution")]
    public int startingTraitsAmount = 1;
    [Tooltip("Agent health value sampled from the health distribution")]
    public int health = 1;
    [Tooltip("Agent strength value sampled from the strength distribution")]
    public int strength = 1;

    [Tooltip("Base carry capacity used by GetCarryCapacity before trait modifiers")]
    public int baseCarryCapacity = 1;
    [Tooltip("Base gather multiplier used by GetGatherMultiplier before trait modifiers")]
    public float baseGatherMultiplier = 1f;
    
    // Cached/computed stats (calculated at Initialize and used by game logic)
    public int carryCapacity;
    public float gatherMultiplier;

    [Header("Job System")]
    public Job currentJob = null;
    public int carriedAmount = 0;
    public Settlement homeSettlement = null;
    
    // Track our current reservation so we can release it when job ends
    private HexTile reservedTile = null;
    private Managers.ResourceManager.GameResource reservedResourceType;
    private int reservedAmount = 0;
    private bool isDroppedReservation = false;
    
    [Header("Job Assignment")]
    [Tooltip("Assigned job type for this agent - they will continuously perform this job type")]
    public JobType assignedJobType = JobType.Gather;
    [Tooltip("Resource type to gather/haul (for Gather and Haul jobs)")]
    public Managers.ResourceManager.GameResource assignedResource = Managers.ResourceManager.GameResource.Materials;
    [Tooltip("If true, agent will automatically start new jobs of their assigned type when idle")]
    public bool autoRepeatJob = true;

    [Header("Hunger System")]
    [Tooltip("Current hunger level (0 = starving, maxHunger = full)")]
    public float currentHunger = 100f;
    [Tooltip("Maximum hunger capacity")]
    public float maxHunger = 100f;
    [Tooltip("Hunger decay per second (lower = slower hunger)")]
    public float hungerDecayRate = 1f;
    [Tooltip("Food restored per eating action")]
    public float foodRestorationAmount = 30f;
    [Tooltip("Current behavioral state of the agent")]
    public AgentState agentState = AgentState.Idle;

    [Header("Harvesting")]
    [Tooltip("Base work units required to harvest one unit (tick-driven)")]
    public float baseHarvestWorkUnits = 1f;
    [Tooltip("Base work units required to pick up one dropped item (tick-driven)")]
    public float basePickupWorkUnits = 0.5f;
    // runtime remaining work units for the current harvest or pickup
    float harvestWorkRemaining = 0f;
    float pickupWorkRemaining = 0f;
    // cached tick manager for subscription
    Managers.ResourceTickManager resourceTickManager;

    [Header("Stat Distributions (probability charts)")]
    [Tooltip("Centralized stat chart ScriptableObject (use this; per-agent arrays removed).")]
    public Prototype.StatChartSO statChart;

    [Header("Carried Item Visual")]
    [Tooltip("Sprite to show when carrying Materials (logs)")]
    public Sprite carriedLogSprite;
    [Tooltip("Sprite to show when carrying Food")]
    public Sprite carriedFoodSprite;
    // Child object for carried item display
    private GameObject carriedItemVisual;
    private SpriteRenderer carriedItemRenderer;

    // local wandering
    Vector3 localTarget;
    public float idleRadius = 0.65f;
    public float idleTargetThreshold = 0.001f;
    float idleTimer = 0f;
    public float idlePickInterval = 2f;

    // Smooth tick-based movement interpolation
    // On each tick, we calculate where the agent should be at tick end, then lerp there over the tick duration
    Vector3 tickStartPosition;
    Vector3 tickEndPosition;
    bool isInterpolatingMovement = false;

    public void Initialize(HexTile start, bool _stayOnTile = false)
    {
        currentTile = start;
        targetTile = null;
        transform.position = currentTile != null ? currentTile.transform.position : Vector3.zero;
        stayOnTile = _stayOnTile;
        if (stayOnTile)
        {
            // place with a small jitter inside the tile so agents are visible
            transform.position = GetRandomPointInTile();
            PickNewLocalTarget();
        }
        // Set sprite renderer order in layer to be above tiles and trees
        // Use Y-based sorting like trees, starting at a higher base
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float worldY = transform.position.y;
            sr.sortingOrder = 2000 + Mathf.RoundToInt(-worldY * 100f);
        }
        
        // Create carried item visual as child object
        CreateCarriedItemVisual();
        
        // Initialize interpolation positions
        tickStartPosition = transform.position;
        tickEndPosition = transform.position;
        isInterpolatingMovement = false;
        
        // Set transform scale
        // TODO: make this configurable depending on stats like strength ie?
        // transform.localScale = Vector3.one * 4f;

        // Sample base stats from configured probability charts
        // (if the charts are not provided, keep defaults)
        // Prefer charts from the centralized `statChart` asset if provided, otherwise fall back to per-agent arrays
        var startChart = statChart != null ? statChart.startingTraitsAmountChart : null;
        var startValues = statChart != null ? statChart.startingTraitsAmountValues : null;
        startingTraitsAmount = SampleStatFromChart(startChart, startValues, startingTraitsAmount);

        var hChart = statChart != null ? statChart.healthChart : null;
        var hValues = statChart != null ? statChart.healthValues : null;
        health = SampleStatFromChart(hChart, hValues, health);

        var sChart = statChart != null ? statChart.strengthChart : null;
        var sValues = statChart != null ? statChart.strengthValues : null;
        strength = SampleStatFromChart(sChart, sValues, strength);

        // Give starting random traits
        for (int i = 0; i < startingTraitsAmount; i++)
        {
            var trait = Managers.TraitManager.Instance != null ? Managers.TraitManager.Instance.GetRandomTrait() : null;
            AddTrait(trait);
        }

        // Compute derived stats once at initialization so game logic uses stable values
        ComputeDerivedStats();
        
        // Initialize hunger to max
        currentHunger = maxHunger;
        agentState = AgentState.Idle;

        // Subscribe to global tick events for deterministic work progression
        resourceTickManager = FindFirstObjectByType<Managers.ResourceTickManager>();
        if (resourceTickManager != null)
            resourceTickManager.OnTickEvent += HandleTick;
    }

    void OnDisable()
    {
        // Release any resource reservations when disabled/destroyed
        ReleaseCurrentReservation();
        
        if (resourceTickManager != null)
            resourceTickManager.OnTickEvent -= HandleTick;
    }

    // Trait helpers
    public bool HasTrait(string traitId)
    {
        if (string.IsNullOrEmpty(traitId)) return false;
        foreach (var t in traits)
            if (t != null && t.traitId == traitId)
                return true;
        return false;
    }

    public void AddTrait(TraitSO trait)
    {
        if (trait == null) return;
        if (!traits.Contains(trait))
            traits.Add(trait);
    }

    public void RemoveTrait(TraitSO trait)
    {
        if (trait == null) return;
        if (traits.Contains(trait))
            traits.Remove(trait);
    }

    public int GetCarryCapacity()
    {
        return Mathf.Max(0, carryCapacity);
    }

    public float GetGatherMultiplier()
    {
        return gatherMultiplier;
    }

    // Recompute cached derived stats based on base stats and traits.
    void ComputeDerivedStats()
    {
        int cap = baseCarryCapacity;
        float mul = baseGatherMultiplier;
        if (traits != null)
        {
            foreach (var t in traits)
            {
                if (t == null) continue;
                cap += t.carryCapacityBonus;
                mul *= t.gatherMultiplier;
            }
        }
        carryCapacity = Mathf.Max(0, cap);
        gatherMultiplier = mul;
    }

    void Update()
    {
        // Update carried item visual
        UpdateCarriedItemVisual();
        
        // Tick hunger down over time
        currentHunger -= hungerDecayRate * Time.deltaTime;
        currentHunger = Mathf.Max(0f, currentHunger);

        // Check if starving and not already seeking food
        if (currentHunger <= 0f && agentState != AgentState.SeekingFood && agentState != AgentState.Eating)
        {
            // Find nearest food tile and go there
            HexTile foodTile = FindNearestFoodTile();
            if (foodTile != null)
            {
                agentState = AgentState.SeekingFood;
                stayOnTile = false;
                SetTarget(foodTile);
                Debug.Log($"{name}: Starving! Seeking food at {foodTile.HexCoordinates}");
            }
        }

        if (stayOnTile && targetTile == null && agentState == AgentState.Idle)
        {
            // idle wandering inside current tile
            idleTimer += Time.deltaTime;
            Vector3 targetPos = localTarget;
            // Idle wandering remains frame-driven so it feels smooth even when movement is ticked.
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            if (Vector3.SqrMagnitude(transform.position - targetPos) < idleTargetThreshold || idleTimer >= idlePickInterval)
            {
                PickNewLocalTarget();
            }
            return;
        }

        if (targetTile == null) return;
        // If movement is driven by ticks, do smooth interpolation between tick positions
        if (useTickMovement)
        {
            if (isInterpolatingMovement)
            {
                // Smoothly lerp from tickStartPosition to tickEndPosition based on tick progress
                float progress = Managers.GlobalTickManager.Instance != null 
                    ? Managers.GlobalTickManager.Instance.TickProgress 
                    : 1f;
                transform.position = Vector3.Lerp(tickStartPosition, tickEndPosition, progress);
                
                // Update sorting order based on Y
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float worldY = transform.position.y;
                    sr.sortingOrder = 2000 + Mathf.RoundToInt(-worldY * 100f);
                }
                
                // Check if interpolation is complete (progress >= 1) and we've arrived at destination
                if (progress >= 0.99f)
                {
                    CheckArrivalAfterInterpolation();
                }
            }
        }
        else
        {
            Vector3 worldTargetPos = targetTile.transform.position;
            transform.position = Vector3.MoveTowards(transform.position, worldTargetPos, moveSpeed * Time.deltaTime);
            if (Vector3.SqrMagnitude(transform.position - worldTargetPos) < 0.0001f)
            {
                ArriveAtTarget();
            }
        }

        // Harvesting is now driven by global ticks via HandleTick
    }

    void ArriveAtTarget()
    {
        if (currentTile != null) currentTile.OnPopulationLeave(this);
        currentTile = targetTile;
        if (currentTile != null) currentTile.OnPopulationEnter(this);
        targetTile = null;

        // If we were seeking food and arrived at a tile with food, eat exactly 1 food and return to idle
        if (agentState == AgentState.SeekingFood && currentTile != null)
        {
            // Immediately change state to prevent re-entry
            agentState = AgentState.Eating;
            
            int availableFood = currentTile.GetResourceAmount(Managers.ResourceManager.GameResource.Food);
            if (availableFood > 0)
            {
                // Consume exactly 1 food from tile and restore hunger
                currentTile.RemoveResource(Managers.ResourceManager.GameResource.Food, 1);
                currentHunger = Mathf.Min(currentHunger + foodRestorationAmount, maxHunger);
                Debug.Log($"{name}: Ate 1 food! Hunger restored to {currentHunger:F1}");
            }
            else
            {
                // No food here anymore
                Debug.Log($"{name}: No food at destination. Will search again when hungry.");
            }
            
            // Return to idle state and resume local wandering
            agentState = AgentState.Idle;
            stayOnTile = true;
            PickNewLocalTarget();
            return;
        }

        // If we were assigned a job and traveled to the source, begin harvesting or picking up (tick-driven)
        if (agentState == AgentState.TravelingToSource && currentJob != null && currentJob.originTile == currentTile)
        {
            // For Haul jobs, go directly to picking up dropped resources
            if (currentJob.type == JobType.Haul)
            {
                int dropped = currentTile.GetDroppedAmount(currentJob.resource);
                if (dropped <= 0)
                {
                    Debug.Log($"{name}: Arrived for Haul job {currentJob.id} but no dropped {currentJob.resource}. Searching for alternative.");
                    HexTile nearestDropped = FindNearestTileWithDroppedResource(currentJob.resource);
                    if (nearestDropped != null)
                    {
                        Debug.Log($"{name}: Found dropped resources at {nearestDropped.HexCoordinates}. Redirecting.");
                        currentJob.originTile = nearestDropped;
                        SetTarget(nearestDropped);
                        return;
                    }
                    else
                    {
                        Debug.Log($"{name}: No dropped {currentJob.resource} found. Canceling Haul job.");
                        ReleaseCurrentReservation();
                        currentJob.isClaimed = false;
                        currentJob.claimedByAgentId = null;
                        currentJob = null;
                        carriedAmount = 0;
                        agentState = AgentState.Idle;
                        stayOnTile = true;
                        PickNewLocalTarget();
                        return;
                    }
                }
                
                // Start picking up
                pickupWorkRemaining = basePickupWorkUnits;
                agentState = AgentState.PickingUp;
                Debug.Log($"{name}: Arrived for Haul job {currentJob.id}. Beginning pickup (work={pickupWorkRemaining:F2}).");
                return;
            }
            
            // For Gather jobs, check if the resource is still available on this tile
            int available = currentTile.GetResourceAmount(currentJob.resource);
            if (available <= 0)
            {
                Debug.Log($"{name}: Arrived at source for job {currentJob.id} but no {currentJob.resource} available. Searching for nearest alternative.");
                
                // Try to find the nearest tile with this resource
                HexTile nearestSource = FindNearestTileWithResource(currentJob.resource);
                if (nearestSource != null)
                {
                    Debug.Log($"{name}: Found alternative source at {nearestSource.HexCoordinates}. Redirecting.");
                    currentJob.originTile = nearestSource; // Update the job's origin
                    SetTarget(nearestSource);
                    // Stay in TravelingToSource state to continue the journey
                    return;
                }
                else
                {
                    Debug.Log($"{name}: No alternative source found for {currentJob.resource}. Canceling job.");
                    // Release the job claim and return to idle
                    ReleaseCurrentReservation();
                    currentJob.isClaimed = false;
                    currentJob.claimedByAgentId = null;
                    currentJob = null;
                    carriedAmount = 0;
                    agentState = AgentState.Idle;
                    stayOnTile = true;
                    PickNewLocalTarget();
                    return;
                }
            }
            
            // Set remaining work units for one unit of resource
            harvestWorkRemaining = baseHarvestWorkUnits;
            agentState = AgentState.Harvesting;
            Debug.Log($"{name}: Arrived at source for job {currentJob.id}. Beginning harvest (work={harvestWorkRemaining:F2}).");
            return;
        }

        // If we were carrying to settlement and arrived, deposit carried resources
        if (agentState == AgentState.CarryingToSettlement)
        {
            // Determine target settlement tile (prefer homeSettlement, fall back to job target)
            Settlement targetSettlement = homeSettlement ?? (currentJob != null ? currentJob.targetSettlement : null);
            HexTile settlementTile = targetSettlement != null ? targetSettlement.GetComponent<HexTile>() : null;

            // If we've arrived at the settlement tile, deposit
            if (settlementTile != null && currentTile == settlementTile)
            {
                int depositAmount = carriedAmount;
                if (depositAmount > 0)
                {
                    if (targetSettlement != null)
                    {
                        // Let settlement handle storage and global updates
                        targetSettlement.OnJobComplete(currentJob, depositAmount);
                    }
                    else
                    {
                        // No settlement to accept - fallback to global ResourceManager
                        if (Managers.ResourceManager.Instance != null && currentJob != null)
                        {
                            Managers.ResourceManager.Instance.AddResource(currentJob.resource, depositAmount);
                        }
                    }
                }

                // Adjust job remaining amount and either complete or requeue
                if (currentJob != null)
                {
                    currentJob.amount -= depositAmount;
                    if (currentJob.amount > 0)
                    {
                        // Partially fulfilled: release claim and re-enqueue only if not already in queue
                        currentJob.isClaimed = false;
                        currentJob.claimedByAgentId = null;
                        var enqueueTarget = currentJob.targetSettlement ?? targetSettlement;
                        if (enqueueTarget != null)
                        {
                            enqueueTarget.EnqueueJob(currentJob);
                            Debug.Log($"{name}: Partially completed job {currentJob.id}. Remaining {currentJob.amount} re-enqueued.");
                        }
                    }
                    else
                    {
                        // Fully completed - no re-enqueue
                        Debug.Log($"{name}: Job {currentJob.id} fully completed and closed.");
                    }
                }

                // Clear carried goods and job assignment
                ReleaseCurrentReservation();
                carriedAmount = 0;
                currentJob = null;
                
                // Auto-repeat: start a new job of the assigned type
                if (autoRepeatJob)
                {
                    TryStartAssignedJob();
                }
                else
                {
                    agentState = AgentState.Idle;
                    stayOnTile = true;
                    PickNewLocalTarget();
                }
                return;
            }
        }
    }

    public void SetTarget(HexTile t)
    {
        if (t == null) return;
        targetTile = t;
    }

    void PickNewLocalTarget()
    {
        localTarget = GetRandomPointInTile();
        idleTimer = 0f;
    }

    Vector3 GetRandomPointInTile()
    {
        if (currentTile == null) return transform.position;
        var center = currentTile.transform.position;
        var off = Random.insideUnitCircle * idleRadius;
        return new Vector3(center.x + off.x, center.y + off.y, center.z);
    }

    // Called to begin inter-tile movement
    public void StartMovement()
    {
        if (!stayOnTile) return;
        stayOnTile = false;
    }

    /// <summary>
    /// Try to start a new job based on the agent's assigned job type.
    /// Used for auto-repeat functionality.
    /// </summary>
    public void TryStartAssignedJob()
    {
        // Find home settlement if not set
        if (homeSettlement == null)
        {
            homeSettlement = FindFirstObjectByType<Settlement>();
        }
        
        if (assignedJobType == JobType.Gather)
        {
            // Find nearest tile with the assigned resource
            HexTile targetTile = FindNearestTileWithResource(assignedResource);
            if (targetTile != null)
            {
                // Reserve the resource before claiming the job
                ReserveResources(targetTile, assignedResource, 1, false);
                
                var job = new Job
                {
                    type = JobType.Gather,
                    resource = assignedResource,
                    amount = 1,
                    originTile = targetTile,
                    priority = 0
                };
                job.isClaimed = true;
                job.claimedByAgentId = name;
                currentJob = job;
                carriedAmount = 0;
                stayOnTile = false;
                SetTarget(targetTile);
                agentState = AgentState.TravelingToSource;
                Debug.Log($"{name}: Auto-started Gather job for {assignedResource} at {targetTile.HexCoordinates}");
                return;
            }
            else
            {
                Debug.Log($"{name}: No {assignedResource} to gather. Going idle.");
            }
        }
        else if (assignedJobType == JobType.Haul)
        {
            // Find nearest tile with dropped resources
            HexTile targetTile = FindNearestTileWithDroppedResource(assignedResource);
            if (targetTile != null)
            {
                // Reserve the dropped resource before claiming the job
                ReserveResources(targetTile, assignedResource, 1, true);
                
                var job = new Job
                {
                    type = JobType.Haul,
                    resource = assignedResource,
                    amount = 1,
                    originTile = targetTile,
                    priority = 1
                };
                job.isClaimed = true;
                job.claimedByAgentId = name;
                currentJob = job;
                carriedAmount = 0;
                stayOnTile = false;
                SetTarget(targetTile);
                agentState = AgentState.TravelingToSource;
                Debug.Log($"{name}: Auto-started Haul job for {assignedResource} at {targetTile.HexCoordinates}");
                return;
            }
            else
            {
                Debug.Log($"{name}: No dropped {assignedResource} to haul. Going idle.");
            }
        }
        
        // Fallback to idle
        agentState = AgentState.Idle;
        stayOnTile = true;
        PickNewLocalTarget();
    }

    // Claim and start a job. Returns true if the job was successfully claimed and started.
    public bool StartJob(Job job, Settlement home)
    {
        if (job == null) return false;
        if (job.isClaimed)
        {
            Debug.LogWarning($"{name}: Tried to start job {job.id} but it is already claimed.");
            return false;
        }
        
        // Check if agent is assigned to a different job type
        if (job.type != assignedJobType)
        {
            Debug.Log($"{name}: Rejected job {job.id} (type {job.type}) - assigned to {assignedJobType} jobs only.");
            return false;
        }

        // Claim the job
        job.isClaimed = true;
        job.claimedByAgentId = name;

        // Assign local fields
        currentJob = job;
        homeSettlement = home;
        carriedAmount = 0;
        stayOnTile = false;

        HexTile targetTile = null;

        // For Haul jobs, look for dropped resources; for Gather jobs, look for harvestable resources
        if (job.type == JobType.Haul)
        {
            targetTile = FindNearestTileWithDroppedResource(job.resource);
            if (targetTile != null)
            {
                ReserveResources(targetTile, job.resource, 1, true);
                job.originTile = targetTile;
                SetTarget(targetTile);
                agentState = AgentState.TravelingToSource;
                Debug.Log($"{name}: Started Haul job {job.id}. Heading to pick up dropped {job.resource} at {targetTile.HexCoordinates}.");
                return true;
            }
            else
            {
                Debug.LogWarning($"{name}: No dropped {job.resource} found. Cannot start Haul job.");
                job.isClaimed = false;
                job.claimedByAgentId = null;
                currentJob = null;
                return false;
            }
        }
        else
        {
            // Gather job: find nearest tile with the resource
            targetTile = FindNearestTileWithResource(job.resource);
            if (targetTile != null)
            {
                ReserveResources(targetTile, job.resource, 1, false);
                job.originTile = targetTile;
                SetTarget(targetTile);
            }
            else if (job.originTile != null)
            {
                // Fallback to original origin tile - try to reserve it
                if (job.originTile.GetAvailableResourceAmount(job.resource) > 0)
                {
                    ReserveResources(job.originTile, job.resource, 1, false);
                    SetTarget(job.originTile);
                }
                else
                {
                    Debug.LogWarning($"{name}: Origin tile has no available {job.resource}. Cannot start job.");
                    job.isClaimed = false;
                    job.claimedByAgentId = null;
                    currentJob = null;
                    return false;
                }
            }
            else
            {
                // No valid tile - unclaim and fail
                Debug.LogWarning($"{name}: No tiles with {job.resource} found. Cannot start job.");
                job.isClaimed = false;
                job.claimedByAgentId = null;
                currentJob = null;
                return false;
            }

            agentState = AgentState.TravelingToSource;
            return true;
        }
    }

    // Convenience overload for calls that don't provide a home settlement yet
    public bool StartJob(Job job)
    {
        return StartJob(job, null);
    }

    // Handle global tick events to progress harvesting and picking up work.
    void HandleTick()
    {
        if (resourceTickManager == null) return;
        // Movement should happen on ticks if enabled
        if (useTickMovement)
        {
            HandleMovementTick();
            // Continue to harvest/pickup logic below after movement
        }
        
        // Handle Harvesting state
        if (agentState == AgentState.Harvesting)
        {
            HandleHarvestTick();
            return;
        }
        
        // Handle PickingUp state
        if (agentState == AgentState.PickingUp)
        {
            HandlePickupTick();
            return;
        }
    }

    void HandleMovementTick()
    {
        if (targetTile == null)
        {
            isInterpolatingMovement = false;
            return;
        }
        if (stayOnTile)
        {
            isInterpolatingMovement = false;
            return;
        }

        Vector3 worldTargetPos = targetTile.transform.position;
        // moveSpeed is in tiles per tick - agent moves this many tiles each tick regardless of tick interval
        // Faster ticks = agent visually moves faster, slower ticks = agent visually moves slower
        float perTickDistance = moveSpeed;

        // Calculate where we'll be at the end of this tick
        Vector3 currentPos = tickEndPosition; // Use the previous tick's end position
        Vector3 newEndPos = Vector3.MoveTowards(currentPos, worldTargetPos, perTickDistance);
        
        // Set up interpolation from current visual position to new end position
        tickStartPosition = transform.position;
        tickEndPosition = newEndPos;
        isInterpolatingMovement = true;

        // Check if we've arrived (will complete at end of interpolation)
        if (Vector3.SqrMagnitude(newEndPos - worldTargetPos) < 0.0001f)
        {
            // We'll arrive at the target this tick - handle arrival after interpolation completes
            // For now, mark that we should arrive. The actual ArriveAtTarget will be called
            // when interpolation completes (at progress = 1)
        }
    }
    
    // Called at the end of tick interpolation to finalize arrival if needed
    void CheckArrivalAfterInterpolation()
    {
        if (!isInterpolatingMovement || targetTile == null) return;
        
        Vector3 worldTargetPos = targetTile.transform.position;
        if (Vector3.SqrMagnitude(tickEndPosition - worldTargetPos) < 0.0001f)
        {
            isInterpolatingMovement = false;
            transform.position = tickEndPosition;
            ArriveAtTarget();
        }
    }

    void HandleHarvestTick()
    {
        if (currentJob == null || currentTile == null)
        {
            // Abort harvesting if job or tile is missing
            if (currentJob != null)
            {
                ReleaseCurrentReservation();
                currentJob.isClaimed = false;
                currentJob.claimedByAgentId = null;
                currentJob = null;
            }
            agentState = AgentState.Idle;
            stayOnTile = true;
            PickNewLocalTarget();
            return;
        }

        // Subtract work units scaled by gather multiplier
        float workThisTick = resourceTickManager.workUnitsPerTick * Mathf.Max(0.0001f, gatherMultiplier);
        harvestWorkRemaining -= workThisTick;
        
        if (harvestWorkRemaining <= 0f)
        {
            // Harvest the resource and DROP it on the ground instead of carrying directly
            int taken = currentTile.Harvest(currentJob.resource, 1);
            if (taken > 0)
            {
                currentTile.DropResource(currentJob.resource, taken);
                Debug.Log($"{name}: Harvest complete for job {currentJob.id}. Dropped {taken} {currentJob.resource} on tile.");
            }

            // If this agent is assigned as a Gatherer, they should NEVER pick up - always leave for haulers
            // and continue gathering. Only pick up if no haulers exist at all.
            bool isDesignatedGatherer = (assignedJobType == JobType.Gather);
            bool haulersExist = DoHaulersExist();
            
            if (isDesignatedGatherer && haulersExist)
            {
                Debug.Log($"{name}: Gatherer leaving dropped {currentJob.resource} for haulers. Continuing to next tree.");
                // Release current job and start a new gather job
                ReleaseCurrentReservation();
                currentJob.isClaimed = false;
                currentJob.claimedByAgentId = null;
                currentJob = null;
                carriedAmount = 0;
                
                // Immediately start another gather job
                if (autoRepeatJob)
                {
                    TryStartAssignedJob();
                }
                else
                {
                    agentState = AgentState.Idle;
                    stayOnTile = true;
                    PickNewLocalTarget();
                }
                return;
            }

            // No haulers exist - gatherer picks up themselves
            pickupWorkRemaining = basePickupWorkUnits;
            agentState = AgentState.PickingUp;
            Debug.Log($"{name}: No haulers available. Starting to pick up dropped {currentJob.resource} (work={pickupWorkRemaining:F2}).");
        }
    }

    // Check if any hauler agents exist in the scene (not just available, but assigned as haulers)
    bool DoHaulersExist()
    {
        var agents = FindObjectsOfType<PopulationAgent>();
        foreach (var agent in agents)
        {
            if (agent == this) continue;
            if (agent == null) continue;
            
            // Check if this agent is assigned as a hauler
            if (agent.assignedJobType == JobType.Haul)
            {
                return true;
            }
        }
        return false;
    }

    void HandlePickupTick()
    {
        if (currentJob == null || currentTile == null)
        {
            // Abort if job or tile is missing
            if (currentJob != null)
            {
                ReleaseCurrentReservation();
                currentJob.isClaimed = false;
                currentJob.claimedByAgentId = null;
                currentJob = null;
            }
            carriedAmount = 0;
            agentState = AgentState.Idle;
            stayOnTile = true;
            PickNewLocalTarget();
            return;
        }

        // Subtract work units
        float workThisTick = resourceTickManager.workUnitsPerTick * Mathf.Max(0.0001f, gatherMultiplier);
        pickupWorkRemaining -= workThisTick;
        
        if (pickupWorkRemaining <= 0f)
        {
            // Pick up the dropped resource
            int pickedUp = currentTile.PickupDropped(currentJob.resource, 1);
            carriedAmount = pickedUp;
            Debug.Log($"{name}: Picked up {pickedUp} {currentJob.resource}. Now carrying to settlement.");

            // Switch to carrying state and head to home settlement
            agentState = AgentState.CarryingToSettlement;
            if (homeSettlement != null)
            {
                var homeTile = homeSettlement.GetComponent<HexTile>();
                if (homeTile != null) SetTarget(homeTile);
            }
            else
            {
                // No home provided: become idle
                Debug.LogWarning($"{name}: No home settlement assigned for job {currentJob.id}. Dropping job.");
                ReleaseCurrentReservation();
                currentJob.isClaimed = false;
                currentJob.claimedByAgentId = null;
                currentJob = null;
                carriedAmount = 0;
                agentState = AgentState.Idle;
                stayOnTile = true;
                PickNewLocalTarget();
            }
        }
    }
    
    // Helper: sample an index from a probability chart and map to optional values array safely.
    // Returns fallbackValue if sampling/mapping cannot produce a valid result.
    int SampleStatFromChart(float[] chart, int[] valueMap, int fallbackValue)
    {
        int idx = RandomUtil.PickIndexFromChart(chart);
        if (valueMap != null && valueMap.Length > 0)
        {
            if (idx < 0) return fallbackValue;
            if (idx >= valueMap.Length)
            {
                // lengths mismatch; clamp to last available value and warn
                Debug.LogWarning($"PopulationAgent: valueMap length ({valueMap.Length}) smaller than sampled index ({idx}). Clamping to last value.");
                return valueMap[valueMap.Length - 1];
            }
            return valueMap[idx];
        }
        // No mapping provided; use the sampled index as the stat (or fallback if negative)
        return Mathf.Max(fallbackValue, idx);
    }

    // Find nearest tile with food resources
    HexTile FindNearestFoodTile()
    {
        return FindNearestTileWithResource(Managers.ResourceManager.GameResource.Food);
    }

    // Find nearest tile with a specific resource (checks available/unreserved amount)
    HexTile FindNearestTileWithResource(Managers.ResourceManager.GameResource resource)
    {
        if (currentTile == null) return null;

        var gen = FindFirstObjectByType<HexGridGenerator>();
        if (gen == null || gen.tiles == null) return null;

        HexTile nearest = null;
        float minDist = float.MaxValue;

        foreach (var tile in gen.tiles.Values)
        {
            if (tile == null) continue;
            int amount = tile.GetAvailableResourceAmount(resource);
            if (amount > 0)
            {
                float dist = Vector3.Distance(transform.position, tile.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = tile;
                }
            }
        }

        return nearest;
    }

    // Find nearest tile with dropped resources of a specific type (checks available/unreserved amount)
    HexTile FindNearestTileWithDroppedResource(Managers.ResourceManager.GameResource resource)
    {
        if (currentTile == null) return null;

        var gen = FindFirstObjectByType<HexGridGenerator>();
        if (gen == null || gen.tiles == null) return null;

        HexTile nearest = null;
        float minDist = float.MaxValue;

        foreach (var tile in gen.tiles.Values)
        {
            if (tile == null) continue;
            int amount = tile.GetAvailableDroppedAmount(resource);
            if (amount > 0)
            {
                float dist = Vector3.Distance(transform.position, tile.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = tile;
                }
            }
        }

        return nearest;
    }

    #region Resource Reservations
    /// <summary>
    /// Reserve resources on a tile for this agent. Call when starting a job.
    /// </summary>
    void ReserveResources(HexTile tile, Managers.ResourceManager.GameResource resource, int amount, bool isDropped)
    {
        if (tile == null || amount <= 0) return;
        
        // Release any existing reservation first
        ReleaseCurrentReservation();
        
        bool success;
        if (isDropped)
            success = tile.ReserveDroppedResource(resource, amount);
        else
            success = tile.ReserveResource(resource, amount);
            
        if (success)
        {
            reservedTile = tile;
            reservedResourceType = resource;
            reservedAmount = amount;
            isDroppedReservation = isDropped;
        }
    }
    
    /// <summary>
    /// Release the current reservation (call when job completes, cancels, or agent dies).
    /// </summary>
    void ReleaseCurrentReservation()
    {
        if (reservedTile == null || reservedAmount <= 0) return;
        
        if (isDroppedReservation)
            reservedTile.ReleaseDroppedReservation(reservedResourceType, reservedAmount);
        else
            reservedTile.ReleaseReservation(reservedResourceType, reservedAmount);
            
        reservedTile = null;
        reservedAmount = 0;
    }
    #endregion

    #region Carried Item Visual
    void CreateCarriedItemVisual()
    {
        carriedItemVisual = new GameObject("CarriedItemVisual");
        carriedItemVisual.transform.SetParent(transform);
        carriedItemVisual.transform.localPosition = new Vector3(0, 0.35f, 0); // Above agent's head
        carriedItemVisual.transform.localScale = Vector3.one * 0.4f; // Smaller than regular icons

        carriedItemRenderer = carriedItemVisual.AddComponent<SpriteRenderer>();
        carriedItemRenderer.sortingOrder = 2500; // Above agents
        carriedItemVisual.SetActive(false); // Hidden by default
    }

    void UpdateCarriedItemVisual()
    {
        if (carriedItemVisual == null || carriedItemRenderer == null) return;

        if (carriedAmount > 0 && currentJob != null)
        {
            // Show appropriate sprite based on carried resource type from current job
            Sprite spriteToUse = null;
            if (currentJob.resource == Managers.ResourceManager.GameResource.Materials)
                spriteToUse = carriedLogSprite;
            else if (currentJob.resource == Managers.ResourceManager.GameResource.Food)
                spriteToUse = carriedFoodSprite;

            if (spriteToUse != null)
            {
                carriedItemRenderer.sprite = spriteToUse;
                carriedItemVisual.SetActive(true);
            }
            else
            {
                carriedItemVisual.SetActive(false);
            }
        }
        else
        {
            carriedItemVisual.SetActive(false);
        }
    }
    #endregion
}
