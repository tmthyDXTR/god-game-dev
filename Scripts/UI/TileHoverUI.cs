using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HexGrid;
using TMPro;

// Simple hover popup for HexTile info.
// Attach to an active GameObject (e.g. UI manager). Assign references in inspector.
public class TileHoverUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Screen-space canvas (Screen Space - Camera or Overlay).")]
    public Canvas uiCanvas;
    [Tooltip("Camera used to render world (usually your main camera).")]
    public Camera worldCamera;
    [Tooltip("Panel GameObject containing UI Texts (initially inactive).")]
    public GameObject popupPanel;
    [Tooltip("Text element for the title / tile type.")]
    public TextMeshProUGUI titleText;
    [Tooltip("Text element for the body/details.")]
    public TextMeshProUGUI bodyText;

    [Header("Raycast")]
    [Tooltip("Layer mask for tiles (create a 'Tiles' layer and assign to hex tile GameObjects).")]
    public LayerMask tileLayerMask;

    [Header("Layout")]
    [Tooltip("Screen offset for the popup from mouse position (pixels).")]
    public Vector2 popupOffset = new Vector2(16f, -16f);

    // cached RectTransform for popup positioning
    RectTransform popupRect;

    void Awake()
    {
        if (popupPanel != null)
            popupRect = popupPanel.GetComponent<RectTransform>();

        // fallbacks
        if (worldCamera == null) worldCamera = Camera.main;
        if (uiCanvas == null) uiCanvas = FindFirstObjectByType<Canvas>();
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    void Update()
    {
        // If pointer is over UI element (e.g., card UI) don't show map hover
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            HidePopup();
            return;
        }

        // Raycast from mouse into world (2D) using the new Input System.
        Vector2 mouseScreen = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        // Build a screen-space Vector3 for ScreenToWorldPoint (use camera near plane)
        Vector3 mousePos = new Vector3(mouseScreen.x, mouseScreen.y, worldCamera != null ? worldCamera.nearClipPlane : 0f);
        Vector3 worldPoint = worldCamera != null ? worldCamera.ScreenToWorldPoint(mousePos) : Vector3.zero;
        Vector2 worldPoint2D = new Vector2(worldPoint.x, worldPoint.y);

        // Physics2D Raycast: for 2D tiled map (sprite + collider)
        RaycastHit2D hit = Physics2D.Raycast(worldPoint2D, Vector2.zero, 0f, tileLayerMask);
        if (hit.collider != null)
        {
            var tile = hit.collider.GetComponent<HexTile>();
            if (tile != null)
            {
                ShowPopup(tile, mousePos);
                return;
            }
        }

        // Nothing hit
        HidePopup();
    }

    void ShowPopup(HexTile tile, Vector3 screenPos)
    {
        if (popupPanel == null || titleText == null || bodyText == null || uiCanvas == null)
            return;

        // Update text content (concise, relevant info)
        titleText.text = tile.TileType.ToString();
        int food = tile.GetResourceAmount(ResourceType.Food);
        int sap = tile.GetResourceAmount(ResourceType.Sap);
        string resources = "";
        if (food > 0) resources += $"Food: {food}\n";
        if (sap > 0) resources += $"Sap: {sap}\n";

        bodyText.text = $"Pop: {tile.populationCount}\n" +
                        resources +
                        $"Explored: {tile.isExplored}";

        // Ensure visible
        if (!popupPanel.activeSelf) popupPanel.SetActive(true);

        // Position at mousecursor with offset
        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
              uiCanvas.transform as RectTransform,
              screenPos + (Vector3)popupOffset,
              uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera,
              out anchoredPos);
        popupRect.anchoredPosition = anchoredPos;
    }

    void HidePopup()
    {
        if (popupPanel != null && popupPanel.activeSelf)
            popupPanel.SetActive(false);
    }

    void OnDisable()
    {
        HidePopup();
    }
}