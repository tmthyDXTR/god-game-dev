using System.Collections.Generic;
using UnityEngine;

namespace HexGrid
{
    public enum ResourceType { None, Sap, Food }

    public class HexTile : MonoBehaviour
    {
        [Header("Resource Icons")]
        public Sprite foodIcon;
        public Sprite sapIcon;
        private List<GameObject> foodIcons = new List<GameObject>();
        private List<GameObject> sapIcons = new List<GameObject>();

        // Resource amounts for each type
        public Dictionary<ResourceType, int> resourceAmounts = new Dictionary<ResourceType, int>();

        // Add resource to tile
        public void AddResource(ResourceType type, int amount)
        {
            if (resourceAmounts.ContainsKey(type))
                resourceAmounts[type] += amount;
            else
                resourceAmounts[type] = amount;
            UpdateResourceIcons();
        }
        public int GetResourceAmount(ResourceType type)
        {
            if (resourceAmounts.TryGetValue(type, out int value))
                return value;
            return 0;
        }

        // Remove resource from tile
        public void RemoveResource(ResourceType type, int amount)
        {
            if (resourceAmounts.ContainsKey(type))
            {
                resourceAmounts[type] -= amount;
                if (resourceAmounts[type] <= 0)
                    resourceAmounts.Remove(type);
                UpdateResourceIcons();
            }
        }
        public void RemoveAllResources()
        {
            resourceAmounts.Clear();
            UpdateResourceIcons();
        }
        private void OnValidate()
        {
            UpdateVisual();
            UpdateResourceIcons();
        }

        public HexTileType TileType = HexTileType.Grass;
        [SerializeField]
        public Hex HexCoordinates;
        public bool isExplored = true;

        [Header("Tile Type Sprites")]
        public Sprite grassSprite;
        public Sprite forestSprite;
        public Sprite stoneSprite;
        public Sprite boneSprite;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            UpdateVisual();
        }

        public void SetTileType(HexTileType type)
        {
            TileType = type;
            UpdateVisual();
        }

        public void UpdateVisual()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            switch (TileType)
            {
                case HexTileType.Grass:
                    spriteRenderer.sprite = grassSprite;
                    break;
                case HexTileType.Forest:
                    spriteRenderer.sprite = forestSprite;
                    break;
                case HexTileType.Stone:
                    spriteRenderer.sprite = stoneSprite;
                    break;
                case HexTileType.Bone:
                    spriteRenderer.sprite = boneSprite;
                    break;
                default:
                    spriteRenderer.sprite = grassSprite;
                    break;
            }
            // Dim unexplored tiles
            if (!isExplored)
                spriteRenderer.color = new Color(0.2f, 0.2f, 0.2f, 1f); // dark
            else
                spriteRenderer.color = Color.white;
            // Update food icons after visual change
            UpdateResourceIcons();
        }

        // Visualize food and sap amount with icons
        public void UpdateResourceIcons()
        {
            // Remove old icons
            foreach (var icon in foodIcons)
                if (icon != null) DestroyImmediate(icon);
            foodIcons.Clear();
            foreach (var icon in sapIcons)
                if (icon != null) DestroyImmediate(icon);
            sapIcons.Clear();

            int foodAmount = GetResourceAmount(ResourceType.Food);
            int sapAmount = GetResourceAmount(ResourceType.Sap);

            // Food icons (top row)
            if (foodIcon != null && foodAmount > 0)
            {
                float spacing = 0.4f;
                float startX = -((foodAmount - 1) * spacing) / 2f;
                float y = 0.4f; // top row
                for (int i = 0; i < foodAmount; i++)
                {
                    var go = new GameObject("FoodIcon");
                    go.transform.SetParent(transform);
                    go.transform.localPosition = new Vector3(startX + i * spacing, y, -0.1f);
                    go.transform.localScale = Vector3.one;
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = foodIcon;
                    sr.sortingOrder = 10;
                    foodIcons.Add(go);
                }
            }
            // Sap icons (row below food)
            if (sapIcon != null && sapAmount > 0)
            {
                float spacing = 0.4f;
                float startX = -((sapAmount - 1) * spacing) / 2f;
                float y = (foodAmount > 0) ? -1f : 0.4f; // below food if food exists, else top row
                for (int i = 0; i < sapAmount; i++)
                {
                    var go = new GameObject("SapIcon");
                    go.transform.SetParent(transform);
                    go.transform.localPosition = new Vector3(startX + i * spacing, y, -0.1f);
                    go.transform.localScale = Vector3.one;
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sapIcon;
                    sr.sortingOrder = 11;
                    sapIcons.Add(go);
                }
            }
        }
    }
}
