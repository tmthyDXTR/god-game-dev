using UnityEngine;
using HexGrid;

public class PopulationTestSpawner : MonoBehaviour
{
    public int spawnCount = 8;

    void Start()
    {
        // find existing HexTile objects in the scene
        var tiles = FindObjectsByType<HexTile>(FindObjectsSortMode.None);
        if (tiles == null || tiles.Length == 0)
        {
            // if no tiles exist, create a tiny 3x3 grid for quick tests
            tiles = CreateSimpleTileGrid(3, 3, 1.0f);
        }

        // Try to spawn all agents at the god-beast origin tile (0,0)
        var gen = FindFirstObjectByType<HexGridGenerator>();
        Hex origin = new Hex(0, 0);
        HexTile spawnTile = null;
        if (gen != null && gen.tiles != null && gen.tiles.TryGetValue(origin, out var originTile))
        {
            spawnTile = originTile;
        }

        if (spawnTile != null)
        {
            for (int i = 0; i < spawnCount; i++)
                PopulationManager.Instance.SpawnAgent(spawnTile);
        }
        else
        {
            for (int i = 0; i < spawnCount; i++)
            {
                var t = tiles[Random.Range(0, tiles.Length)];
                PopulationManager.Instance.SpawnAgent(t);
            }
        }
    }

    HexGrid.HexTile[] CreateSimpleTileGrid(int w, int h, float spacing)
    {
        var list = new HexGrid.HexTile[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var go = new GameObject($"Tile_{x}_{y}");
                go.transform.position = new Vector3((x - w / 2f) * spacing, (y - h / 2f) * spacing, 0);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.color = new Color(0, 0.5f, 0, 0.3f);
                var tile = go.AddComponent<HexGrid.HexTile>();
                list[y * w + x] = tile;
            }
        }

        // Optional: populate HexGridGenerator.tiles if present so neighbors can be resolved
        var gen = FindFirstObjectByType<HexGrid.HexGridGenerator>();
        if (gen != null)
        {
            foreach (var t in list)
            {
                // Attempt to add to generator.tiles using its HexCoordinates if set; otherwise skip
                if (t.HexCoordinates != null)
                {
                    gen.tiles[t.HexCoordinates] = t;
                }
            }
        }

        return list;
    }
}
