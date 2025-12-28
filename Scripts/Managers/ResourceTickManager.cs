using System.Collections;
using System.Linq;
using UnityEngine;
using HexGrid;

namespace Managers
{
    /// <summary>
    /// Handles resource-specific tick logic (gathering, consumption, etc.).
    /// Subscribes to GlobalTickManager for tick events instead of running its own loop.
    /// </summary>
    public class ResourceTickManager : MonoBehaviour
    {
        // Work units granted to agents per tick. Agents consume work units to complete harvesting tasks.
        [Tooltip("Work units provided to agents per tick. Harvest progress is driven by these units.")]
        public float workUnitsPerTick = 1f;

        // Simple event fired after tick processing so agents and systems can react deterministically.
        public event System.Action OnTickEvent;

        [Tooltip("Food gathered per person per tick from their tile")]
        public int gatherRatePerPerson = 1;

        [Tooltip("Food consumed per person per tick (can be fractional; will be rounded up for integer storage)")]
        public float foodPerPersonPerTick = 0.5f;

        /// <summary>
        /// Convenience property to get tick interval from GlobalTickManager.
        /// </summary>
        public float tickInterval => GlobalTickManager.Instance != null ? GlobalTickManager.Instance.tickInterval : 2f;

        HexGridGenerator gridGenerator;

        void Start()
        {
            if (!Application.isPlaying) return;
            gridGenerator = FindFirstObjectByType<HexGridGenerator>();
            
            // Subscribe to GlobalTickManager
            if (GlobalTickManager.Instance != null)
            {
                GlobalTickManager.Instance.OnTick += HandleGlobalTick;
            }
            else
            {
                Debug.LogWarning("ResourceTickManager: GlobalTickManager not found! Ticks will not fire.");
            }
        }

        void OnDestroy()
        {
            if (GlobalTickManager.Instance != null)
            {
                GlobalTickManager.Instance.OnTick -= HandleGlobalTick;
            }
        }

        void HandleGlobalTick()
        {
            try
            {
                OnTick();
            }
            catch (System.Exception e) { Debug.LogException(e); }
            
            try
            {
                OnTickEvent?.Invoke();
            }
            catch (System.Exception e) { Debug.LogException(e); }
        }

        /// <summary>
        /// Trigger resource tick logic manually (still fires OnTickEvent for agents).
        /// </summary>
        public void TriggerTick()
        {
            Debug.Log("ResourceTickManager.TriggerTick called");
            HandleGlobalTick();
        }

        void OnTick()
        {
            if (ResourceManager.Instance == null) return;
            if (gridGenerator == null) gridGenerator = FindFirstObjectByType<HexGridGenerator>();
            if (gridGenerator == null || gridGenerator.tiles == null) return;

            // // 1) Gather from tiles into global resources
            // int totalGathered = 0;
            // foreach (var tile in gridGenerator.tiles.Values)
            // {
            //     if (tile == null) continue;
            //     int pop = Mathf.Max(0, tile.populationCount);
            //     if (pop <= 0) continue;
            //     int avail = tile.GetResourceAmount(Managers.ResourceManager.GameResource.Food);
            //     if (avail <= 0) continue;
            //     int want = pop * gatherRatePerPerson;
            //     int taken = Mathf.Min(avail, want);
            //     if (taken > 0)
            //     {
            //         tile.RemoveResource(Managers.ResourceManager.GameResource.Food, taken);
            //         ResourceManager.Instance.AddResource(ResourceManager.GameResource.Food, taken);
            //         totalGathered += taken;
            //     }
            // }
            // if (totalGathered > 0)
            //     Debug.Log($"ResourceTick: Gathered {totalGathered} food this tick.");

            // 2) Consumption: global food upkeep
            // int totalPopulation = 0;
            // foreach (var tile in gridGenerator.tiles.Values)
            // {
            //     if (tile == null) continue;
            //     totalPopulation += Mathf.Max(0, tile.populationCount);
            // }

            // if (totalPopulation <= 0) return;

            // float demandF = totalPopulation * foodPerPersonPerTick;
            // int demand = Mathf.CeilToInt(demandF);
            // int removed = ResourceManager.Instance.TryRemoveResource(ResourceManager.GameResource.Food, demand);
            // if (removed >= demand)
            // {
            //     // all good
            //     return;
            // }

            // int shortage = demand - removed;
            // // approximate number of people that cannot be fed
            // int starvationPeople = Mathf.CeilToInt(shortage / Mathf.Max(foodPerPersonPerTick, 0.0001f));
            // Debug.LogWarning($"ResourceTick: Food shortage {shortage}. Starvation affecting ~{starvationPeople} people.");

            // // Despawn agents to represent starvation (prefer any agents currently active)
            // var pm = PopulationManager.Instance ?? FindFirstObjectByType<PopulationManager>();
            // if (pm == null)
            // {
            //     Debug.LogWarning("ResourceTick: No PopulationManager found to remove starving agents.");
            //     return;
            // }

            // var agents = FindObjectsByType<PopulationAgent>(FindObjectsSortMode.None);
            // int removedCount = 0;
            // // remove agents up to starvationPeople
            // foreach (var a in agents)
            // {
            //     if (removedCount >= starvationPeople) break;
            //     if (a == null) continue;
            //     pm.DespawnAgent(a);
            //     removedCount++;
            // }
            // Debug.LogWarning($"ResourceTick: Despawned {removedCount} agents due to starvation.");
        }

        void OnDisable()
        {
            if (GlobalTickManager.Instance != null)
            {
                GlobalTickManager.Instance.OnTick -= HandleGlobalTick;
            }
        }
    }
}
