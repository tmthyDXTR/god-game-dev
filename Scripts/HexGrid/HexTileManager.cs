using System.Collections.Generic;
using UnityEngine;

namespace HexGrid
{
    public abstract class HexTileManager : MonoBehaviour
    {
        protected HexTile selectedTile;

        // Abstract method to get tiles in range
        public abstract List<Hex> GetTilesInRange(Hex center, int range);

        public virtual void SetSelectedTile(HexTile tile)
        {
            selectedTile = tile;
        }
    }

    public class DefaultHexTileManager : HexTileManager
    {
        public static DefaultHexTileManager Instance { get; private set; }
        public HexTile SelectedTile { get; private set; }
        public HexGridGenerator gridGenerator;
        public GodBeast.GodBeast godBeast;
        [Header("Tiles in Range")]
        [SerializeField]
        public List<HexTile> tilesInRange = new List<HexTile>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            UpdateTilesInRange();
        }

        public override void SetSelectedTile(HexTile tile)
        {
            SelectedTile = tile;
            if (tile == null)
            {
                Debug.Log("Deselected tile.");
                return;
            }
            Debug.Log($"Selected tile at {tile.HexCoordinates} of type {tile.TileType}");
        }

        public void UpdateTilesInRange()
        {
            tilesInRange.Clear();
            if (godBeast != null && gridGenerator != null)
            {
                var hexes = GetTilesInRange(godBeast.HexCoordinates, 1);
                foreach (var hex in hexes)
                {
                    if (gridGenerator.tiles.TryGetValue(hex, out var tile))
                        tilesInRange.Add(tile);
                }
            }
        }

        public override List<Hex> GetTilesInRange(Hex center, int range)
        {
            var result = new List<Hex>();
            if (range < 1) return result;
            if (range == 1)
            {
                for (int dir = 0; dir < 6; dir++)
                {
                    var neighbor = center.Neighbor(dir);
                    if (gridGenerator.tiles.ContainsKey(neighbor))
                        result.Add(neighbor);
                }
            }
            else
            {
                var visited = new HashSet<Hex> { center };
                var frontier = new List<Hex> { center };
                for (int k = 0; k < range; k++)
                {
                    var nextFrontier = new List<Hex>();
                    foreach (var hex in frontier)
                    {
                        for (int dir = 0; dir < 6; dir++)
                        {
                            var neighbor = hex.Neighbor(dir);
                            if (!visited.Contains(neighbor) && gridGenerator.tiles.ContainsKey(neighbor))
                            {
                                visited.Add(neighbor);
                                nextFrontier.Add(neighbor);
                            }
                        }
                    }
                    frontier = nextFrontier;
                }
                visited.Remove(center);
                result.AddRange(visited);
            }
            return result;
        }
    }
}
