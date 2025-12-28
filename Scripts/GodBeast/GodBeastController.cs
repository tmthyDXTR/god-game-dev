using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using HexGrid;

namespace GodBeast
{
    /// <summary>
    /// Controls the God Beast movement using A* pathfinding.
    /// Highlights the path when hovering over tiles while the god beast is selected.
    /// Click to move along the path.
    /// </summary>
    public class GodBeastController : MonoBehaviour
    {
        [Header("References")]
        public GodBeast godBeast;
        public HexGridGenerator gridGenerator;

        [Header("Path Highlighting")]
        [Tooltip("Color to tint path tiles when previewing")]
        public Color pathHighlightColor = new Color(0.5f, 1f, 0.5f, 1f);
        [Tooltip("Color for the destination tile")]
        public Color destinationColor = new Color(0.3f, 0.8f, 0.3f, 1f);

        [Header("Movement")]
        [Tooltip("If true, movement advances on global ticks with smooth interpolation")]
        public bool useTickMovement = true;
        
        // Current path being followed
        private List<HexTile> currentPath;
        private int currentPathIndex = 0;
        private bool isMoving = false;

        // Path preview state
        private List<HexTile> previewPath;
        private List<Color> originalColors = new List<Color>();
        private bool isPreviewingPath = false;
        private HexTile lastHoveredTile;

        // Smooth movement interpolation (like PopulationAgent)
        private Vector3 tickStartPosition;
        private Vector3 tickEndPosition;
        private bool isInterpolatingMovement = false;
        private HexTile currentTile;
        private HexTile targetTile;

        // Tick manager reference
        private Managers.ResourceTickManager resourceTickManager;

        void Start()
        {
            if (godBeast == null)
                godBeast = FindFirstObjectByType<GodBeast>();
            if (gridGenerator == null)
                gridGenerator = FindFirstObjectByType<HexGridGenerator>();

            // Find current tile
            if (godBeast != null && gridGenerator != null)
            {
                var startHex = new Hex(godBeast.gridPosition.x, godBeast.gridPosition.y);
                if (gridGenerator.tiles.TryGetValue(startHex, out HexTile tile))
                    currentTile = tile;
            }

            // Initialize interpolation positions
            if (godBeast != null)
            {
                tickStartPosition = godBeast.transform.position;
                tickEndPosition = godBeast.transform.position;
            }

            // Subscribe to tick events
            resourceTickManager = FindFirstObjectByType<Managers.ResourceTickManager>();
            if (resourceTickManager != null)
                resourceTickManager.OnTickEvent += HandleTick;
        }

        void OnDestroy()
        {
            if (resourceTickManager != null)
                resourceTickManager.OnTickEvent -= HandleTick;
        }

        void Update()
        {
            HandlePathPreview();
            HandleMovementInput();
            HandleSmoothInterpolation();
        }

        void HandlePathPreview()
        {
            if (godBeast == null || gridGenerator == null) return;

            // Only show path preview when god beast is selected
            var selectionManager = SelectionManager.Instance;
            bool isSelected = selectionManager != null && selectionManager.SelectedUnit == godBeast;

            if (!isSelected)
            {
                ClearPathPreview();
                return;
            }

            // Get tile under mouse
            HexTile hoveredTile = GetTileUnderMouse();

            if (hoveredTile == null || hoveredTile == currentTile)
            {
                ClearPathPreview();
                lastHoveredTile = null;
                return;
            }

            // Only recalculate if hovered tile changed
            if (hoveredTile == lastHoveredTile && isPreviewingPath)
                return;

            lastHoveredTile = hoveredTile;

            // Calculate path from god beast to hovered tile
            var startHex = new Hex(godBeast.gridPosition.x, godBeast.gridPosition.y);
            var path = HexPathfinding.FindPathTiles(currentTile, hoveredTile, gridGenerator);

            if (path != null && path.Count > 1)
            {
                ShowPathPreview(path);
            }
            else
            {
                ClearPathPreview();
            }
        }

        void HandleMovementInput()
        {
            if (godBeast == null || isMoving) return;

            // Check for left click
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Only move when god beast is selected and we have a preview path
                var selectionManager = SelectionManager.Instance;
                bool isSelected = selectionManager != null && selectionManager.SelectedUnit == godBeast;

                if (isSelected && previewPath != null && previewPath.Count > 1)
                {
                    StartMovingAlongPath(previewPath);
                    ClearPathPreview();
                }
            }
        }

        void HandleSmoothInterpolation()
        {
            if (!useTickMovement || !isInterpolatingMovement || godBeast == null) return;

            // Smoothly lerp from tickStartPosition to tickEndPosition based on tick progress
            float progress = Managers.GlobalTickManager.Instance != null
                ? Managers.GlobalTickManager.Instance.TickProgress
                : 1f;

            godBeast.transform.position = Vector3.Lerp(tickStartPosition, tickEndPosition, progress);

            // Update sorting order based on Y
            var sr = godBeast.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                float worldY = godBeast.transform.position.y;
                sr.sortingOrder = 1500 + Mathf.RoundToInt(-worldY * 100f);
            }

            // Check if interpolation is complete
            if (progress >= 0.99f)
            {
                CheckArrivalAfterInterpolation();
            }
        }

        void HandleTick()
        {
            if (!useTickMovement || !isMoving || currentPath == null) return;

            // Move to next tile in path
            if (currentPathIndex < currentPath.Count)
            {
                HexTile nextTile = currentPath[currentPathIndex];
                
                // Set up interpolation
                tickStartPosition = godBeast.transform.position;
                tickEndPosition = nextTile.transform.position;
                isInterpolatingMovement = true;
                targetTile = nextTile;
            }
        }

        void CheckArrivalAfterInterpolation()
        {
            if (!isInterpolatingMovement || targetTile == null) return;

            isInterpolatingMovement = false;
            godBeast.transform.position = tickEndPosition;

            // Update current tile
            currentTile = targetTile;
            godBeast.gridPosition = new Vector2Int(currentTile.HexCoordinates.Q, currentTile.HexCoordinates.R);

            // Move to next waypoint
            currentPathIndex++;
            if (currentPathIndex >= currentPath.Count)
            {
                // Arrived at destination
                isMoving = false;
                currentPath = null;
                Debug.Log($"GodBeast arrived at {currentTile.HexCoordinates}");
            }
        }

        void StartMovingAlongPath(List<HexTile> path)
        {
            if (path == null || path.Count < 2) return;

            currentPath = new List<HexTile>(path);
            currentPathIndex = 1; // Start from index 1 (skip current position)
            isMoving = true;

            Debug.Log($"GodBeast starting movement along path of {path.Count} tiles");

            // If not using ticks, we could do instant movement, but we want tick-based
            if (!useTickMovement)
            {
                // Instant move to destination (fallback)
                var dest = path[path.Count - 1];
                godBeast.transform.position = dest.transform.position;
                currentTile = dest;
                godBeast.gridPosition = new Vector2Int(dest.HexCoordinates.Q, dest.HexCoordinates.R);
                isMoving = false;
                currentPath = null;
            }
        }

        void ShowPathPreview(List<HexTile> path)
        {
            ClearPathPreview();

            previewPath = path;
            originalColors.Clear();
            isPreviewingPath = true;

            for (int i = 0; i < path.Count; i++)
            {
                var tile = path[i];
                if (tile == null) continue;

                var sr = tile.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    originalColors.Add(sr.color);

                    // Destination gets special color, path tiles get path color
                    if (i == path.Count - 1)
                        sr.color = destinationColor;
                    else if (i > 0) // Skip current tile
                        sr.color = pathHighlightColor;
                    else
                        originalColors[originalColors.Count - 1] = sr.color; // Keep original for start
                }
                else
                {
                    originalColors.Add(Color.white);
                }
            }
        }

        void ClearPathPreview()
        {
            if (!isPreviewingPath || previewPath == null) return;

            for (int i = 0; i < previewPath.Count && i < originalColors.Count; i++)
            {
                var tile = previewPath[i];
                if (tile == null) continue;

                var sr = tile.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = originalColors[i];
                }
            }

            previewPath = null;
            originalColors.Clear();
            isPreviewingPath = false;
        }

        HexTile GetTileUnderMouse()
        {
            if (Camera.main == null) return null;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));

            // Convert to hex and find tile
            var hex = gridGenerator.layout.PixelToHexRounded(new Point(worldPos.x, worldPos.y));

            if (gridGenerator.tiles.TryGetValue(hex, out HexTile tile))
                return tile;

            return null;
        }

        /// <summary>
        /// Externally command the god beast to move to a specific tile.
        /// </summary>
        public void MoveTo(HexTile destination)
        {
            if (destination == null || currentTile == null || isMoving) return;

            var path = HexPathfinding.FindPathTiles(currentTile, destination, gridGenerator);
            if (path != null && path.Count > 1)
            {
                StartMovingAlongPath(path);
            }
        }

        /// <summary>
        /// Check if the god beast is currently moving.
        /// </summary>
        public bool IsMoving => isMoving;

        /// <summary>
        /// Get the current tile the god beast is on.
        /// </summary>
        public HexTile CurrentTile => currentTile;
    }
}
