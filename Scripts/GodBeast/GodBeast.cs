using UnityEngine;
using HexGrid;

namespace GodBeast
{
    [RequireComponent(typeof(Collider2D))]
    public class GodBeast : MonoBehaviour, ISelectable
    {
        public Vector2Int gridPosition; // Hex grid position

        private SpriteRenderer spriteRenderer;

        public int sap = 3;

        private void Awake()
        {
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

        public void ConsumeSap(int amount)
        {
            sap -= amount;
            if (sap < 0) sap = 0;
        }
    }
}
