using System.Collections.Generic;
using UnityEngine;
using HexGrid;
using Prototype.Traits;

public class PopulationAgent : MonoBehaviour
{
    public float moveSpeed = .25f;
    public HexTile currentTile;
    HexTile targetTile;
    bool stayOnTile = false;

    public enum BaseStat {
        startingTraitsAmount,
        Health,
        Strength,
    }

    public enum AgentState {
        Idle,
        SeekingFood,
        Eating
    }
    
    [Header("Traits")]
    public List<TraitSO> traits = new List<TraitSO>();


    [Header("Base Stats")]
    // Concrete sampled stat values (set at Initialize)
    [Tooltip("Number of starting traits assigned to this agent after sampling the distribution")]
    public int startingTraitsAmount = 1;
    [Tooltip("Agent health value sampled from the health distribution")]
    public int health = 1;
    [Tooltip("Agent strength value sampled from the strength distribution")]
    public int strength = 1;

    [Tooltip("Base carry capacity used by GetCarryCapacity before trait modifiers")]
    public int baseCarryCapacity = 1;
    [Tooltip("Base gather multiplier used by GetGatherMultiplier before trait modifiers")]
    public float baseGatherMultiplier = 1f;
    
    // Cached/computed stats (calculated at Initialize and used by game logic)
    public int carryCapacity;
    public float gatherMultiplier;

    [Header("Hunger System")]
    [Tooltip("Current hunger level (0 = starving, maxHunger = full)")]
    public float currentHunger = 100f;
    [Tooltip("Maximum hunger capacity")]
    public float maxHunger = 100f;
    [Tooltip("Hunger decay per second (lower = slower hunger)")]
    public float hungerDecayRate = 1f;
    [Tooltip("Food restored per eating action")]
    public float foodRestorationAmount = 30f;
    [Tooltip("Current behavioral state of the agent")]
    public AgentState agentState = AgentState.Idle;

    [Header("Stat Distributions (probability charts)")]
    [Tooltip("Centralized stat chart ScriptableObject (use this; per-agent arrays removed).")]
    public Prototype.StatChartSO statChart;

    // local wandering
    Vector3 localTarget;
    public float idleRadius = 0.65f;
    public float idleTargetThreshold = 0.001f;
    float idleTimer = 0f;
    public float idlePickInterval = 2f;

    public void Initialize(HexTile start, bool _stayOnTile = false)
    {
        currentTile = start;
        targetTile = null;
        transform.position = currentTile != null ? currentTile.transform.position : Vector3.zero;
        stayOnTile = _stayOnTile;
        if (stayOnTile)
        {
            // place with a small jitter inside the tile so agents are visible
            transform.position = GetRandomPointInTile();
            PickNewLocalTarget();
        }
        // Set sprite renderer order in layer to be above tiles
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 20;
        }
        // Set transform scale
        // TODO: make this configurable depending on stats like strength ie?
        // transform.localScale = Vector3.one * 4f;

        // Sample base stats from configured probability charts
        // (if the charts are not provided, keep defaults)
        // Prefer charts from the centralized `statChart` asset if provided, otherwise fall back to per-agent arrays
        var startChart = statChart != null ? statChart.startingTraitsAmountChart : null;
        var startValues = statChart != null ? statChart.startingTraitsAmountValues : null;
        startingTraitsAmount = SampleStatFromChart(startChart, startValues, startingTraitsAmount);

        var hChart = statChart != null ? statChart.healthChart : null;
        var hValues = statChart != null ? statChart.healthValues : null;
        health = SampleStatFromChart(hChart, hValues, health);

        var sChart = statChart != null ? statChart.strengthChart : null;
        var sValues = statChart != null ? statChart.strengthValues : null;
        strength = SampleStatFromChart(sChart, sValues, strength);

        // Give starting random traits
        for (int i = 0; i < startingTraitsAmount; i++)
        {
            var trait = Managers.TraitManager.Instance != null ? Managers.TraitManager.Instance.GetRandomTrait() : null;
            AddTrait(trait);
        }

        // Compute derived stats once at initialization so game logic uses stable values
        ComputeDerivedStats();
        
        // Initialize hunger to max
        currentHunger = maxHunger;
        agentState = AgentState.Idle;
    }

    // Trait helpers
    public bool HasTrait(string traitId)
    {
        if (string.IsNullOrEmpty(traitId)) return false;
        foreach (var t in traits)
            if (t != null && t.traitId == traitId)
                return true;
        return false;
    }

    public void AddTrait(TraitSO trait)
    {
        if (trait == null) return;
        if (!traits.Contains(trait))
            traits.Add(trait);
    }

    public void RemoveTrait(TraitSO trait)
    {
        if (trait == null) return;
        if (traits.Contains(trait))
            traits.Remove(trait);
    }

    public int GetCarryCapacity()
    {
        return Mathf.Max(0, carryCapacity);
    }

    public float GetGatherMultiplier()
    {
        return gatherMultiplier;
    }

    // Recompute cached derived stats based on base stats and traits.
    void ComputeDerivedStats()
    {
        int cap = baseCarryCapacity;
        float mul = baseGatherMultiplier;
        if (traits != null)
        {
            foreach (var t in traits)
            {
                if (t == null) continue;
                cap += t.carryCapacityBonus;
                mul *= t.gatherMultiplier;
            }
        }
        carryCapacity = Mathf.Max(0, cap);
        gatherMultiplier = mul;
    }

    void Update()
    {
        // Tick hunger down over time
        currentHunger -= hungerDecayRate * Time.deltaTime;
        currentHunger = Mathf.Max(0f, currentHunger);

        // Check if starving and not already seeking food
        if (currentHunger <= 0f && agentState != AgentState.SeekingFood && agentState != AgentState.Eating)
        {
            // Find nearest food tile and go there
            HexTile foodTile = FindNearestFoodTile();
            if (foodTile != null)
            {
                agentState = AgentState.SeekingFood;
                stayOnTile = false;
                SetTarget(foodTile);
                Debug.Log($"{name}: Starving! Seeking food at {foodTile.HexCoordinates}");
            }
        }

        if (stayOnTile && targetTile == null && agentState == AgentState.Idle)
        {
            // idle wandering inside current tile
            idleTimer += Time.deltaTime;
            Vector3 targetPos = localTarget;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            if (Vector3.SqrMagnitude(transform.position - targetPos) < idleTargetThreshold || idleTimer >= idlePickInterval)
            {
                PickNewLocalTarget();
            }
            return;
        }

        if (targetTile == null) return;

        Vector3 worldTargetPos = targetTile.transform.position;
        transform.position = Vector3.MoveTowards(transform.position, worldTargetPos, moveSpeed * Time.deltaTime);
        if (Vector3.SqrMagnitude(transform.position - worldTargetPos) < 0.0001f)
        {
            ArriveAtTarget();
        }
    }

    void ArriveAtTarget()
    {
        if (currentTile != null) currentTile.OnPopulationLeave(this);
        currentTile = targetTile;
        if (currentTile != null) currentTile.OnPopulationEnter(this);
        targetTile = null;

        // If we were seeking food and arrived at a tile with food, eat exactly 1 food and return to idle
        if (agentState == AgentState.SeekingFood && currentTile != null)
        {
            // Immediately change state to prevent re-entry
            agentState = AgentState.Eating;
            
            int availableFood = currentTile.GetResourceAmount(Managers.ResourceManager.GameResource.Food);
            if (availableFood > 0)
            {
                // Consume exactly 1 food from tile and restore hunger
                currentTile.RemoveResource(Managers.ResourceManager.GameResource.Food, 1);
                currentHunger = Mathf.Min(currentHunger + foodRestorationAmount, maxHunger);
                Debug.Log($"{name}: Ate 1 food! Hunger restored to {currentHunger:F1}");
            }
            else
            {
                // No food here anymore
                Debug.Log($"{name}: No food at destination. Will search again when hungry.");
            }
            
            // Return to idle state and resume local wandering
            agentState = AgentState.Idle;
            stayOnTile = true;
            PickNewLocalTarget();
        }
    }

    public void SetTarget(HexTile t)
    {
        if (t == null) return;
        targetTile = t;
    }

    void PickNewLocalTarget()
    {
        localTarget = GetRandomPointInTile();
        idleTimer = 0f;
    }

    Vector3 GetRandomPointInTile()
    {
        if (currentTile == null) return transform.position;
        var center = currentTile.transform.position;
        var off = Random.insideUnitCircle * idleRadius;
        return new Vector3(center.x + off.x, center.y + off.y, center.z);
    }

    // Called to begin inter-tile movement
    public void StartMovement()
    {
        if (!stayOnTile) return;
        stayOnTile = false;
    }
    
    // Helper: sample an index from a probability chart and map to optional values array safely.
    // Returns fallbackValue if sampling/mapping cannot produce a valid result.
    int SampleStatFromChart(float[] chart, int[] valueMap, int fallbackValue)
    {
        int idx = RandomUtil.PickIndexFromChart(chart);
        if (valueMap != null && valueMap.Length > 0)
        {
            if (idx < 0) return fallbackValue;
            if (idx >= valueMap.Length)
            {
                // lengths mismatch; clamp to last available value and warn
                Debug.LogWarning($"PopulationAgent: valueMap length ({valueMap.Length}) smaller than sampled index ({idx}). Clamping to last value.");
                return valueMap[valueMap.Length - 1];
            }
            return valueMap[idx];
        }
        // No mapping provided; use the sampled index as the stat (or fallback if negative)
        return Mathf.Max(fallbackValue, idx);
    }

    // Find nearest tile with food resources
    HexTile FindNearestFoodTile()
    {
        if (currentTile == null) return null;

        var gen = FindFirstObjectByType<HexGridGenerator>();
        if (gen == null || gen.tiles == null) return null;

        HexTile nearest = null;
        float minDist = float.MaxValue;

        foreach (var tile in gen.tiles.Values)
        {
            if (tile == null) continue;
            int food = tile.GetResourceAmount(Managers.ResourceManager.GameResource.Food);
            if (food > 0)
            {
                float dist = Vector3.Distance(transform.position, tile.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = tile;
                }
            }
        }

        return nearest;
    }
}
