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
