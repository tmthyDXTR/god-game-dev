using UnityEngine;
using HexGrid;

namespace Prototype.Cards
{
    /// <summary>
    /// Central resolver for playing cards. Keeps card-play wiring out of DeckManager/UI and
    /// provides a place for logging, visual effects, and future rules (targets, costs, validation).
    /// </summary>
    public class CardPlayManager : MonoBehaviour
    {
        [Header("References")]
        public DeckManager deckManager; // optional, will search in scene if null

        private void Awake()
        {
            if (deckManager == null) deckManager = FindFirstObjectByType<DeckManager>();
        }

        /// <summary>
        /// Play a card in the overworld, targeting a tile. This executes the card's Overworld hook
        /// and advances deck state via DeckManager.PlayCard.
        /// </summary>
        public void PlayCardOnTile(CardSO card, HexTile tile)
        {
            Debug.Log($"CardPlayManager: PlayCardOnTile card={card?.cardName} tile={tile?.name}");
            if (card == null || tile == null) return;

            // Call the card's overridable hook. This keeps logic inside the card asset and
            // avoids switch statements in the play manager.
            try
            {
                card.PlayOverworld(tile);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"CardPlayManager: error executing PlayOverworld on card {card.cardName}: {ex}");
            }

            // Advance deck state (remove from hand and move to discard or exhaust)
            if (deckManager != null)
            {
                deckManager.PlayCard(card); 
            }
        }

        /// <summary>
        /// Generic play entry point for other targets. Use overloads as needed for combat, world, etc.
        /// </summary>
        public void PlayCard(CardSO card, object target)
        {
            if (card == null) return;
            // Attempt overworld by default if a HexTile is passed
            if (target is HexTile ht)
            {
                PlayCardOnTile(card, ht);
                return;
            }

            // fallthrough: call generic PlayOverworld with whatever target -- cards can validate types
            try { card.PlayOverworld(target); }
            catch (System.Exception ex) { Debug.LogError($"CardPlayManager.PlayCard: {ex}"); }

            if (deckManager != null) deckManager.PlayCard(card);
        }
    }
}
