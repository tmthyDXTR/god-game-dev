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
        int cap = baseCarryCapacity;
        foreach (var t in traits)
        {
            if (t == null) continue;
            cap += t.carryCapacityBonus;
        }
        return Mathf.Max(0, cap);
    }

    public float GetGatherMultiplier()
    {
        float mul = baseGatherMultiplier;
        foreach (var t in traits)
        {
            if (t == null) continue;
            mul *= t.gatherMultiplier;
        }
        return mul;
    }

    void Update()
    {
        if (stayOnTile && targetTile == null)
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
}
