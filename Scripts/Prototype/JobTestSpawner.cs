using UnityEngine;
using HexGrid;

/// <summary>
/// Simple test script to manually spawn wood-gather jobs.
/// Attach to a GameObject in the scene and call SpawnTestJob from inspector or debug UI.
/// </summary>
public class JobTestSpawner : MonoBehaviour
{
    [Header("Test Job Settings")]
    [Tooltip("Resource type for the test job")]
    public Managers.ResourceManager.GameResource testResource = Managers.ResourceManager.GameResource.Materials;
    
    [Tooltip("Amount to gather (will be split into 1-unit jobs for testing)")]
    public int testAmount = 3;
    
    [Tooltip("Job type for the test")]
    public JobType testJobType = JobType.Gather;

    [Header("References")]
    [Tooltip("If provided, jobs will be enqueued to this settlement. If null, first found settlement will be used.")]
    public Settlement targetSettlement;

    void Start()
    {
        // Auto-find settlement if not assigned
        if (targetSettlement == null)
            targetSettlement = FindFirstObjectByType<Settlement>();
    }

    /// <summary>
    /// Spawn test jobs for all forest tiles with Materials resources.
    /// Call this from inspector or a debug UI button.
    /// </summary>
    [ContextMenu("Spawn Wood Gather Jobs")]
    public void SpawnWoodGatherJobs()
    {
        var gen = FindFirstObjectByType<HexGridGenerator>();
        if (gen == null || gen.tiles == null)
        {
            Debug.LogError("JobTestSpawner: No HexGridGenerator found in scene.");
            return;
        }

        Settlement settlement = targetSettlement ?? FindFirstObjectByType<Settlement>();
        if (settlement == null)
        {
            Debug.LogError("JobTestSpawner: No Settlement found in scene. Create a settlement first.");
            return;
        }

        int jobsCreated = 0;
        foreach (var tile in gen.tiles.Values)
        {
            if (tile == null) continue;
            
            // Only create jobs for tiles with Materials (wood)
            int available = tile.GetResourceAmount(Managers.ResourceManager.GameResource.Materials);
            if (available > 0)
            {
                // Create a small 1-unit job for testing
                var job = new Job
                {
                    type = JobType.Gather,
                    resource = Managers.ResourceManager.GameResource.Materials,
                    amount = 1,
                    originTile = tile,
                    priority = 0
                };
                settlement.EnqueueJob(job);
                jobsCreated++;
                Debug.Log($"JobTestSpawner: Created job {job.id} for tile {tile.HexCoordinates} (Materials: {available})");
            }
        }

        Debug.Log($"JobTestSpawner: Created {jobsCreated} wood-gather jobs for settlement '{settlement.name}'.");
    }

    /// <summary>
    /// Spawn a single test job for the specified resource and amount.
    /// </summary>
    [ContextMenu("Spawn Single Test Job")]
    public void SpawnSingleTestJob()
    {
        var gen = FindFirstObjectByType<HexGridGenerator>();
        if (gen == null || gen.tiles == null)
        {
            Debug.LogError("JobTestSpawner: No HexGridGenerator found.");
            return;
        }

        Settlement settlement = targetSettlement ?? FindFirstObjectByType<Settlement>();
        if (settlement == null)
        {
            Debug.LogError("JobTestSpawner: No Settlement found. Create a settlement first.");
            return;
        }

        // Find a tile with the desired resource
        HexTile targetTile = null;
        foreach (var tile in gen.tiles.Values)
        {
            if (tile == null) continue;
            if (tile.GetResourceAmount(testResource) > 0)
            {
                targetTile = tile;
                break;
            }
        }

        if (targetTile == null)
        {
            Debug.LogWarning($"JobTestSpawner: No tiles found with {testResource}. Add resources to tiles first.");
            return;
        }

        var job = new Job
        {
            type = testJobType,
            resource = testResource,
            amount = testAmount,
            originTile = targetTile,
            priority = 0
        };
        settlement.EnqueueJob(job);
        Debug.Log($"JobTestSpawner: Created job {job.id} for {testAmount} {testResource} at tile {targetTile.HexCoordinates}. Settlement queue: {settlement.QueuedJobCount}");
    }

    /// <summary>
    /// Print current settlement queue status for debugging.
    /// </summary>
    [ContextMenu("Print Settlement Queue")]
    public void PrintSettlementQueue()
    {
        Settlement settlement = targetSettlement ?? FindFirstObjectByType<Settlement>();
        if (settlement == null)
        {
            Debug.LogError("JobTestSpawner: No Settlement found.");
            return;
        }

        Debug.Log($"Settlement '{settlement.name}' has {settlement.QueuedJobCount} jobs queued.");
        Debug.Log($"Stored resources: {string.Join(", ", settlement.storedResources)}");
    }
}
