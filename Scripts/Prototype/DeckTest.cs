using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Prototype.Cards
{
    [DisallowMultipleComponent]
    public class DeckTest : MonoBehaviour
    {
        [Tooltip("Reference to the DeckManager in scene")]
        public DeckManager deckManager;

        [Tooltip("How many cards to draw when running the test")]
        public int drawCount = 5;

        [Tooltip("Automatically draw on Start if true")]
        public bool drawOnStart = true;

        private void Start()
        {
            if (drawOnStart) DrawNow();
        }

        // Inspector-friendly method to trigger the draw from the context menu
        [ContextMenu("Draw N Cards")]
        public void DrawNow()
        {
            if (deckManager == null)
            {
                // Use the newer API to avoid obsolete warning
                deckManager = UnityEngine.Object.FindFirstObjectByType<DeckManager>();
                if (deckManager == null)
                {
                    Debug.LogWarning("DeckTest: No DeckManager found in scene.", this);
                    return;
                }
            }

            // Ensure the deck is initialized (useful if script execution order differs)
            deckManager.ResetDeck();

            var hand = deckManager.DrawToHand(Mathf.Max(0, drawCount));
            Debug.Log($"DeckTest: Drew {hand.Count} cards (requested {drawCount}).", this);

            // Log deck internals to help debug empty draws
            try
            {
                Debug.Log($"DeckTest: Deck state after draw - initial:{deckManager.initialDeck.Count}, draw:{deckManager.DrawPileCount}, discard:{deckManager.DiscardPileCount}, hand:{deckManager.HandCount}", this);
            }
            catch (System.Exception)
            {
                // ignore if reflection/access changed
            }
        }
    }
}
