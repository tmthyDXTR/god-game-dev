using System.Collections.Generic;
using UnityEngine;

namespace HexGrid
{
    public class SelectionManager : MonoBehaviour
    {
        public static SelectionManager Instance { get; private set; }

        [Header("Selection Colors")]
        public Color hoverColor = new Color(1f, 1f, 0.5f, 0.5f);
        public Color selectColor = new Color(1f, 1f, 0f, 0.7f);
        public Color normalColor = Color.white;

        [SerializeField]
        public HexTile SelectedTile { get; private set; }
        public ISelectable SelectedUnit { get; private set; }

        public HexGridGenerator gridGenerator;

        [Header("Selection Layers")]
        [Tooltip("Layer mask used to detect selectable units (units layer)")]
        public LayerMask unitsLayerMask;

        [Header("Tiles in Range")]
        [SerializeField]
        public List<HexTile> tilesInRange = new List<HexTile>();

        private HexTile hoveredTile;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // On game start, explore tiles around the first god-beast so the nearby area is revealed
            var god = FindFirstObjectByType<GodBeast.GodBeast>();
            if (god != null && gridGenerator != null)
            {
                // Temporarily set SelectedUnit to compute tiles in range without triggering selection visuals
                SelectedUnit = god;
                UpdateTilesInRange();
                // Clear the temporary selection
                SelectedUnit = null;
            }
        }

        void Update()
        {
            HandleHover();
            HandleSelect();
        }

        public void SetSelectedTile(HexTile tile)
        {
            // Clear previous selection highlight
            if (SelectedTile != null && SelectedTile != tile)
                SetTileHighlight(SelectedTile, normalColor);

            SelectedTile = tile;

            if (tile == null)
            {
                Debug.Log("Deselected tile.");
                return;
            }

            SetTileHighlight(tile, selectColor);
            Debug.Log($"Selected tile at {tile.HexCoordinates} of type {tile.TileType}");

            if (DefaultHexTileManager.Instance != null)
                DefaultHexTileManager.Instance.SetSelectedTile(tile);
        }

        public void SetSelectedUnit(ISelectable unit)
        {
            // Remove highlight from previous tiles in range
            foreach (var tile in tilesInRange)
                SetTileHighlight(tile, normalColor);

            if (SelectedUnit != null && SelectedUnit != unit)
                SelectedUnit.OnDeselected();

            SelectedUnit = unit;

            if (unit == null)
            {
                Debug.Log("Deselected unit.");
                UpdateTilesInRange();
                return;
            }

            unit.OnSelected();
            Debug.Log($"Selected unit at {unit.HexCoordinates}");
            UpdateTilesInRange();
            // Highlight new tiles in range
            foreach (var tile in tilesInRange)
                SetTileHighlight(tile, selectColor);
        }

        void HandleHover()
        {
            HexTile tile = RaycastTile();
            // Only override color if not in tilesInRange
            if (hoveredTile != null && hoveredTile != SelectedTile && !tilesInRange.Contains(hoveredTile))
                SetTileHighlight(hoveredTile, normalColor);
            if (tile != null && tile != SelectedTile && !tilesInRange.Contains(tile))
                SetTileHighlight(tile, hoverColor);
            hoveredTile = tile;
        }

        void HandleSelect()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                    TrySelectAtPointer();
                if (mouse.rightButton.wasPressedThisFrame)
                    DeselectAll();
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                TrySelectAtPointer();
            }
            if (Input.GetMouseButtonDown(1))
            {
                DeselectAll();
            }

#endif
        }

        void DeselectAll()
        {
            // Remove highlight from all tiles in range
            foreach (var tile in tilesInRange)
                SetTileHighlight(tile, normalColor);

            // Deselect unit
            if (SelectedUnit != null)
            {
                SelectedUnit.OnDeselected();
                SelectedUnit = null;
            }

            // Deselect tile
            if (SelectedTile != null)
            {
                SetTileHighlight(SelectedTile, normalColor);
                SelectedTile = null;
            }
        }

        void TrySelectAtPointer()
        {
            // Try unit selectable first
            var selectable = RaycastSelectable();
            if (selectable != null)
            {
                // Selecting a unit clears tile selection
                SetSelectedTile(null);
                SetSelectedUnit(selectable);
                return;
            }

            // Fallback to tile selection
            HexTile tile = RaycastTile();
            if (tile != null)
            {
                // If a unit is selected, try to move it
                if (SelectedUnit != null && tilesInRange.Contains(tile))
                {
                    // Remove highlight from previous tiles in range
                    foreach (var t in tilesInRange)
                        SetTileHighlight(t, normalColor);
                    // Move the unit to the clicked tile
                    var godBeast = SelectedUnit as GodBeast.GodBeast;
                    if (godBeast != null)
                    {
                        godBeast.gridPosition = new Vector2Int(tile.HexCoordinates.q, tile.HexCoordinates.r);
                        godBeast.transform.position = tile.transform.position;
                        Debug.Log($"Moved GodBeast to {tile.HexCoordinates}");

                        // Resource collection animation and inventory update
                        var debugMenu = FindFirstObjectByType<UI.DebugMenuUI>();
                        if (debugMenu != null)
                        {
                            foreach (var kvp in tile.resourceAmounts)
                            {
                                if (kvp.Key == ResourceType.Food || kvp.Key == ResourceType.Sap)
                                {
                                    debugMenu.AnimateResourceCollection(tile.transform.position, kvp.Key, kvp.Value);
                                }
                            }
                        }
                        // Remove resources from tile after collection
                        tile.RemoveAllResources();

                        // Update explored area after move
                        UpdateTilesInRange();
                        // Optionally deselect after move
                        SetSelectedUnit(null);
                    }
                    return;
                }

                if (SelectedTile == tile)
                {
                    SetTileHighlight(SelectedTile, normalColor);
                    SetSelectedTile(null);
                }
                else
                {
                    SetSelectedTile(tile);
                }
            }
        }

        public void UpdateTilesInRange()
        {
            tilesInRange.Clear();
            if (SelectedUnit == null)
                return;
            if (gridGenerator == null)
            {
                Debug.LogWarning("SelectionManager: gridGenerator is not assigned!");
                return;
            }
            if (gridGenerator.tiles == null || gridGenerator.tiles.Count == 0)
            {
                Debug.LogWarning("SelectionManager: gridGenerator.tiles is empty! Grid may not be generated or tiles dictionary not populated.");
                return;
            }
            Debug.Log($"UpdateTilesInRange: SelectedUnit={SelectedUnit}, HexCoordinates={SelectedUnit.HexCoordinates}");
            Debug.Log($"GridGenerator tiles count: {gridGenerator.tiles.Count}");
            var hexes = GetTilesInRange(SelectedUnit.HexCoordinates, 1);
            Debug.Log($"Neighbor hexes count: {hexes.Count}");
            // Also include and mark the center tile (where the unit stands) as explored
            if (gridGenerator.tiles.TryGetValue(SelectedUnit.HexCoordinates, out var centerTile))
            {
                if (!tilesInRange.Contains(centerTile))
                    tilesInRange.Add(centerTile);
                centerTile.isExplored = true;
                centerTile.UpdateVisual();
            }
            foreach (var hex in hexes)
            {
                if (!gridGenerator.tiles.ContainsKey(hex))
                    Debug.LogWarning($"Hex {hex} not found in gridGenerator.tiles");
                if (gridGenerator.tiles.TryGetValue(hex, out var tile))
                {
                    tilesInRange.Add(tile);
                    tile.isExplored = true; // Mark as explored for fog of war
                    tile.UpdateVisual(); // Optionally update visual immediately
                }
            }
            Debug.Log($"tilesInRange count after update: {tilesInRange.Count}");
        }

        public List<Hex> GetTilesInRange(Hex center, int range)
        {
            var result = new List<Hex>();
            if (range < 1) return result;
            if (range == 1)
            {
                for (int dir = 0; dir < 6; dir++)
                {
                    var neighbor = center.Neighbor(dir);
                    if (gridGenerator.tiles.ContainsKey(neighbor))
                        result.Add(neighbor);
                }
            }
            else
            {
                var visited = new HashSet<Hex> { center };
                var frontier = new List<Hex> { center };
                for (int k = 0; k < range; k++)
                {
                    var nextFrontier = new List<Hex>();
                    foreach (var hex in frontier)
                    {
                        for (int dir = 0; dir < 6; dir++)
                        {
                            var neighbor = hex.Neighbor(dir);
                            if (!visited.Contains(neighbor) && gridGenerator.tiles.ContainsKey(neighbor))
                            {
                                visited.Add(neighbor);
                                nextFrontier.Add(neighbor);
                            }
                        }
                    }
                    frontier = nextFrontier;
                }
                visited.Remove(center);
                result.AddRange(visited);
            }
            return result;
        }

        // Raycast for selectable units first (uses unitsLayerMask). Returns ISelectable if found, otherwise null.
        public ISelectable RaycastSelectable()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return null;
            Vector2 mousePos = mouse.position.ReadValue();
#else
            Vector2 mousePos = Input.mousePosition;
#endif
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -Camera.main.transform.position.z));
            var hits = Physics2D.RaycastAll(worldPos, Vector2.zero, Mathf.Infinity, unitsLayerMask);
            if (hits == null || hits.Length == 0) return null;
            foreach (var h in hits)
            {
                var mbs = h.collider.GetComponents<MonoBehaviour>();
                foreach (var mb in mbs)
                {
                    if (mb is ISelectable sel)
                        return sel;
                }
            }
            return null;
        }

        public HexTile RaycastTile()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return null;
            Vector2 mousePos = mouse.position.ReadValue();
#else
            Vector2 mousePos = Input.mousePosition;
#endif
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -Camera.main.transform.position.z));
            var hit = Physics2D.Raycast(worldPos, Vector2.zero);
            if (hit.collider != null)
                return hit.collider.GetComponent<HexTile>();
            return null;
        }

        public void SetTileHighlight(HexTile tile, Color color)
        {
            if (tile == null) return;
            var sr = tile.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Never override dim for unexplored tiles
                if (!tile.isExplored)
                    sr.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                else
                    sr.color = color;
            }
        }
    }
}
