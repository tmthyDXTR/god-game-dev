using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Prototype.Cards;

namespace Prototype.Cards
{
    [DisallowMultipleComponent]
    public class DeckManager : MonoBehaviour
    {
        [Header("Deck configuration")]
        public List<CardSO> initialDeck = new List<CardSO>();
        public bool shuffleOnStart = true;

        [Header("Debug")]
        public bool debugDeckManager = false;

        // runtime
        private List<CardSO> drawPile = new List<CardSO>();
        private List<CardSO> discardPile = new List<CardSO>();
        private List<CardSO> hand = new List<CardSO>();

        // save seed for reproducible runs
        private System.Random rng = new System.Random();

        public event Action<List<CardSO>> OnHandChanged;

        // mark whether ResetDeck has run to avoid double-initializing during test calls
        private bool isInitialized = false;

        private void Start()
        {
            if (!isInitialized)
            {
                ResetDeck();
            }
        }

        public void ResetDeck()
        {
            drawPile.Clear();
            discardPile.Clear();
            hand.Clear();

            drawPile.AddRange(initialDeck);
            if (shuffleOnStart) Shuffle(drawPile);
            if (debugDeckManager) Debug.Log("DeckManager: ResetDeck() — deck reset and initialised", this);
            BroadcastHandChanged();
            isInitialized = true;
        }

        public void Shuffle(List<CardSO> list)
        {
            int n = list.Count;
            for (int i = 0; i < n - 1; i++)
            {
                int j = rng.Next(i, n);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
            if (debugDeckManager) Debug.Log($"DeckManager: Shuffle() shuffled {n} items", this);
        }

        // Draw N cards to hand (will attempt reshuffle when needed)
        public List<CardSO> DrawToHand(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var card = DrawOne();
                if (card != null)
                {
                    hand.Add(card);
                    if (debugDeckManager) Debug.Log($"DeckManager: DrawToHand() drew '{card.name}'", this);
                }
            }

            BroadcastHandChanged();
            return new List<CardSO>(hand);
        }

        private CardSO DrawOne()
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0) return null; // nothing to draw
                // reshuffle discard into draw
                drawPile.AddRange(discardPile);
                discardPile.Clear();
                Shuffle(drawPile);
                if (debugDeckManager) Debug.Log("DeckManager: DrawOne() — reshuffled discard into draw pile", this);
            }

            var top = drawPile[0];
            drawPile.RemoveAt(0);
            if (debugDeckManager) Debug.Log($"DeckManager: DrawOne() -> '{top.name}'", this);
            return top;
        }

        public void Discard(CardSO card)
        {
            if (card == null) return;
            discardPile.Add(card);
            if (hand.Remove(card)) BroadcastHandChanged();
            if (debugDeckManager) Debug.Log($"DeckManager: Discard() -> '{card.name}' moved to discard", this);
        }

        public void PlayCard(CardSO card)
        {
            // play resolves elsewhere; here we remove from hand and if not exhaust, goes to discard
            if (hand.Remove(card))
            {
                if (card.exhaustsOnUse)
                {
                    // do nothing - removed from play
                }
                else
                {
                    discardPile.Add(card);
                }

                BroadcastHandChanged();
                if (debugDeckManager) Debug.Log($"DeckManager: PlayCard() -> '{card.name}' played (exhausts:{card.exhaustsOnUse})", this);
            }
        }

        public List<CardSO> GetHand() => new List<CardSO>(hand);

        // Debug/read-only accessors for runtime inspection
        public int DrawPileCount => drawPile.Count;
        public int DiscardPileCount => discardPile.Count;
        public int HandCount => hand.Count;

        public void LogState()
        {
            if (debugDeckManager)
            {
                Debug.Log($"DeckManager state - initialDeck:{initialDeck.Count}, draw:{drawPile.Count}, discard:{discardPile.Count}, hand:{hand.Count}", this);
                var names = new System.Text.StringBuilder();
                names.AppendLine("Hand:");
                foreach (var c in hand) names.AppendLine($" - {c.name}");
                names.AppendLine("DrawPile (top 8):");
                for (int i = 0; i < Mathf.Min(8, drawPile.Count); i++) names.AppendLine($" {i}: {drawPile[i].name}");
                Debug.Log(names.ToString(), this);
            }
        }

        // Helper for Editor UI: shuffle the current draw pile
        public void ShuffleDrawPile()
        {
            Shuffle(drawPile);
            if (debugDeckManager) Debug.Log("DeckManager: ShuffleDrawPile() called", this);
        }

        // Helper: force reshuffle of discard into draw pile (keeps order randomised)
        public void ReshuffleDiscardIntoDraw()
        {
            if (discardPile.Count == 0) return;
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);
            if (debugDeckManager) Debug.Log("DeckManager: ReshuffleDiscardIntoDraw() called", this);
        }

        private void BroadcastHandChanged()
        {
            OnHandChanged?.Invoke(new List<CardSO>(hand));
        }
    }
}
