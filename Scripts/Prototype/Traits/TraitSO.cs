using UnityEngine;

namespace Prototype.Traits
{
    [CreateAssetMenu(menuName = "Population/Trait", fileName = "NewTrait")]
    public class TraitSO : ScriptableObject
    {
        [Tooltip("Unique id for quick checks (e.g. 'hardy', 'forager')")]
        public string traitId;
        public string displayName;
        public string description;
        public Sprite icon;

        [Header("Stat Modifiers")]
        [Tooltip("Multiplier applied to gathering (1.0 = no change)")]
        public float gatherMultiplier = 1f;
        [Tooltip("Additive bonus to carry capacity (can be negative)")]
        public int carryCapacityBonus = 0;
        [Tooltip("Multiplier applied to movement speed on agent (1.0 = no change)")]
        public float moveSpeedMultiplier = 1f;

        // Future: hook points for behaviour callbacks (events) can be added here.
    }
}
