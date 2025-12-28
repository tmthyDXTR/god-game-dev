using System.Collections.Generic;
using UnityEngine;

namespace HexGrid
{
    // ResourceType removed; use Managers.ResourceManager.GameResource throughout.

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
        public Sprite materialsIcon;
        [Tooltip("Sprite for dropped logs on the ground")]
        public Sprite logIcon;
        private List<GameObject> foodIcons = new List<GameObject>();
        private List<GameObject> materialsIcons = new List<GameObject>();
        private List<GameObject> droppedIcons = new List<GameObject>();
        // Overlay object used to show infestation using the bone sprite scaled by level
        private GameObject infestationOverlay = null;

        // Resource amounts for each type (use global GameResource enum)
        public System.Collections.Generic.Dictionary<Managers.ResourceManager.GameResource, int> resourceAmounts = new System.Collections.Generic.Dictionary<Managers.ResourceManager.GameResource, int>();
        
        // Dropped resources waiting to be picked up (harvested but not yet carried)
        public System.Collections.Generic.Dictionary<Managers.ResourceManager.GameResource, int> droppedResources = new System.Collections.Generic.Dictionary<Managers.ResourceManager.GameResource, int>();

        // Reserved resources (claimed by agents heading to this tile)
        public System.Collections.Generic.Dictionary<Managers.ResourceManager.GameResource, int> reservedResources = new System.Collections.Generic.Dictionary<Managers.ResourceManager.GameResource, int>();
        public System.Collections.Generic.Dictionary<Managers.ResourceManager.GameResource, int> reservedDropped = new System.Collections.Generic.Dictionary<Managers.ResourceManager.GameResource, int>();

        // Infestation level for BoneBloom: 0 = clean, 1 = sporeling, 2 = thicket, 3 = graveyard (becomes Bone tile)
        [Tooltip("0 = clean, 1 = sporeling, 2 = thicket, 3 = graveyard (converts to Bone)")]
        public int InfestationLevel = 0;

        // Add resource to tile
        public void AddResource(Managers.ResourceManager.GameResource type, int amount)
        {
            if (resourceAmounts.ContainsKey(type))
                resourceAmounts[type] += amount;
            else
                resourceAmounts[type] = amount;
            UpdateResourceIcons();
        }
        public int GetResourceAmount(Managers.ResourceManager.GameResource type)
        {
            if (resourceAmounts.TryGetValue(type, out int value))
                return value;
            return 0;
        }

        // Remove resource from tile
        public void RemoveResource(Managers.ResourceManager.GameResource type, int amount)
        {
            if (resourceAmounts.ContainsKey(type))
            {
                resourceAmounts[type] -= amount;
                if (resourceAmounts[type] <= 0)
                    resourceAmounts.Remove(type);
                UpdateResourceIcons();
            }
        }

        /// <summary>
        /// Attempt to harvest up to <paramref name="want"/> units of <paramref name="type"/> from this tile.
        /// Removes the amount actually taken from the tile and returns that amount.
        /// </summary>
        public int Harvest(Managers.ResourceManager.GameResource type, int want)
        {
            if (want <= 0) return 0;
            int have = GetResourceAmount(type);
            int take = Mathf.Min(want, have);
            if (take > 0)
            {
                // Reuse existing removal logic to update icons and amounts
                RemoveResource(type, take);
            }
            return take;
        }

        // ========== Dropped Resources (items on the ground waiting to be picked up) ==========
        
        /// <summary>
        /// Drop a resource on this tile (e.g., logs after harvesting a tree).
        /// </summary>
        public void DropResource(Managers.ResourceManager.GameResource type, int amount)
        {
            if (amount <= 0) return;
            if (droppedResources.ContainsKey(type))
                droppedResources[type] += amount;
            else
                droppedResources[type] = amount;
            UpdateResourceIcons();
        }

        /// <summary>
        /// Get how many dropped resources of a type are on this tile.
        /// </summary>
        public int GetDroppedAmount(Managers.ResourceManager.GameResource type)
        {
            if (droppedResources.TryGetValue(type, out int value))
                return value;
            return 0;
        }

        // ========== Resource Reservations ==========
        
        /// <summary>
        /// Get how many resources are reserved (claimed by agents heading here).
        /// </summary>
        public int GetReservedAmount(Managers.ResourceManager.GameResource type)
        {
            if (reservedResources.TryGetValue(type, out int value))
                return value;
            return 0;
        }

        /// <summary>
        /// Get how many dropped resources are reserved.
        /// </summary>
        public int GetReservedDroppedAmount(Managers.ResourceManager.GameResource type)
        {
            if (reservedDropped.TryGetValue(type, out int value))
                return value;
            return 0;
        }

        /// <summary>
        /// Get available (unreserved) resource amount.
        /// </summary>
        public int GetAvailableResourceAmount(Managers.ResourceManager.GameResource type)
        {
            return Mathf.Max(0, GetResourceAmount(type) - GetReservedAmount(type));
        }

        /// <summary>
        /// Get available (unreserved) dropped resource amount.
        /// </summary>
        public int GetAvailableDroppedAmount(Managers.ResourceManager.GameResource type)
        {
            return Mathf.Max(0, GetDroppedAmount(type) - GetReservedDroppedAmount(type));
        }

        /// <summary>
        /// Reserve resources for an agent heading to this tile. Returns true if reservation succeeded.
        /// </summary>
        public bool ReserveResource(Managers.ResourceManager.GameResource type, int amount)
        {
            if (amount <= 0) return false;
            int available = GetAvailableResourceAmount(type);
            if (available < amount) return false;

            if (reservedResources.ContainsKey(type))
                reservedResources[type] += amount;
            else
                reservedResources[type] = amount;
            return true;
        }

        /// <summary>
        /// Release a reservation (when job completes, cancels, or agent dies).
        /// </summary>
        public void ReleaseReservation(Managers.ResourceManager.GameResource type, int amount)
        {
            if (amount <= 0) return;
            if (reservedResources.ContainsKey(type))
            {
                reservedResources[type] -= amount;
                if (reservedResources[type] <= 0)
                    reservedResources.Remove(type);
            }
        }

        /// <summary>
        /// Reserve dropped resources for an agent heading to pick them up.
        /// </summary>
        public bool ReserveDroppedResource(Managers.ResourceManager.GameResource type, int amount)
        {
            if (amount <= 0) return false;
            int available = GetAvailableDroppedAmount(type);
            if (available < amount) return false;

            if (reservedDropped.ContainsKey(type))
                reservedDropped[type] += amount;
            else
                reservedDropped[type] = amount;
            return true;
        }

        /// <summary>
        /// Release a dropped resource reservation.
        /// </summary>
        public void ReleaseDroppedReservation(Managers.ResourceManager.GameResource type, int amount)
        {
            if (amount <= 0) return;
            if (reservedDropped.ContainsKey(type))
            {
                reservedDropped[type] -= amount;
                if (reservedDropped[type] <= 0)
                    reservedDropped.Remove(type);
            }
        }

        /// <summary>
        /// Pick up dropped resources from the ground. Returns amount actually picked up.
        /// </summary>
        public int PickupDropped(Managers.ResourceManager.GameResource type, int want)
        {
            if (want <= 0) return 0;
            int have = GetDroppedAmount(type);
            int take = Mathf.Min(want, have);
            if (take > 0)
            {
                droppedResources[type] -= take;
                if (droppedResources[type] <= 0)
                    droppedResources.Remove(type);
                UpdateResourceIcons();
            }
            return take;
        }

        /// <summary>
        /// Check if any dropped resources exist on this tile.
        /// </summary>
        public bool HasDroppedResources()
        {
            foreach (var kvp in droppedResources)
                if (kvp.Value > 0) return true;
            return false;
        }

        public void RemoveAllResources()
        {
            resourceAmounts.Clear();
            droppedResources.Clear();
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
        // Non-destructive highlight renderer (created as a child so original sprite/color aren't overwritten)
        private GameObject highlightObject;
        private SpriteRenderer highlightRenderer;
        private const float defaultHighlightScale = 1.06f;
        private const int highlightOrderOffset = 2;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            UpdateVisual();
        }

        // Ensure the highlight child object exists.
        private void EnsureHighlight()
        {
            if (highlightObject != null && highlightRenderer != null)
                return;
            highlightObject = new GameObject("Highlight");
            highlightObject.transform.SetParent(transform);
            highlightObject.transform.localRotation = Quaternion.identity;
            highlightObject.transform.localPosition = new Vector3(0f, 0f, -0.04f);
            highlightObject.transform.localScale = new Vector3(defaultHighlightScale, defaultHighlightScale, 1f);
            highlightRenderer = highlightObject.AddComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                highlightRenderer.sortingOrder = spriteRenderer.sortingOrder + highlightOrderOffset;
            highlightRenderer.color = new Color(1f, 1f, 1f, 0f);
            highlightRenderer.enabled = false;
        }

        // Set a visible highlight using a colored, slightly larger copy of the tile sprite. Passing a null color clears the highlight.
        public void SetHighlight(Color? color, float scaleMultiplier = defaultHighlightScale)
        {
            // Don't highlight unexplored tiles
            if (!isExplored)
            {
                ClearHighlight();
                return;
            }
            EnsureHighlight();
            if (spriteRenderer != null && highlightRenderer != null)
            {
                highlightRenderer.sprite = spriteRenderer.sprite;
                highlightObject.transform.localScale = new Vector3(scaleMultiplier, scaleMultiplier, 1f);
                if (color.HasValue)
                {
                    highlightRenderer.color = color.Value;
                    highlightRenderer.enabled = true;
                }
                else
                {
                    highlightRenderer.enabled = false;
                }
            }
        }

        public void ClearHighlight()
        {
            if (highlightRenderer != null)
                highlightRenderer.enabled = false;
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
            // Fully hide unexplored tiles: disable the base sprite renderer so nothing shows.
            // When explored, enable renderer and show normally.
            if (!isExplored)
            {
                if (spriteRenderer != null) spriteRenderer.enabled = false;
            }
            else
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = true;
                    spriteRenderer.color = Color.white;
                    // All tiles use fixed sorting order so they're on the same layer
                    spriteRenderer.sortingOrder = 100;
                }
            }
            // Keep highlight sprite in sync with base sprite
            if (highlightRenderer != null)
            {
                highlightRenderer.sprite = spriteRenderer.sprite;
                if (spriteRenderer != null)
                    highlightRenderer.sortingOrder = spriteRenderer.sortingOrder + highlightOrderOffset;
            }
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

            // Don't show infestation overlay on unexplored tiles
            if (!isExplored) return;

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

        // Visualize food and materials amount with icons
        public void UpdateResourceIcons()
        {
            // Remove old icons
            foreach (var icon in foodIcons)
                if (icon != null) DestroyImmediate(icon);
            foodIcons.Clear();
            foreach (var icon in materialsIcons)
                if (icon != null) DestroyImmediate(icon);
            materialsIcons.Clear();
            foreach (var icon in droppedIcons)
                if (icon != null) DestroyImmediate(icon);
            droppedIcons.Clear();

            // Don't display resource icons for unexplored tiles
            if (!isExplored) return;

            int foodAmount = GetResourceAmount(Managers.ResourceManager.GameResource.Food);
            int materialsAmount = GetResourceAmount(Managers.ResourceManager.GameResource.Materials);
            int droppedLogsAmount = GetDroppedAmount(Managers.ResourceManager.GameResource.Materials);
            
            // Food icons (top row)
            if (foodIcon != null && foodAmount > 0)
            {
                int baseOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
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
                    sr.sortingOrder = baseOrder + 10;
                    foodIcons.Add(go);
                }
            }
            // Materials icons (trees scattered naturally on tile)
            if (materialsIcon != null && materialsAmount > 0)
            {
                // Predefined positions for up to 5 trees to look natural on the hexagonal tile
                Vector2[] treePositions = new Vector2[]
                {
                    new Vector2(0f, 0f),        // Center
                    new Vector2(-1.2f, 0.6f),   // Top-left
                    new Vector2(1.2f, 0.6f),    // Top-right
                    new Vector2(-1.4f, -0.2f),  // Bottom-left
                    new Vector2(1.4f, -0.2f)    // Bottom-right
                };
                
                int treesToShow = Mathf.Min(materialsAmount, treePositions.Length);
                for (int i = 0; i < treesToShow; i++)
                {
                    var go = new GameObject("TreeIcon");
                    go.transform.SetParent(transform);
                    go.transform.localPosition = new Vector3(treePositions[i].x, treePositions[i].y, -0.08f);
                    go.transform.localScale = Vector3.one * 3f;
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = materialsIcon;
                    // Calculate sorting order based on tree's actual world Y position
                    // Start at 1000 + Y-based offset so trees are always above tiles (which are at 100)
                    float worldY = transform.position.y + treePositions[i].y;
                    sr.sortingOrder = 1000 + Mathf.RoundToInt(-worldY * 100f);
                    materialsIcons.Add(go);
                }
            }
            
            // Dropped logs icons (scattered on ground)
            if (logIcon != null && droppedLogsAmount > 0)
            {
                // Positions for dropped logs (different from trees, closer to ground)
                Vector2[] logPositions = new Vector2[]
                {
                    new Vector2(-0.3f, -0.5f),
                    new Vector2(0.3f, -0.6f),
                    new Vector2(0f, -0.3f),
                    new Vector2(-0.5f, -0.2f),
                    new Vector2(0.5f, -0.4f)
                };
                
                int logsToShow = Mathf.Min(droppedLogsAmount, logPositions.Length);
                for (int i = 0; i < logsToShow; i++)
                {
                    var go = new GameObject("LogIcon");
                    go.transform.SetParent(transform);
                    go.transform.localPosition = new Vector3(logPositions[i].x, logPositions[i].y, -0.09f);
                    go.transform.localScale = Vector3.one * 1.5f;
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = logIcon;
                    // Logs are on the ground, below trees but above tiles
                    float worldY = transform.position.y + logPositions[i].y;
                    sr.sortingOrder = 800 + Mathf.RoundToInt(-worldY * 100f);
                    droppedIcons.Add(go);
                }
            }
        }
    }
}
