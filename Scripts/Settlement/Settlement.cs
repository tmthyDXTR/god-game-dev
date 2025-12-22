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

        private void Reset()
        {
            settlementId = System.Guid.NewGuid().ToString();
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

    }
}