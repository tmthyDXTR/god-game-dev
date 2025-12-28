using System.Collections.Generic;
using UnityEngine;

namespace HexGrid
{
    /// <summary>
    /// A* pathfinding for hex grids.
    /// Uses the HexGridGenerator.tiles dictionary to determine valid tiles.
    /// </summary>
    public static class HexPathfinding
    {
        /// <summary>
        /// Find the shortest path from start to goal using A* algorithm.
        /// Returns a list of Hex coordinates representing the path (including start and goal).
        /// Returns null if no path exists.
        /// </summary>
        public static List<Hex> FindPath(Hex start, Hex goal, HexGridGenerator gridGenerator, System.Func<HexTile, bool> isWalkable = null)
        {
            if (gridGenerator == null || gridGenerator.tiles == null)
                return null;

            // Default walkability: tile exists
            if (isWalkable == null)
                isWalkable = (tile) => tile != null;

            // Check start and goal are valid
            if (!gridGenerator.tiles.ContainsKey(start) || !gridGenerator.tiles.ContainsKey(goal))
                return null;

            if (!isWalkable(gridGenerator.tiles[start]) || !isWalkable(gridGenerator.tiles[goal]))
                return null;

            // A* data structures
            var openSet = new HashSet<Hex> { start };
            var cameFrom = new Dictionary<Hex, Hex>();
            var gScore = new Dictionary<Hex, float> { [start] = 0 };
            var fScore = new Dictionary<Hex, float> { [start] = HexDistance(start, goal) };

            // Priority queue using simple list (could optimize with proper heap)
            var openList = new List<Hex> { start };

            while (openList.Count > 0)
            {
                // Get node with lowest fScore
                openList.Sort((a, b) => fScore.GetValueOrDefault(a, float.MaxValue).CompareTo(fScore.GetValueOrDefault(b, float.MaxValue)));
                Hex current = openList[0];

                if (current.Equals(goal))
                {
                    // Reconstruct path
                    return ReconstructPath(cameFrom, current);
                }

                openList.RemoveAt(0);
                openSet.Remove(current);

                // Check all 6 neighbors
                for (int dir = 0; dir < 6; dir++)
                {
                    Hex neighbor = current.Neighbor(dir);

                    // Check if neighbor exists and is walkable
                    if (!gridGenerator.tiles.TryGetValue(neighbor, out HexTile neighborTile))
                        continue;
                    if (!isWalkable(neighborTile))
                        continue;

                    // Calculate tentative gScore (each hex step costs 1)
                    float tentativeGScore = gScore.GetValueOrDefault(current, float.MaxValue) + 1f;

                    if (tentativeGScore < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                    {
                        // This path is better
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + HexDistance(neighbor, goal);

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                            openList.Add(neighbor);
                        }
                    }
                }
            }

            // No path found
            return null;
        }

        /// <summary>
        /// Find path and return the HexTile objects instead of Hex coordinates.
        /// </summary>
        public static List<HexTile> FindPathTiles(HexTile start, HexTile goal, HexGridGenerator gridGenerator, System.Func<HexTile, bool> isWalkable = null)
        {
            if (start == null || goal == null)
                return null;

            var hexPath = FindPath(start.HexCoordinates, goal.HexCoordinates, gridGenerator, isWalkable);
            if (hexPath == null)
                return null;

            var tilePath = new List<HexTile>();
            foreach (var hex in hexPath)
            {
                if (gridGenerator.tiles.TryGetValue(hex, out HexTile tile))
                    tilePath.Add(tile);
            }
            return tilePath;
        }

        /// <summary>
        /// Calculate hex distance (Manhattan distance for hex grids).
        /// </summary>
        public static int HexDistance(Hex a, Hex b)
        {
            return (Mathf.Abs(a.Q - b.Q) + Mathf.Abs(a.R - b.R) + Mathf.Abs(a.S - b.S)) / 2;
        }

        private static List<Hex> ReconstructPath(Dictionary<Hex, Hex> cameFrom, Hex current)
        {
            var path = new List<Hex> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            return path;
        }
    }

    /// <summary>
    /// Extension methods for Dictionary to support GetValueOrDefault.
    /// </summary>
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default)
        {
            return dict.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
    }
}
