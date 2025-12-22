using UnityEngine;
using System;

namespace HexGrid
{
    // Lightweight job record intended for use by Settlements and a central JobManager.
    public enum JobType { Gather, Haul, Deposit, Build, Custom }

    public class Job
    {
        public string id;
        public JobType type;
        public Managers.ResourceManager.GameResource resource;
        public int amount = 1;
        public HexTile originTile;
        public Settlement targetSettlement;
        public int priority = 0; // higher = more urgent
        public DateTime createdAt;

        // Claiming fields (set when an agent takes the job)
        public bool isClaimed = false;
        public string claimedByAgentId = null;

        public Job()
        {
            id = Guid.NewGuid().ToString();
            createdAt = DateTime.UtcNow;
        }
    }
}