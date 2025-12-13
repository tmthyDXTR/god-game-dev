using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Singleton class that manages gathered resources and their distribution.
    /// </summary>
    
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [Header("Startup Preset")]
        [Tooltip("Optional preset to apply to this ResourceManager. Useful for setting starting resources before entering Play mode.")]
        public ResourcePreset defaultPreset;

        [Tooltip("If true, the Default Preset will be applied when this component awakens in the Editor (edit-mode). Useful to configure resources before Play.")]
        public bool applyPresetInEditMode = false;

        [Tooltip("If true, the Default Preset will be applied when entering Play mode on Awake.")]
        public bool applyPresetOnPlayStart = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            // Apply default preset if configured. Supports applying in edit-mode (before Play)
            if (defaultPreset != null)
            {
                if (Application.isPlaying)
                {
                    if (applyPresetOnPlayStart)
                        ApplyPreset(defaultPreset);
                }
                else
                {
                    if (applyPresetInEditMode)
                    {
                        ApplyPreset(defaultPreset);
#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(this);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(this.gameObject.scene);
#endif
                    }
                }
            }
        }

        // Minimal global resource enum for the prototype
        public enum GameResource { Food = 0, Materials = 1, Faith = 2 }

        // underlying storage
        private System.Collections.Generic.Dictionary<GameResource, int> resources = new System.Collections.Generic.Dictionary<GameResource, int>();

        // Event fired when a resource amount changes. Pass the resource key.
        public event System.Action<GameResource> OnResourceChanged;

        // Get current amount (0 if not present)
        public int GetAmount(GameResource res)
        {
            if (resources.TryGetValue(res, out var v)) return v;
            return 0;
        }

        // Add amount (amount must be > 0)
        public void AddResource(GameResource res, int amount)
        {
            if (amount <= 0) return;
            if (!resources.ContainsKey(res)) resources[res] = 0;
            resources[res] += amount;
            OnResourceChanged?.Invoke(res);
        }

        // Try to remove up to `amount` and return how many were removed (0..amount).
        // This is intentionally permissive so callers can handle partial fulfillment.
        public int TryRemoveResource(GameResource res, int amount)
        {
            if (amount <= 0) return 0;
            int have = GetAmount(res);
            int removed = Mathf.Min(have, amount);
            if (removed > 0)
            {
                resources[res] = have - removed;
                OnResourceChanged?.Invoke(res);
            }
            return removed;
        }

        // Convenience: attempt to remove and return whether full amount was satisfied
        public bool TryConsume(GameResource res, int amount)
        {
            int removed = TryRemoveResource(res, amount);
            return removed >= amount;
        }

        // Helper to set a resource amount (useful for debug/setup)
        public void SetResource(GameResource res, int amount)
        {
            if (amount < 0) amount = 0;
            resources[res] = amount;
            OnResourceChanged?.Invoke(res);
        }

        // Apply a preset (overwrite resource amounts with the preset values)
        public void ApplyPreset(ResourcePreset preset)
        {
            if (preset == null) return;
            SetResource(GameResource.Food, preset.food);
            SetResource(GameResource.Materials, preset.materials);
            SetResource(GameResource.Faith, preset.faith);
        }
    }
}