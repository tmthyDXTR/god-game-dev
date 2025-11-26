using System.Collections.Generic;
using UnityEngine;
using Inventory;

namespace GameData
{
    [System.Serializable]
    public class ResourceStack
    {
        public ResourceItem item; // The resource item that god uses like sap for health i.e.
        public int amount = 0;
    }

    [CreateAssetMenu(fileName = "GodBeastData", menuName = "Game/GodBeast Data", order = 1)]
    public class GodBeastData : ScriptableObject
    {
        public string beastName = "GodBeast";
        public List<ResourceStack> startingResources = new List<ResourceStack>();
        [Header("Per-turn consumption")]
        public ResourceItem perTurnResource;
        public int perTurnAmount = 1;
    }
}
