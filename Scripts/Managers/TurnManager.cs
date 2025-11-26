using UnityEngine;
using GodBeast;
using UI;
using Inventory;
using HexGrid;

namespace Managers
{
    public class TurnManager : MonoBehaviour
    {
        public GodBeast.GodBeast godBeast;
        public DebugMenuUI debugMenuUI;
        public HexGridGenerator gridGenerator;
        public int turnCount = 0;
        public int sapWarningThreshold = 1;
        public bool isGameOver = false;

        private void Start()
        {
            if (godBeast == null)
                godBeast = FindFirstObjectByType<GodBeast.GodBeast>();
            if (debugMenuUI == null)
                debugMenuUI = FindFirstObjectByType<DebugMenuUI>();

            // Initialize UI to reflect current god-beast resources immediately
            if (debugMenuUI != null && godBeast != null)
            {
                // If GodBeastData defines a per-turn resource, display current amount
                var dataField = godBeast.GetType().GetField("data");
                GameData.GodBeastData data = null;
                if (dataField != null)
                    data = dataField.GetValue(godBeast) as GameData.GodBeastData;
                if (data != null && data.perTurnResource != null)
                {
                    int current = godBeast.GetResourceAmount(data.perTurnResource);
                    debugMenuUI.UpdateSap(current);
                    debugMenuUI.ShowSapWarning(current);
                }
            }
        }

        public void EndTurn()
        {
            if (isGameOver) return;
            turnCount++;
            EndTurnConsume();
            // Spread BoneBloom after resource consumption
            SpreadBoneBloom();
            // Add other end turn logic here (e.g., Warden food, Bloom spread)
        }

        private void SpreadBoneBloom()
        {
            var gen = gridGenerator == null ? FindFirstObjectByType<HexGridGenerator>() : gridGenerator;
            if (gen == null)
            {
                Debug.LogWarning("TurnManager: HexGridGenerator not found; cannot spread BoneBloom.");
                return;
            }

            // Snapshot sources: only tiles that are real Bone tiles should spread
            var sources = new System.Collections.Generic.List<Hex>();
            foreach (var kvp in gen.tiles)
            {
                var tile = kvp.Value;
                if (tile.TileType == HexTileType.Bone)
                    sources.Add(kvp.Key);
            }

            // Choose target neighbors for each source (may select same tile multiple times)
            var targetSelections = new System.Collections.Generic.List<Hex>();
            foreach (var hex in sources)
            {
                var candidates = new System.Collections.Generic.List<Hex>();
                for (int dir = 0; dir < 6; dir++)
                {
                    var n = hex.Neighbor(dir);
                    if (!gen.tiles.ContainsKey(n)) continue;
                    var t = gen.tiles[n];
                    // skip if already full Bone
                    if (t.TileType == HexTileType.Bone) continue;
                    candidates.Add(n);
                }
                if (candidates.Count == 0) continue;
                int idx = UnityEngine.Random.Range(0, candidates.Count);
                targetSelections.Add(candidates[idx]);
            }

            // Aggregate selections to apply multi-source increments
            var increments = new System.Collections.Generic.Dictionary<Hex, int>();
            foreach (var h in targetSelections)
            {
                if (increments.ContainsKey(h)) increments[h]++;
                else increments[h] = 1;
            }

            int totalIncrements = 0;
            foreach (var kvp in increments)
            {
                if (gen.tiles.TryGetValue(kvp.Key, out var tile))
                {
                    tile.IncreaseInfestation(kvp.Value);
                    totalIncrements += kvp.Value;
                }
            }

            if (totalIncrements > 0)
                Debug.Log($"BoneBloom spread: applied {totalIncrements} infestation increments to {increments.Count} tiles.");
        }

        private void EndTurnConsume()
        {
            if (godBeast == null) return;
            // read per-turn resource from godBeast data
            var dataField = godBeast.GetType().GetField("data");
            GameData.GodBeastData data = null;
            if (dataField != null)
                data = dataField.GetValue(godBeast) as GameData.GodBeastData;
            if (data == null || data.perTurnResource == null)
            {
                Debug.LogWarning("TurnManager: godBeast.data.perTurnResource is not assigned. Configure a ResourceItem on the GodBeastData to consume each turn.");
                return;
            }

            // consume the configured resource and amount
            godBeast.ConsumeResource(data.perTurnResource, data.perTurnAmount);
            int remaining = godBeast.GetResourceAmount(data.perTurnResource);
            if (debugMenuUI != null)
                debugMenuUI.UpdateSap(remaining);
            if (remaining <= sapWarningThreshold && debugMenuUI != null)
                debugMenuUI.ShowSapWarning(remaining);
            if (remaining <= 0)
            {
                isGameOver = true;
                if (debugMenuUI != null)
                    debugMenuUI.ShowGameOver($"God-beast has died ({data.perTurnResource.displayName} = 0)");
            }
        }
    }
}
