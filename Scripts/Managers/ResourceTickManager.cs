using System.Collections;
using System.Linq;
using UnityEngine;
using HexGrid;

namespace Managers
{
    /// <summary>
    /// Drives simple per-tick resource production and consumption.
    /// - Gathers Food from tiles proportional to tile.populationCount.
    /// - Deposits gathered food into ResourceManager.
    /// - Consumes global Food for population upkeep and triggers starvation (despawn agents) on shortfall.
    ///
    /// This is intentionally simple for an MVP; later agent-job systems can replace the gather logic.
    /// </summary>
    public class ResourceTickManager : MonoBehaviour
    {
        [Tooltip("Seconds between ticks")]
        public float tickInterval = 2f;

        [Tooltip("If true, ResourceTickManager will run automatically on a timer. If false, ticks must be triggered manually (e.g., by End Turn).")]
        public bool autoTick = true;

        [Tooltip("Food gathered per person per tick from their tile")]
        public int gatherRatePerPerson = 1;

        [Tooltip("Food consumed per person per tick (can be fractional; will be rounded up for integer storage)")]
        public float foodPerPersonPerTick = 0.5f;

        HexGridGenerator gridGenerator;

        private Coroutine loop;

        void Start()
        {
            if (!Application.isPlaying) return;
            gridGenerator = FindFirstObjectByType<HexGridGenerator>();
            if (autoTick)
                loop = StartCoroutine(TickLoop());
        }

        /// <summary>
        /// Trigger a single tick immediately (can be called from TurnManager.EndTurn or UI).
        /// </summary>
        public void TriggerTick()
        {
            Debug.Log("ResourceTickManager.TriggerTick called");
            // Run OnTick synchronously; safe because it's small and idempotent-ish.
            try {
                Debug.Log("ResourceTickManager: OnTick start");
                OnTick();
                Debug.Log("ResourceTickManager: OnTick end");
            } catch (System.Exception e) { Debug.LogException(e); }
        }

        /// <summary>
        /// Enable automatic ticking (starts coroutine if not running).
        /// </summary>
        public void EnableAutoTick()
        {
            if (loop == null && Application.isPlaying)
            {
                loop = StartCoroutine(TickLoop());
            }
            autoTick = true;
        }

        /// <summary>
        /// Disable automatic ticking (stops coroutine if running).
        /// </summary>
        public void DisableAutoTick()
        {
            if (loop != null)
            {
                StopCoroutine(loop);
                loop = null;
            }
            autoTick = false;
        }

        IEnumerator TickLoop()
        {
            while (Application.isPlaying)
            {
                yield return new WaitForSeconds(tickInterval);
                try { OnTick(); } catch (System.Exception e) { Debug.LogException(e); }
            }
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
            int totalPopulation = 0;
            foreach (var tile in gridGenerator.tiles.Values)
            {
                if (tile == null) continue;
                totalPopulation += Mathf.Max(0, tile.populationCount);
            }

            if (totalPopulation <= 0) return;

            float demandF = totalPopulation * foodPerPersonPerTick;
            int demand = Mathf.CeilToInt(demandF);
            int removed = ResourceManager.Instance.TryRemoveResource(ResourceManager.GameResource.Food, demand);
            if (removed >= demand)
            {
                // all good
                return;
            }

            int shortage = demand - removed;
            // approximate number of people that cannot be fed
            int starvationPeople = Mathf.CeilToInt(shortage / Mathf.Max(foodPerPersonPerTick, 0.0001f));
            Debug.LogWarning($"ResourceTick: Food shortage {shortage}. Starvation affecting ~{starvationPeople} people.");

            // Despawn agents to represent starvation (prefer any agents currently active)
            var pm = PopulationManager.Instance ?? FindFirstObjectByType<PopulationManager>();
            if (pm == null)
            {
                Debug.LogWarning("ResourceTick: No PopulationManager found to remove starving agents.");
                return;
            }

            var agents = FindObjectsByType<PopulationAgent>(FindObjectsSortMode.None);
            int removedCount = 0;
            // remove agents up to starvationPeople
            foreach (var a in agents)
            {
                if (removedCount >= starvationPeople) break;
                if (a == null) continue;
                pm.DespawnAgent(a);
                removedCount++;
            }
            Debug.LogWarning($"ResourceTick: Despawned {removedCount} agents due to starvation.");
        }

        void OnDisable()
        {
            if (loop != null) StopCoroutine(loop);
        }
    }
}
