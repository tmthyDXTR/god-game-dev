using System.Collections.Generic;
using UnityEngine;
using HexGrid;

public class PopulationManager : MonoBehaviour
{
    public static PopulationManager Instance { get; private set; }

    [Header("Pool Settings")]
    public int initialPool = 20;
    [Header("Prefab (optional)")]
    [Tooltip("If provided, this prefab will be instantiated for agents. If null, a simple runtime agent GameObject will be created.")]
    public GameObject populationAgentPrefab;

    Queue<GameObject> pool = new Queue<GameObject>();

    [Header("Job Scheduler")]
    [Tooltip("Seconds between job assignment scans")]
    public float jobScanInterval = 1f;
    float jobScanTimer = 0f;



    // shared sprite for runtime agents (small black rectangle)
    Sprite agentSprite;

    HexGridGenerator gridGenerator;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);

        // Only create the shared runtime sprite if we will generate procedural agents
        if (populationAgentPrefab == null)
            CreateAgentSprite();

        // find grid generator in scene if present
        gridGenerator = FindFirstObjectByType<HexGridGenerator>();

        for (int i = 0; i < initialPool; i++)
        {
            var go = CreateAgentGO();
            go.SetActive(false);
            pool.Enqueue(go);
        }
        Debug.Log($"PopulationManager: Awake prewarmed pool with {pool.Count} agents. Prefab assigned: {populationAgentPrefab != null}");
    }

    void CreateAgentSprite()
    {
        var tex = new Texture2D(4, 4);
        Color32[] cols = new Color32[4 * 4];
        for (int i = 0; i < cols.Length; i++) cols[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(cols);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        agentSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
    }

    GameObject CreateAgentGO()
    {
        GameObject go;
        if (populationAgentPrefab != null)
        {
            // instantiate prefab; ensure it has a PopulationAgent component
            go = Instantiate(populationAgentPrefab);
            // rename
            go.name = "worker";
            if (go.GetComponent<PopulationAgent>() == null)
                go.AddComponent<PopulationAgent>();
            Debug.Log($"PopulationManager: Instantiated prefab for agent '{go.name}'");
        }
        else
        {
            go = new GameObject("PopulationAgent");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = agentSprite;
            sr.color = Color.black;
            go.transform.localScale = Vector3.one * 0.04f;
            go.AddComponent<PopulationAgent>();
            Debug.Log($"PopulationManager: Created procedural agent '{go.name}'");
        }
        go.transform.parent = GameObject.Find("Population")?.transform ?? transform;
        return go;
    }

    // spawn agent on the given tile. If stayOnTile is true (default), agent will not move
    // until `StartAgentMovement` is called for that agent.
    public PopulationAgent SpawnAgent(HexTile tile, bool stayOnTile = true)
    {
        Debug.Log($"PopulationManager: SpawnAgent requested. Pool before spawn: {pool.Count}");
        GameObject go = pool.Count > 0 ? pool.Dequeue() : CreateAgentGO();
        go.SetActive(true);
        Debug.Log($"PopulationManager: SpawnAgent dequeued '{go.name}'. Active: {go.activeSelf}");
        var agent = go.GetComponent<PopulationAgent>();
        agent.Initialize(tile, stayOnTile);
        tile.OnPopulationEnter(agent);
        Debug.Log($"PopulationManager: Spawned agent '{agent.name}' at tile {tile.HexCoordinates}. Pool now: {pool.Count}");
        return agent;
    }

    void Update()
    {
        // Job scheduler runs periodically to assign queued jobs to nearest idle agents
        jobScanTimer += Time.deltaTime;
        if (jobScanTimer >= jobScanInterval)
        {
            jobScanTimer = 0f;
            RunJobScheduler();
        }
    }

    void RunJobScheduler()
    {
        // Find all settlements with queued jobs
        var settlements = FindObjectsOfType<Settlement>();
        if (settlements == null || settlements.Length == 0) return;

        // Cache all agents for assignment lookup
        var agents = FindObjectsOfType<PopulationAgent>();
        if (agents == null || agents.Length == 0) return;

        // First, auto-create Haul jobs for any dropped resources if there are designated haulers
        AutoCreateHaulJobs(settlements);

        foreach (var s in settlements)
        {
            if (s == null) continue;
            // Try to assign as many jobs as possible down the queue until no idle agents remain
            bool progress = true;
            while (progress && s.QueuedJobCount > 0)
            {
                progress = false;
                
                // Peek the next job to find a suitable agent
                var nextJob = s.PeekNextJob();
                if (nextJob == null) break;
                
                // Find nearest idle agent that can take this job type
                PopulationAgent nearest = null;
                float bestDist = float.MaxValue;
                foreach (var a in agents)
                {
                    if (a == null) continue;
                    if (a.agentState != PopulationAgent.AgentState.Idle) continue;
                    if (a.currentTile == null) continue;
                    
                    // Check if agent is assigned to this job type
                    if (a.assignedJobType != nextJob.type)
                        continue; // Agent assigned to different job type
                    
                    float dist = Vector3.Distance(a.currentTile.transform.position, s.transform.position);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        nearest = a;
                    }
                }

                if (nearest == null) break; // no compatible idle agents available for this job

                // Dequeue a job and attempt to assign
                if (s.TryDequeueJob(out Job job))
                {
                    if (job == null) continue;
                    bool started = nearest.StartJob(job, s);
                    if (!started)
                    {
                        // failed to claim/start job - re-enqueue and stop trying for now
                        s.EnqueueJob(job);
                        break;
                    }
                    else
                    {
                        progress = true; // we assigned a job; attempt next in queue
                        Debug.Log($"PopulationManager: Assigned job {job.id} ({job.type}) to agent {nearest.name}");
                    }
                }
            }
        }
    }

    // Auto-create Haul jobs for dropped resources if there are haulers available
    void AutoCreateHaulJobs(Settlement[] settlements)
    {
        if (gridGenerator == null || gridGenerator.tiles == null) return;
        
        // Check if there are any designated haulers
        bool hasHaulers = false;
        var agents = FindObjectsOfType<PopulationAgent>();
        foreach (var a in agents)
        {
            if (a != null && a.assignedJobType == HexGrid.JobType.Haul)
            {
                hasHaulers = true;
                break;
            }
        }
        
        if (!hasHaulers) return; // No haulers, gatherers will pick up themselves
        
        // Find first settlement to queue haul jobs
        Settlement targetSettlement = null;
        foreach (var s in settlements)
        {
            if (s != null) { targetSettlement = s; break; }
        }
        if (targetSettlement == null) return;
        
        // Check all tiles for dropped resources
        foreach (var tile in gridGenerator.tiles.Values)
        {
            if (tile == null) continue;
            
            // Check for dropped materials
            int droppedMaterials = tile.GetDroppedAmount(Managers.ResourceManager.GameResource.Materials);
            if (droppedMaterials > 0)
            {
                // Check if there's already a haul job for this tile
                bool hasExistingJob = false;
                foreach (var a in agents)
                {
                    if (a != null && a.currentJob != null && 
                        a.currentJob.type == HexGrid.JobType.Haul && 
                        a.currentJob.originTile == tile)
                    {
                        hasExistingJob = true;
                        break;
                    }
                }
                
                if (!hasExistingJob)
                {
                    // Create a haul job for the dropped resource
                    var haulJob = new HexGrid.Job
                    {
                        type = HexGrid.JobType.Haul,
                        resource = Managers.ResourceManager.GameResource.Materials,
                        amount = 1,
                        originTile = tile,
                        priority = 1 // Higher priority than gather
                    };
                    targetSettlement.EnqueueJob(haulJob);
                    Debug.Log($"PopulationManager: Auto-created Haul job for dropped Materials at {tile.HexCoordinates}");
                }
            }
        }
    }

    // Start movement for an agent that was spawned idle.
    public void StartAgentMovement(PopulationAgent agent)
    {
        if (agent == null) return;
        // tell agent to leave its tile and then request next move
        agent.StartMovement();
    }

    public void DespawnAgent(PopulationAgent agent)
    {
        if (agent == null) return;
        // Inform the tile that the agent is leaving so populationCount stays correct
        try
        {
            if (agent.currentTile != null)
            {
                agent.currentTile.OnPopulationLeave(agent);
                agent.currentTile = null;
            }
        }
        catch { }
        agent.gameObject.SetActive(false);
        pool.Enqueue(agent.gameObject);
    }


    // Utility: quick population counts for a tile
    public int GetPopulationCount(HexTile tile)
    {
        return tile != null ? tile.populationCount : 0;
    }
}
