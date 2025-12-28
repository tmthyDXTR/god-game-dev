using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using HexGrid;

namespace HexGrid
{
    /// <summary>
    /// Lightweight settlement metadata attached to a tile.
    /// Kept intentionally minimal so cards or managers can extend behavior.
    /// </summary>
    public class Settlement : MonoBehaviour
    {
        public string settlementId;
        public string settlementOwner = "Player";
        public int settlementLevel = 1;
        public bool isMobile = true;
        public bool hasSettlement => !string.IsNullOrEmpty(settlementId);

        // Settlement storage
        public Dictionary<Managers.ResourceManager.GameResource, int> storedResources = new Dictionary<Managers.ResourceManager.GameResource, int>();

        [Header("Storage Visualization")]
        [Tooltip("Sprite to show for stored Materials (logs)")]
        public Sprite storedLogSprite;
        [Tooltip("Sprite to show for stored Food")]
        public Sprite storedFoodSprite;
        [Tooltip("Starting Y offset for first row of storage icons")]
        public float storageYOffset = -0.3f;
        [Tooltip("Vertical spacing between rows")]
        public float storageRowSpacing = 0.4f;
        [Tooltip("Maximum icons per row before wrapping to next row")]
        public int maxIconsPerRow = 5;
        
        // Visual elements for stored resources
        private List<GameObject> storageVisuals = new List<GameObject>();
        // Layout configuration for storage icons
        private const float IconSpacing = 0.15f;
        private const float GroupSpacing = 0.1f; // Extra space between groups of 5
        private const int IconsPerGroup = 5;

        private void Reset()
        {
            settlementId = System.Guid.NewGuid().ToString();
        }

        private void Start()
        {
            // Get sprite references from the HexTile component if not already assigned
            if (storedLogSprite == null || storedFoodSprite == null)
            {
                var hexTile = GetComponent<HexTile>();
                if (hexTile != null)
                {
                    if (storedLogSprite == null)
                        storedLogSprite = hexTile.logIcon;
                    // Food sprite would need to be added to HexTile if needed
                }
            }
            
            UpdateStorageVisuals();
        }


        // Local job queue owned by this Settlement. Jobs should be enqueued
        // via `EnqueueJob` so validation and local rules can be applied.
        Queue<Job> jobQueue = new Queue<Job>();

        // Enqueue a job into this settlement's local queue.
        public void EnqueueJob(Job job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            job.targetSettlement = this;
            job.createdAt = DateTime.UtcNow;
            jobQueue.Enqueue(job);
            // TODO: Notify JobManager (if present) about new job
        }

        // Try to dequeue the next available job from this settlement.
        // Agents should call through JobManager.RequestJob(agent) in a hybrid design.
        public bool TryDequeueJob(out Job job)
        {
            if (jobQueue.Count > 0)
            {
                job = jobQueue.Dequeue();
                return true;
            }
            job = null;
            return false;
        }

        // Peek without removing.
        public Job PeekNextJob()
        {
            return jobQueue.Count > 0 ? jobQueue.Peek() : null;
        }

        public int QueuedJobCount => jobQueue.Count;

        /// <summary>
        /// Deposit a resource into this settlement's local storage. Returns amount accepted.
        /// </summary>
        public int DepositResource(Managers.ResourceManager.GameResource resource, int amount)
        {
            if (amount <= 0) return 0;
            if (storedResources.ContainsKey(resource))
                storedResources[resource] += amount;
            else
                storedResources[resource] = amount;
            // Optionally notify global ResourceManager as well
            if (Managers.ResourceManager.Instance != null)
                Managers.ResourceManager.Instance.AddResource(resource, amount);
            
            // Update visual display
            UpdateStorageVisuals();
            
            return amount;
        }

        /// <summary>
        /// Called when a job delivered resources to this settlement.
        /// Adds producedAmount to storage and logs. Additional bookkeeping (events, persistence)
        /// can be added here.
        /// </summary>
        public void OnJobComplete(Job job, int producedAmount)
        {
            if (job == null) return;
            if (producedAmount <= 0)
            {
                Debug.LogWarning($"Settlement {name}: OnJobComplete called with 0 or negative amount for job {job.id}");
                return;
            }
            DepositResource(job.resource, producedAmount);
            Debug.Log($"Settlement {name}: Job {job.id} completed, received {producedAmount} {job.resource}.");
            // TODO: fire events, update settlement stats, persist job completion
        }

        #region Storage Visuals
        /// <summary>
        /// Updates the visual display of stored resources around the settlement.
        /// Shows one icon per resource, grouped in sets of 5 for easy visual counting.
        /// </summary>
        void UpdateStorageVisuals()
        {
            // Clear existing visuals
            foreach (var visual in storageVisuals)
            {
                if (visual != null) Destroy(visual);
            }
            storageVisuals.Clear();

            int totalIconCount = 0;
            float worldY = transform.position.y;
            int baseSortOrder = 800 + Mathf.RoundToInt(-worldY * 100f); // Similar to dropped resources

            foreach (var kvp in storedResources)
            {
                if (kvp.Value <= 0) continue;

                Sprite spriteToUse = null;
                if (kvp.Key == Managers.ResourceManager.GameResource.Materials)
                    spriteToUse = storedLogSprite;
                else if (kvp.Key == Managers.ResourceManager.GameResource.Food)
                    spriteToUse = storedFoodSprite;

                if (spriteToUse == null) continue;

                // Show one icon per resource
                for (int i = 0; i < kvp.Value; i++)
                {
                    GameObject icon = new GameObject($"StoredResource_{kvp.Key}_{i}");
                    icon.transform.SetParent(transform);
                    
                    // Calculate row and position within row
                    int row = totalIconCount / maxIconsPerRow;
                    int positionInRow = totalIconCount % maxIconsPerRow;
                    
                    // Calculate position with grouping within the row
                    int groupIndex = positionInRow / IconsPerGroup;
                    int positionInGroup = positionInRow % IconsPerGroup;
                    
                    // Calculate x position: spacing within group + extra spacing between groups
                    float xOffset = positionInGroup * IconSpacing + groupIndex * GroupSpacing;
                    // Center the row (based on how many icons are in this row)
                    int iconsInThisRow = Mathf.Min(maxIconsPerRow, kvp.Value - row * maxIconsPerRow);
                    int groupsInRow = (iconsInThisRow - 1) / IconsPerGroup;
                    float rowWidth = (iconsInThisRow - 1) * IconSpacing + groupsInRow * GroupSpacing;
                    xOffset -= rowWidth / 2f;
                    
                    float yOffset = storageYOffset - (row * storageRowSpacing);
                    
                    icon.transform.localPosition = new Vector3(xOffset, yOffset, 0);
                    icon.transform.localScale = Vector3.one * 0.3f;

                    var sr = icon.AddComponent<SpriteRenderer>();
                    sr.sprite = spriteToUse;
                    sr.sortingOrder = baseSortOrder + totalIconCount;

                    storageVisuals.Add(icon);
                    totalIconCount++;
                }
            }
        }
        #endregion

    }
}