using System.Collections.Generic;
using UnityEngine;
using Prototype.Cards;

namespace Prototype.Cards
{
    public class HandUI : MonoBehaviour
    {
        [Header("References")]
        public DeckManager deckManager;
        public GameObject cardPrefab; // assign your existing card prefab here
        public Transform handContainer; // where card prefabs are instantiated
        [Header("Debug")]
        [Tooltip("If true and the hand's parent canvas isn't suitable, create a dedicated overlay Canvas and reparent the handContainer into it")]
        public bool createOverlayIfMissing = true;

        [Header("Layout")]
        [Tooltip("Horizontal spacing (pixels) between card instances in the hand")]
        public float cardSpacing = 80f;
        [Tooltip("Optional start offset for the first card (anchoredPosition)")]
        public Vector2 startOffset = Vector2.zero;

        private List<GameObject> currentViews = new List<GameObject>();

        private void OnEnable()
        {
            if (deckManager == null)
            {
                deckManager = UnityEngine.Object.FindFirstObjectByType<DeckManager>();
            }

            if (deckManager != null)
            {
                deckManager.OnHandChanged += OnHandChanged;

                // Ensure the hand's canvas sorts above other UI (map) so cards are visible
                if (handContainer != null)
                {
                    var canvas = handContainer.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        if (!canvas.overrideSorting || canvas.sortingOrder < 100)
                        {
                            canvas.overrideSorting = true;
                            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 100);
                            Debug.Log($"HandUI: adjusted parent Canvas sortingOrder to {canvas.sortingOrder}", this);
                        }
                    }

                    if (createOverlayIfMissing)
                    {

                        // if current canvas is not ScreenSpaceOverlay (e.g., ScreenSpace-Camera or WorldSpace), create a dedicated overlay
                        bool needOverlay = false;
                        if (canvas == null) needOverlay = true;
                        else if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) needOverlay = true;

                        if (needOverlay)
                        {
                            var overlayName = "HandOverlay_Canvas";
                            var existing = GameObject.Find(overlayName);
                            GameObject overlayGO = existing;
                            if (existing == null)
                            {
                                overlayGO = new GameObject(overlayName);
                                var overlayCanvas = overlayGO.AddComponent<Canvas>();
                                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                                overlayCanvas.overrideSorting = true;
                                overlayCanvas.sortingOrder = 1000;
                                overlayGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                                overlayGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                                Debug.Log("HandUI: Created overlay Canvas for hand UI", this);
                            }

                            if (overlayGO != null)
                            {
                                // reparent the handContainer's transform under the overlay so instantiated cards render above everything
                                try
                                {
                                    handContainer.SetParent(overlayGO.transform, false);
                                    Debug.Log($"HandUI: Reparented handContainer under {overlayGO.name}", this);
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogWarning($"HandUI: Failed to reparent handContainer: {ex}", this);
                                }
                            }
                        }
                    }
                }

                // Immediately sync with current hand in case DeckManager broadcast happened earlier
                Refresh(deckManager.GetHand());
            }
            else
            {
                Debug.LogWarning("HandUI: No DeckManager assigned or found in scene.", this);
            }
        }

        private void OnDisable()
        {
            if (deckManager != null) deckManager.OnHandChanged -= OnHandChanged;
        }

        private void OnHandChanged(List<CardSO> hand)
        {
            Refresh(hand);
        }

        public void Refresh(List<CardSO> hand)
        {
            Debug.Log($"HandUI.Refresh called. handCount={(hand==null?0:hand.Count)} currentViews={currentViews.Count} cardPrefabAssigned={(cardPrefab!=null)} handContainerAssigned={(handContainer!=null)} handContainerActive={(handContainer!=null?handContainer.gameObject.activeInHierarchy:false)}", this);

            // validate prefab/container
            if (cardPrefab == null)
            {
                Debug.LogWarning("HandUI.Refresh: cardPrefab is not assigned.", this);
                return;
            }

            if (handContainer == null)
            {
                Debug.LogWarning("HandUI.Refresh: handContainer is not assigned.", this);
                return;
            }

            // clear
            foreach (var v in currentViews) Destroy(v);
            currentViews.Clear();

            // instantiate (positioned horizontally to avoid stacking)
            for (int i = 0; i < (hand?.Count ?? 0); i++)
            {
                var card = hand[i];
                // instantiate as UI child and preserve local transform (worldPositionStays = false)
                var go = Instantiate(cardPrefab, handContainer, false);
                // force active and sane transform so UI layout won't hide it
                if (!go.activeSelf) go.SetActive(true);

                // ensure the instantiated root has a RectTransform so UI children render correctly under a Canvas
                var rootRT = go.GetComponent<RectTransform>();
                if (rootRT == null)
                {
                    // add a RectTransform and try to copy basic transform state
                    rootRT = go.AddComponent<RectTransform>();
                    rootRT.localScale = Vector3.one;
                    rootRT.anchoredPosition = Vector2.zero;
                    rootRT.localPosition = Vector3.zero;
                    Debug.Log($"HandUI: Added RectTransform to instantiated card root {go.name}", this);
                }
                else
                {
                    rootRT.localScale = Vector3.one;
                    rootRT.anchoredPosition = Vector2.zero;
                    rootRT.localPosition = Vector3.zero;
                }

                // ensure it's rendered on top of siblings in the hand container
                go.transform.SetAsLastSibling();

                var view = go.GetComponent<CardView>();
                if (view != null)
                {
                    // assign stable logical slot index so layout groups can preserve order
                    view.slotIndex = i;
                    view.SetCard(card);
                }
                else
                {
                    Debug.LogWarning($"HandUI.Refresh: Instantiated card prefab is missing CardView component. GameObject={go.name}", this);

                    // Fallback: try to populate TMP texts in the prefab children in a best-effort manner
                    var texts = go.GetComponentsInChildren<TMPro.TMP_Text>(true);
                    if (texts != null && texts.Length > 0)
                    {
                        if (texts.Length >= 1) texts[0].text = card?.cardName ?? "<card>";
                        if (texts.Length >= 2) texts[1].text = card?.description ?? "";
                        if (texts.Length >= 3) texts[2].text = card?.zone.ToString() ?? "";
                        Debug.Log($"HandUI: Fallback populated TMP_Texts on {go.name}", this);
                    }

                    // Ensure there's a visible background image as a last-resort visual aid
                    var img = go.GetComponentInChildren<UnityEngine.UI.Image>(true);
                    if (img != null)
                    {
                        img.color = new Color(0.15f, 0.45f, 0.65f, 0.9f);
                    }
                }

                // position cards to avoid stacking
                var idx = i;
                try
                {
                    var rt2 = go.GetComponent<RectTransform>();
                    if (rt2 != null)
                    {
                        rt2.anchoredPosition = new Vector2(startOffset.x + idx * cardSpacing, startOffset.y);
                    }
                }
                catch { }

                Debug.Log($"HandUI: Spawned card view for '{card?.cardName ?? "<null>"}' as {go.name} parent={go.transform.parent?.name ?? "<null>"} activeInHierarchy={go.activeInHierarchy} pos={go.GetComponent<RectTransform>()?.anchoredPosition}", this);
                currentViews.Add(go);
            }
        }

        // helper: request draw n via DeckManager
        public void DrawN(int n)
        {
            if (deckManager == null) return;
            deckManager.DrawToHand(n);
        }
    }
}
