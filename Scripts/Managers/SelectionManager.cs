using System.Collections.Generic;
using System.Resources;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HexGrid
{
    public class SelectionManager : MonoBehaviour
    {
        public static SelectionManager Instance { get; private set; }

        [Header("Selection Colors")]
        public Color hoverColor = HighlightConfig.Hover;
        public Color selectColor = HighlightConfig.Select;
        public Color normalColor = HighlightConfig.Normal;

        [SerializeField]
        public HexTile SelectedTile { get; private set; }
        public ISelectable SelectedUnit { get; private set; }

        public HexGridGenerator gridGenerator;

        public bool IsPointerOverUI => isPointerOverUI;
        private bool isPointerOverUI = false;

        [Header("UI Layer")]
        [Tooltip("Layer mask used to detect UI elements (e.g. card prefabs). Set to your UI layer, e.g. layer 5.")]
        public LayerMask uiLayerMask = 1 << 5;

        [Header("Selection Layers")]
        [Tooltip("Layer mask used to detect selectable units (units layer)")]
        public LayerMask unitsLayerMask;

        [Header("Tiles in Range")]
        [SerializeField]
        public List<HexTile> tilesInRange = new List<HexTile>();

        private HexTile hoveredTile;
        // Track per-tile highlight owners to allow layered, non-destructive highlights.
        private Dictionary<HexTile, Dictionary<string, Color>> tileHighlightOwners = new Dictionary<HexTile, Dictionary<string, Color>>();

        // Owner priority (higher in list = higher priority)
        // Move "Hover" above "Range" so hover color can take precedence when desired.
        private readonly List<string> highlightPriority = new List<string> { "Selection", "CardTarget", "Hover", "Range" };


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
            UpdatePointerOverUI();
            HandleHover();
            HandleSelect();
        }

        public void SetSelectedTile(HexTile tile)
        {
            // Clear previous selection highlight
            if (SelectedTile != null && SelectedTile != tile)
                SetTileHighlight(SelectedTile, null, "Selection");

            SelectedTile = tile;

            if (tile == null)
            {
                Debug.Log("Deselected tile.");
                return;
            }

            SetTileHighlight(tile, selectColor, "Selection");
            Debug.Log($"Selected tile at {tile.HexCoordinates} of type {tile.TileType}");

            if (DefaultHexTileManager.Instance != null)
                DefaultHexTileManager.Instance.SetSelectedTile(tile);
        }

        public void SetSelectedUnit(ISelectable unit)
        {
            // Remove highlight from previous tiles in range
            foreach (var tile in tilesInRange)
                SetTileHighlight(tile, null, "Range");

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
                SetTileHighlight(tile, selectColor, "Range");
        }

        void HandleHover()
        {
            // If pointer is over UI, clear any hover highlight and skip hover logic
            if (IsPointerOverUI)
            {
                    if (hoveredTile != null && hoveredTile != SelectedTile)
                        SetTileHighlight(hoveredTile, null, "Hover"); // Clear hover highlight
                hoveredTile = null;
                return;
            }

            HexTile tile = RaycastTile();
                // Clear previous hover owner regardless of range (we only remove the "Hover" owner)
                if (hoveredTile != null && hoveredTile != SelectedTile)
                    SetTileHighlight(hoveredTile, null, "Hover");
                // Apply hover owner even for tiles in tilesInRange; visual precedence determined by highlightPriority
                if (tile != null && tile != SelectedTile)
                    SetTileHighlight(tile, hoverColor, "Hover");
            hoveredTile = tile;
        }

        void HandleSelect()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    if (!IsPointerOverUI)
                        TrySelectAtPointer();
                }
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    if (!IsPointerOverUI)
                        DeselectAll();
                }
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                if (!IsPointerOverUI)
                    TrySelectAtPointer();
            }
            if (Input.GetMouseButtonDown(1))
            {
                if (!IsPointerOverUI)
                    DeselectAll();
            }

#endif
        }

        void UpdatePointerOverUI()
        {
            bool overUI = false;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                overUI = true;
            }
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            else
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null)
                {
                    Vector2 mousePos = mouse.position.ReadValue();
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -Camera.main.transform.position.z));
                    var hits = Physics2D.RaycastAll(worldPos, Vector2.zero, Mathf.Infinity, uiLayerMask);
                    if (hits != null && hits.Length > 0)
                        overUI = true;
                }
            }
#else
            else
            {
                Vector2 mousePos = Input.mousePosition;
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -Camera.main.transform.position.z));
                var hits = Physics2D.RaycastAll(worldPos, Vector2.zero, Mathf.Infinity, uiLayerMask);
                if (hits != null && hits.Length > 0)
                    overUI = true;
            }
#endif
            isPointerOverUI = overUI;
        }

        void DeselectAll()
        {
            // Remove highlight from all tiles in range
            foreach (var tile in tilesInRange)
                SetTileHighlight(tile, null, "Range");

            // Deselect unit
            if (SelectedUnit != null)
            {
                SelectedUnit.OnDeselected();
                SelectedUnit = null;
            }

            // Deselect tile
            if (SelectedTile != null)
            {
                SetTileHighlight(SelectedTile, null, "Selection");
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
                        SetTileHighlight(t, null, "Range");
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
                                // Animate collection for supported global resources and add to global store
                                if (kvp.Key == Managers.ResourceManager.GameResource.Food || kvp.Key == Managers.ResourceManager.GameResource.Faith || kvp.Key == Managers.ResourceManager.GameResource.Materials)
                                {
                                    debugMenu.AnimateResourceCollection(tile.transform.position, kvp.Key, kvp.Value);
                                    var rm = Managers.ResourceManager.Instance;
                                    if (rm != null)
                                    {
                                        rm.AddResource(kvp.Key, kvp.Value);
                                    }
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
                    SetTileHighlight(SelectedTile, null, "Selection");
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

        // Set or clear a highlight for a specific owner. Passing `null` color removes that owner's highlight.
        public void SetTileHighlight(HexTile tile, Color? color, string owner = "default")
        {
            if (tile == null) return;

            // ensure entry
            if (!tileHighlightOwners.TryGetValue(tile, out var owners))
            {
                if (color == null)
                    return; // nothing to remove
                owners = new Dictionary<string, Color>();
                tileHighlightOwners[tile] = owners;
            }

            if (color == null)
            {
                // remove owner
                if (owners.ContainsKey(owner))
                    owners.Remove(owner);
                if (owners.Count == 0)
                    tileHighlightOwners.Remove(tile);
            }
            else
            {
                owners[owner] = color.Value;
            }

            // determine effective color by priority
            Color? effective = null;
            foreach (var p in highlightPriority)
            {
                if (owners != null && owners.TryGetValue(p, out var c))
                {
                    effective = c;
                    break;
                }
            }
            // fallback to any owner's color if none in priority list
            if (effective == null && owners != null && owners.Count > 0)
            {
                foreach (var kv in owners)
                {
                    effective = kv.Value;
                    break;
                }
            }

            tile.SetHighlight(effective);
        }
    }
}
