using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HexGrid
{
    public enum GridShape { Hexagon, RectanglePointy, RectangleFlat, Triangle, Parallelogram }

    public enum GridOrientation { Pointy, Flat }

    public class HexGridGenerator : MonoBehaviour
    {
        [Header("Startup Options")]
        public bool generateOnStart = false;
        public bool loadMapOnStart = false;
        // Save current tile types and resources to a MapData asset
        public void SaveTileTypesToMap(MapData mapData)
        {
            mapData.tiles.Clear();
            foreach (var kvp in tiles)
            {
                var record = new TileTypeRecord
                {
                    q = kvp.Key.q,
                    r = kvp.Key.r,
                    s = kvp.Key.s,
                    type = kvp.Value.TileType
                };
                // Save resources for this tile
                foreach (var res in kvp.Value.resourceAmounts)
                {
                    record.resources.Add(new ResourceRecord { type = res.Key, amount = res.Value });
                }
                mapData.tiles.Add(record);
            }
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(mapData);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
            Debug.Log($"Saved {mapData.tiles.Count} tile types/resources to map data asset.");
        }

        // Load tile types and resources from a MapData asset
        public void LoadTileTypesFromMap(MapData mapData)
        {
            foreach (var record in mapData.tiles)
            {
                var hex = new Hex(record.q, record.r, record.s);
                if (tiles.TryGetValue(hex, out var tile))
                {
                    tile.TileType = record.type;
                    tile.resourceAmounts.Clear();
                    foreach (var r in record.resources)
                    {
                        tile.resourceAmounts[r.type] = r.amount;
                    }
                    tile.UpdateVisual();
                }
            }
            Debug.Log($"Loaded {mapData.tiles.Count} tile types/resources from map data asset.");
        }

        // Remove all resources from all tiles
        public void RemoveAllResources()
        {
            foreach (var tile in tiles.Values)
            {
                tile.resourceAmounts.Clear();
                tile.UpdateVisual();
            }
        }

        // Generic method to add resources to tiles of a given type using a probability chart
        public void AddResourceToTiles(ResourceType resourceType, HexTileType targetTileType, float[] probabilityChart)
        {
            var rand = new System.Random();
            int forestCount = 0;
            int resourceCount = 0;
            foreach (var tile in tiles.Values)
            {
                if (tile.TileType == targetTileType)
                {
                    // Reset resource amount before assigning new value
                    tile.RemoveResource(resourceType, tile.GetResourceAmount(resourceType));
                    forestCount++;
                    int amount = GetRandomResourceAmount(rand, probabilityChart);
                    if (amount > 0)
                    {
                        tile.AddResource(resourceType, amount);
                        resourceCount++;
                        Debug.Log($"Added {amount} of {resourceType} to tile at {tile.HexCoordinates}");
                    }
                }
            }
            Debug.Log($"Processed {forestCount} {targetTileType} tiles, added resources to {resourceCount} tiles.");
        }

        // Helper for probability chart (e.g. [0.4f, 0.3f, 0.2f, 0.1f] for 0-3)
        private int GetRandomResourceAmount(System.Random rand, float[] probabilityChart)
        {
            double roll = rand.NextDouble();
            float cumulative = 0f;
            for (int i = 0; i < probabilityChart.Length; i++)
            {
                cumulative += probabilityChart[i];
                if (roll < cumulative)
                    return i; // allow zero as valid amount
            }
            return 0; // fallback to zero
        }

        public void AddFoodResourcesToForestTiles()
        {
            float[] foodProbabilities = { 0.0f, 0.5f, 0.2f, 0.1f };
            AddResourceToTiles(ResourceType.Food, HexTileType.Forest, foodProbabilities);
        }

        public void AddSapResourcesToForestTiles()
        {
            float[] sapProbabilities = { 0.6f, 0.3f, 0.05f, 0.2f };
            AddResourceToTiles(ResourceType.Sap, HexTileType.Forest, sapProbabilities);
        }

        
        [Header("Paint Tool")]
        public HexTileType paintTileType = HexTileType.Grass;
        public bool paintMode = false;
        [Header("Grid Settings")]
        public GridShape gridShape = GridShape.Hexagon;
        public GridOrientation orientation = GridOrientation.Pointy;
        public int gridRadius = 5; // For hexagon/triangle
        public int width = 10;     // For rectangle/parallelogram
        public int height = 10;    // For rectangle/parallelogram
        public int q1 = 0, q2 = 5, r1 = 0, r2 = 5; // For parallelogram

        [Header("Tile Settings")]
        public GameObject hexTilePrefab;
        public float tileSizeX = 1f;
        public float tileSizeY = 1f;
        public Vector2 origin = Vector2.zero;
        public Vector3 tileScale = Vector3.one;

        [HideInInspector]
        public Layout layout;
        [HideInInspector]
        public Dictionary<Hex, HexTile> tiles = new Dictionary<Hex, HexTile>();

        [Header("Map Data")]
        public MapData mapData;

        private void Awake()
        {
            UpdateLayout();
            if (generateOnStart)
            {
                GenerateGrid();
            }
            if (loadMapOnStart && mapData != null)
            {
                LoadTileTypesFromMap(mapData);
            }
        }

        public void UpdateLayout()
        {
            layout = new Layout(
                orientation == GridOrientation.Pointy ? Orientation.Pointy : Orientation.Flat,
                new Point(tileSizeX, tileSizeY),
                new Point(origin.x, origin.y)
            );
        }

        public void GenerateGrid()
        {
            UpdateLayout();
            ClearGrid();
            HashSet<Hex> hexes = null;
            switch (gridShape)
            {
                case GridShape.Hexagon:
                    hexes = HexMap.Hexagon(gridRadius);
                    break;
                case GridShape.RectanglePointy:
                    hexes = HexMap.RectanglePointy(0, width - 1, 0, height - 1);
                    break;
                case GridShape.RectangleFlat:
                    hexes = HexMap.RectangleFlat(0, width - 1, 0, height - 1);
                    break;
                case GridShape.Triangle:
                    hexes = HexMap.Triangle(gridRadius);
                    break;
                case GridShape.Parallelogram:
                    hexes = HexMap.Parallelogram(q1, q2, r1, r2);
                    break;
            }
            if (hexes == null) return;
                Debug.Log($"HexGridGenerator.GenerateGrid called on {gameObject.name}, hexes count: {hexes.Count}");
            foreach (var hex in hexes)
            {
                Vector3 pos = ToWorldPosition(hex);
                var tileObj = Instantiate(hexTilePrefab, pos, Quaternion.identity, transform);
                tileObj.transform.localScale = tileScale;
                var tile = tileObj.GetComponent<HexTile>();
                if (tile != null)
                {
                    tile.HexCoordinates = hex;
                    tiles[hex] = tile;
                }
            }
                Debug.Log($"HexGridGenerator {gameObject.name} tiles populated: {tiles.Count}");
        }

        public void ClearGrid()
        {
            // Collect children first to avoid modifying collection during iteration
            var children = new List<GameObject>();
            foreach (Transform child in transform)
            {
                children.Add(child.gameObject);
            }
            foreach (var obj in children)
            {
                DestroyImmediate(obj);
            }
            tiles.Clear();
        }

        public Vector3 ToWorldPosition(Hex hex)
        {
            var point = layout.HexToPixel(hex);
            return new Vector3((float)point.X, (float)point.Y, 0f);
        }

        public void SetTileType(Hex hex, HexTileType type)
        {
            if (tiles.TryGetValue(hex, out var tile))
            {
                tile.TileType = type;
                tile.UpdateVisual(); // Ensure visuals match type after loading
            }
        }

        public HexTileType GetTileType(Hex hex)
        {
            if (tiles.TryGetValue(hex, out var tile))
                return tile.TileType;
            return HexTileType.Grass;
        }
    }
}
