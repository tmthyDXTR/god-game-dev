using UnityEngine;

namespace Managers
{
    [CreateAssetMenu(menuName = "Managers/ResourcePreset", fileName = "ResourcePreset_")]
    public class ResourcePreset : ScriptableObject
    {
        [Header("Starting resources (apply as absolute values)")]
        public int food = 0;
        public int materials = 0;
        public int faith = 0;
    }
}
