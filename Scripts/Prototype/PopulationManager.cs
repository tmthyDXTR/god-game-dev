using System.Collections.Generic;
using UnityEngine;
using HexGrid;

public class PopulationManager : MonoBehaviour
{
    public static PopulationManager Instance { get; private set; }

    [Header("Pool Settings")]
    public int initialPool = 20;

    Queue<GameObject> pool = new Queue<GameObject>();

    [Header("Movement Weights")]
    public float faithAttraction = 2f;   // multiplier toward faith
    public float heresyRepulsion = 1.5f;// multiplier to avoid heresy

    // shared sprite for runtime agents (small black rectangle)
    Sprite agentSprite;

    HexGridGenerator gridGenerator;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);

        CreateAgentSprite();

        // find grid generator in scene if present
        gridGenerator = FindFirstObjectByType<HexGridGenerator>();

        for (int i = 0; i < initialPool; i++)
        {
            var go = CreateAgentGO();
            go.SetActive(false);
            pool.Enqueue(go);
        }
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
        var go = new GameObject("PopulationAgent");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = agentSprite;
        sr.color = Color.black;
        go.transform.localScale = Vector3.one * 0.04f;
        go.AddComponent<PopulationAgent>();
        go.transform.parent = GameObject.Find("Population")?.transform ?? transform;
        return go;
    }

    // spawn agent on the given tile. If stayOnTile is true (default), agent will not move
    // until `StartAgentMovement` is called for that agent.
    public PopulationAgent SpawnAgent(HexTile tile, bool stayOnTile = true)
    {
        GameObject go = pool.Count > 0 ? pool.Dequeue() : CreateAgentGO();
        go.SetActive(true);
        var agent = go.GetComponent<PopulationAgent>();
        agent.Initialize(tile, stayOnTile);
        tile.OnPopulationEnter(agent);
        if (!stayOnTile)
            RequestNextMove(agent, tile);
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

    // Called by agent after arriving on a tile
    public void RequestNextMove(PopulationAgent agent, HexTile current)
    {
        if (gridGenerator == null)
        {
            gridGenerator = FindFirstObjectByType<HexGridGenerator>();
            if (gridGenerator == null) return;
        }

        var neighbors = new List<HexTile>();
        if (current != null)
        {
            var center = current.HexCoordinates;
            for (int dir = 0; dir < 6; dir++)
            {
                var nHex = center.Neighbor(dir);
                if (gridGenerator.tiles.TryGetValue(nHex, out var nTile))
                    neighbors.Add(nTile);
            }
        }

        if (neighbors.Count == 0) return;

        float total = 0f;
        float[] weights = new float[neighbors.Count];
        for (int i = 0; i < neighbors.Count; i++)
        {
            var t = neighbors[i];
            if (t == null || !t.isExplored) { weights[i] = 0f; continue; }
            float w = 1f;
            // use placeholder fields for faith/heresy (if you add those later to HexTile)
            int faith = 0;
            int heresy = 0;
            // If HexTile gains explicit faith/heresy fields in future, replace these lines
            w += faith * faithAttraction;
            w -= heresy * heresyRepulsion;
            weights[i] = Mathf.Max(0.01f, w);
            total += weights[i];
        }

        float r = Random.value * total;
        float acc = 0f;
        for (int i = 0; i < neighbors.Count; i++)
        {
            acc += weights[i];
            if (r <= acc)
            {
                agent.SetTarget(neighbors[i]);
                return;
            }
        }

        agent.SetTarget(neighbors[0]);
    }

    // Utility: quick population counts for a tile
    public int GetPopulationCount(HexTile tile)
    {
        return tile != null ? tile.populationCount : 0;
    }
}
