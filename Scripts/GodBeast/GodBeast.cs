using UnityEngine;
using HexGrid;
using GameData;

namespace GodBeast
{
    [RequireComponent(typeof(Collider2D))]
    public class GodBeast : MonoBehaviour, ISelectable
    {
        public Vector2Int gridPosition; // Hex grid position

        [Header("Data")]
        public GameData.GodBeastData data;

        private SpriteRenderer spriteRenderer;

        // runtime inventory (resource -> amount)
        private System.Collections.Generic.Dictionary<global::Inventory.ResourceItem, int> inventory = new System.Collections.Generic.Dictionary<global::Inventory.ResourceItem, int>();

        private void Awake()
        {
            // Initialize inventory from ScriptableObject data if provided (minimal, data-driven)
            if (data != null)
            {
                if (!string.IsNullOrEmpty(data.beastName))
                    gameObject.name = data.beastName;
                // populate runtime inventory
                foreach (var rs in data.startingResources)
                {
                    if (rs.item == null) continue;
                    var item = rs.item as global::Inventory.ResourceItem;
                    inventory[item] = rs.amount;
                }
            }

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            var sprite = Resources.Load<Sprite>("figures/tree-god");
            if (spriteRenderer != null && sprite != null)
                spriteRenderer.sprite = sprite;
            else if (sprite == null)
                Debug.LogError("GodBeast sprite not found at Resources/figures/tree-god.png");
        }

        // You can add more logic here for health, hunger, movement, etc.

        public Hex HexCoordinates
        {
            get { return new Hex(gridPosition.x, gridPosition.y); }
        }

        // ISelectable implementation
        public GameObject GameObject => gameObject;

        public void OnSelected()
        {
            if (spriteRenderer != null)
            {
                if (SelectionManager.Instance != null)
                    spriteRenderer.color = SelectionManager.Instance.selectColor;
                else
                    spriteRenderer.color = Color.yellow;
            }
        }

        public void OnDeselected()
        {
            if (spriteRenderer != null)
            {
                if (SelectionManager.Instance != null)
                    spriteRenderer.color = SelectionManager.Instance.normalColor;
                else
                    spriteRenderer.color = Color.white;
            }
        }


        // Generic inventory accessors
        /// <summary>
        /// Return the amount of the given <see cref="Inventory.ResourceItem"/> in the beast's runtime inventory.
        /// </summary>
        public int GetInventoryAmount(global::Inventory.ResourceItem item)
        {
            if (item == null) return 0;
            if (inventory.TryGetValue(item, out var v)) return v;
            return 0;
        }

        /// <summary>
        /// Modify the stored amount of <paramref name="item"/> by <paramref name="delta"/> (can be negative).
        /// Amounts are clamped to zero.
        /// </summary>
        public void ModifyResource(global::Inventory.ResourceItem item, int delta)
        {
            if (item == null) return;
            var cur = GetInventoryAmount(item);
            var next = cur + delta;
            if (next < 0) next = 0;
            inventory[item] = next;
            // Keep nothing else in sync here; systems should use ResourceItem-based APIs
        }

        /// <summary>
        /// Convenience: consume the given amount (reduces stored amount).
        /// </summary>
        public void ConsumeResource(global::Inventory.ResourceItem item, int amount)
        {
            ModifyResource(item, -amount);
        }
    }
}
