using UnityEngine;

namespace Inventory
{
    [CreateAssetMenu(fileName = "ResourceItem", menuName = "Game/Resource Item", order = 0)]
    public class ResourceItem : ScriptableObject
    {
        public string resourceId = "resource"; // e.g. "sap", "food"
        public string displayName = "Resource";
        public Sprite icon;
    }
}
