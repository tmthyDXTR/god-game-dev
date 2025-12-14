using UnityEngine;
using Prototype.Traits;

namespace Managers
{
    /// <summary>
    /// TraitManager is a singleton that manages all trait-related operations.
    /// </summary>

    public class TraitManager : MonoBehaviour
    {
        public static TraitManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public TraitSO GetRandomTrait()
        {
            // Placeholder implementation for getting a random trait.
            // In a real implementation, this would pull from a database or list of available traits.
            // TODO: Implement trait database
            var allTraits = Resources.LoadAll<TraitSO>("Traits");
            if (allTraits.Length == 0) return null;
            int index = UnityEngine.Random.Range(0, allTraits.Length);
            return allTraits[index];
        }
    }
}