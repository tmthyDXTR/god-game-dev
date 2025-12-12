using UnityEngine;
using HexGrid;

namespace HexGrid
{
    public class HexTileSelector : MonoBehaviour
    {
        public Color hoverColor = new Color(1f, 1f, 0.5f, 0.5f); // light yellow
        public Color selectColor = new Color(1f, 1f, 0f, 0.7f); // yellow
        public Color normalColor = Color.white;

        private HexTile hoveredTile;
        private HexTile selectedTile;

        void Update()
        {
            // Defer to SelectionManager when present
            if (SelectionManager.Instance != null) return;

            HandleHover();
            HandleSelect();
        }

        void HandleHover()
        {
            HexTile tile = RaycastTile();
            if (hoveredTile != null && hoveredTile != selectedTile)
                SetTileHighlight(hoveredTile, null);
            if (tile != null && tile != selectedTile)
                SetTileHighlight(tile, hoverColor);
            hoveredTile = tile;
        }

        void HandleSelect()
        {
            #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                HexTile tile = RaycastTile();
                if (tile != null)
                {
                    if (selectedTile == tile)
                    {
                        // Deselect
                        SetTileHighlight(selectedTile, null);
                        selectedTile = null;
                        if (DefaultHexTileManager.Instance != null)
                            DefaultHexTileManager.Instance.SetSelectedTile(null);
                    }
                    else
                    {
                        if (selectedTile != null)
                            SetTileHighlight(selectedTile, null);
                        SetTileHighlight(tile, selectColor);
                        selectedTile = tile;
                        if (DefaultHexTileManager.Instance != null)
                            DefaultHexTileManager.Instance.SetSelectedTile(tile);
                    }
                }
            }
            #else
            if (Input.GetMouseButtonDown(0))
            {
                HexTile tile = RaycastTile();
                if (tile != null)
                {
                    if (selectedTile != null)
                        SetTileHighlight(selectedTile, null);
                    SetTileHighlight(tile, selectColor);
                    selectedTile = tile;
                    if (DefaultHexTileManager.Instance != null)
                        DefaultHexTileManager.Instance.SetSelectedTile(tile);
                }
            }
            #endif
        }

        HexTile RaycastTile()
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

        void SetTileHighlight(HexTile tile, Color? color)
        {
            if (tile == null) return;
            tile.SetHighlight(color);
        }
    }
}
