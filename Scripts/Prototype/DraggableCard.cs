using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Prototype.Cards
{
    /// <summary>
    /// - Moves the card under a drag root (defaults to the root Canvas) so it renders above other UI.
    /// - Creates a placeholder to maintain layout in the original parent during drag.
    /// - Creates a duplicate card to follow the pointer during drag.
    /// - Disables raycast blocking during drag so drop targets receive pointer events.
    /// </summary>
    public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Draggable")]
        [Tooltip("Enable or disable the component's debug logging.")]
        public bool enabledDebug = true;


        /// <summary>
        /// Called when a drag begins. This barebones implementation only logs the event.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (enabledDebug) Debug.Log($"DraggableCard.OnBeginDrag: '{name}' pointerPos={eventData.position}");
            // Apply drag scale if CardView component is present
            CardView cardView = GetComponent<CardView>();
            if (cardView != null)
            {
                // smoothly lerp the card's local scale over time instead of changing instantly
                Vector3 startScale = cardView.transform.localScale;
                Vector3 targetScale = cardView.dragScale * Vector3.one;
                float duration = 0.15f;

                StartCoroutine(ScaleToTarget());

                IEnumerator ScaleToTarget()
                {
                    float elapsed = 0f;
                    while (elapsed < duration)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                        cardView.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                        yield return null;
                    }
                    cardView.transform.localScale = targetScale;
                }
            }
        }

        /// <summary>
        /// Called while dragging. This barebones implementation only logs the current pointer position.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (enabledDebug) Debug.Log($"DraggableCard.OnDrag: '{name}' pointerPos={eventData.position}");
            this.gameObject.transform.position = eventData.position;
        }

        /// <summary>
        /// Called when drag ends. This barebones implementation only logs the event.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (enabledDebug) Debug.Log($"DraggableCard.OnEndDrag: '{name}' pointerPos={eventData.position}");
            // Restore CardView hover/position state (if present) so visuals reset after drag
            CardView cardView = GetComponent<CardView>();
            if (cardView != null)
            {
                try { cardView.RestoreFromDrag(); }
                catch
                {
                    if (enabledDebug) Debug.LogWarning($"DraggableCard.OnEndDrag: '{name}' failed to restore CardView hover/position state after drag.");
                }
            }
        }
    }
}
