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
            }

            var top = drawPile[0];
            drawPile.RemoveAt(0);
            return top;
        }

        public void Discard(CardSO card)
        {
            if (card == null) return;
            discardPile.Add(card);
            if (hand.Remove(card)) BroadcastHandChanged();
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
            }
        }

        public List<CardSO> GetHand() => new List<CardSO>(hand);

        // Debug/read-only accessors for runtime inspection
        public int DrawPileCount => drawPile.Count;
        public int DiscardPileCount => discardPile.Count;
        public int HandCount => hand.Count;

        public void LogState()
        {
            Debug.Log($"DeckManager state - initialDeck:{initialDeck.Count}, draw:{drawPile.Count}, discard:{discardPile.Count}, hand:{hand.Count}", this);
        }

        private void BroadcastHandChanged()
        {
            OnHandChanged?.Invoke(new List<CardSO>(hand));
        }
    }
}
