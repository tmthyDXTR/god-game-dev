using UnityEngine;

namespace Prototype.Cards
{
    /// <summary>
    /// Skeleton controller for card targeting mode.
    /// Currently a minimal stub; will later manage enter/exit targeting, highlights,
    /// and invoking CardPlayManager when a target is confirmed.
    /// </summary>
    public class CardTargetingController : MonoBehaviour
    {
        public static CardTargetingController Instance { get; private set; }

        private CardSO activeCard;
        private CardPlayManager playManager;
        private HexGrid.SelectionManager selectionManager;

        // targeting highlights are non-destructive and use SelectionManager/HexTile.SetHighlight

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            playManager = FindFirstObjectByType<CardPlayManager>();
            selectionManager = FindFirstObjectByType<HexGrid.SelectionManager>();
        }

        /// <summary>
        /// Begin targeting for the provided card. Highlights valid tiles.
        /// </summary>
        public void StartTargeting(CardSO card)
        {
            if (card == null) return;
            // Already targeting another card -> cancel first
            if (activeCard != null) CancelTargeting();

            activeCard = card;
            Debug.Log($"CardTargetingController: Start targeting card={card.cardName}");
            HighlightValidTiles(true);
        }

        /// <summary>
        /// Cancel any active targeting and restore tile visuals.
        /// </summary>
        public void CancelTargeting()
        {
            Debug.Log("CardTargetingController: Cancel targeting");
            HighlightValidTiles(false);
            activeCard = null;
        }

        private void Update()
        {
            if (activeCard == null) return;

            var mouse = UnityEngine.InputSystem.Mouse.current;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;

            // cancel on right-click or Esc
            if ((mouse != null && mouse.rightButton.wasPressedThisFrame) || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            {
                CancelTargeting();
                return;
            }

            // confirm on left-click (ignore if pointer over UI)
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                if (selectionManager != null && selectionManager.IsPointerOverUI) return;

                var tile = selectionManager != null ? selectionManager.RaycastTile() : null;
                if (tile == null) return;

                if (!activeCard.CanTarget(tile))
                {
                    Debug.Log("CardTargetingController: invalid target for this card");
                    return;
                }

                if (playManager != null)
                {
                    playManager.PlayCardOnTile(activeCard, tile);
                }

                CancelTargeting();
            }
        }
        // Highlight valid tiles based on the active card's targeting rules searches all tiles !TODO if map large, optimize with range checks or tile tags
        private void HighlightValidTiles(bool on)
        {
            if (selectionManager == null || selectionManager.gridGenerator == null) return;

            var grid = selectionManager.gridGenerator;
            if (grid.tiles == null) return;

            if (on)
            {
                foreach (var kv in grid.tiles)
                {
                    var tile = kv.Value;
                    if (tile == null) continue;
                        bool valid = activeCard != null && activeCard.CanTarget(tile);
                        if (valid)
                            selectionManager.SetTileHighlight(tile, selectionManager.selectColor, "CardTarget");
                        else
                            selectionManager.SetTileHighlight(tile, null, "CardTarget");
                }
            }
            else
            {
                // clear highlights
                foreach (var kv in grid.tiles)
                {
                    var tile = kv.Value;
                    if (tile == null) continue;
                        selectionManager.SetTileHighlight(tile, null, "CardTarget");
                }
            }
        }
    }
}
