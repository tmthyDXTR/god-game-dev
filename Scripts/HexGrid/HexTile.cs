using System.Collections.Generic;
using UnityEngine;

namespace HexGrid
{
    public enum ResourceType { None, Sap, Food }

    public class HexTile : MonoBehaviour
    {
        // Simple population tracking for prototype (number of agents currently on this tile)
        [Tooltip("Number of population agents currently on this tile (managed by PopulationManager)")]
        public int populationCount = 0;

        // Called by the population system when an agent arrives
        public void OnPopulationEnter(object agent)
        {
            populationCount++;
        }

        // Called by the population system when an agent leaves
        public void OnPopulationLeave(object agent)
        {
            populationCount = Mathf.Max(0, populationCount - 1);
        }

        [Header("Resource Icons")]
        public Sprite foodIcon;
        public Sprite sapIcon;
        private List<GameObject> foodIcons = new List<GameObject>();
        private List<GameObject> sapIcons = new List<GameObject>();
        // Overlay object used to show infestation using the bone sprite scaled by level
        private GameObject infestationOverlay = null;

        // Resource amounts for each type
        public Dictionary<ResourceType, int> resourceAmounts = new Dictionary<ResourceType, int>();

        // Infestation level for BoneBloom: 0 = clean, 1 = sporeling, 2 = thicket, 3 = graveyard (becomes Bone tile)
        [Tooltip("0 = clean, 1 = sporeling, 2 = thicket, 3 = graveyard (converts to Bone)")]
        public int InfestationLevel = 0;

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
            // Dim unexplored tiles; otherwise use default white color.
            if (!isExplored)
                spriteRenderer.color = new Color(0.2f, 0.2f, 0.2f, 1f); // dark
            else
                spriteRenderer.color = Color.white;
            // Update or create infestation overlay (uses bone sprite)
            UpdateInfestationOverlay();
            // Update food icons after visual change
            UpdateResourceIcons();
        }

        // Increase infestation level by amount (default 1). If level reaches 3, convert tile to Bone type.
        public void IncreaseInfestation(int amount = 1)
        {
            if (TileType == HexTileType.Bone)
                return; // already bone
            InfestationLevel = Mathf.Clamp(InfestationLevel + amount, 0, 3);
            if (InfestationLevel >= 3)
            {
                TileType = HexTileType.Bone;
                InfestationLevel = 3;
            }
            UpdateVisual();
        }

        // Create/update a centered overlay using the bone sprite scaled by infestation level
        private void UpdateInfestationOverlay()
        {
            // Remove existing overlay if any
            if (infestationOverlay != null)
            {
                if (Application.isPlaying)
                    Destroy(infestationOverlay);
                else
                    DestroyImmediate(infestationOverlay);
                infestationOverlay = null;
            }

            if (InfestationLevel <= 0)
                return;

            if (boneSprite == null)
                return; // nothing to show

            // Create overlay object
            infestationOverlay = new GameObject("InfestationOverlay");
            infestationOverlay.transform.SetParent(transform);
            infestationOverlay.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            infestationOverlay.transform.localRotation = Quaternion.identity;
            infestationOverlay.transform.localScale = Vector3.one;
            var sr = infestationOverlay.AddComponent<SpriteRenderer>();
            sr.sprite = boneSprite;
            // Ensure overlay draws above the tile but below resource icons
            int baseOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
            sr.sortingOrder = baseOrder + 5;

            // Scale overlay by infestation level (level 1 small -> level 3 large)
            float scale = 1f;
            switch (InfestationLevel)
            {
                case 1: scale = 0.33f; break;
                case 2: scale = 0.66f; break;
                case 3: scale = 1f; break;
            }
            infestationOverlay.transform.localScale = new Vector3(scale, scale, 1f);
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
