using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Prototype.Cards;
using UnityEngine.EventSystems;

namespace Prototype.Cards
{
    [RequireComponent(typeof(Button))]
    public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Bindings")]
        public Image artworkImage;
        public Image zoneImage;
        public TMP_Text cardName;
        public TMP_Text cardText;
        public TMP_Text cardZone;

        [Header("Hover")]
        [Tooltip("Vertical offset (pixels) the card moves up on hover")]
        public float hoverOffset = 10f;
        [Tooltip("When true, compute the hover offset as a percentage of the card height instead of a fixed pixel value")]
        public bool useRelativeHoverOffset = false;
        [Range(0f, 1f)]
        [Tooltip("Relative fraction of the card height to use as the hover offset (0..1). Used when `useRelativeHoverOffset` is true.")]
        public float hoverOffsetPercent = 0.2f;
        [Tooltip("Hover animation duration in seconds")]
        public float hoverDuration = 0.12f;
        [Tooltip("Optional scale applied on hover")]
        public float hoverScale = 1.05f;
        [Tooltip("Small enter delay to avoid flicker from quick pointer moves")]
        public float hoverEnterDelay = 0.04f;
        [Tooltip("Small exit delay to debounce rapid exit/enter flicker")]
        public float hoverExitDelay = 0.06f;

        [Header("Options")]
        [Tooltip("If true, animate RectTransform.localPosition; otherwise animate anchoredPosition")]
        public bool useLocalPosition = false;
        [Tooltip("Enable verbose debug logging for hover events")]
        public bool debugHover = true;
        
        [Header("Bring-To-Front")]
        [Tooltip("If true, use a temporary Canvas override (overrideSorting) while hovered so the card renders above siblings without changing sibling indices.")]
        public bool useCanvasOverrideOnHover = true;
        [Tooltip("Sorting order to apply to the temporary hover Canvas. Higher values render on top.")]
        public int hoverSortingOrder = 2000;
        [Tooltip("If true, do not change the card's sibling index while hovered; use Canvas override to render on top instead. If false, SetAsLastSibling() will be used when Canvas override is disabled.")]
        public bool keepSiblingIndexWhileHovered = true;
        [Tooltip("If true, restore the original sibling index when hover exits. If false, sibling index will not be restored.")]
        public bool restoreSiblingOnExit = true;

        private RectTransform rt;
        private CardSO card;

        private Vector2 originalAnchoredPos;
        private Vector3 originalLocalPos;
        // scale to restore for the RectTransform itself (rarely used) and
        // a separate visual root that we scale for hover effects so the
        // RectTransform's hit area remains stable.
        private Vector3 originalLocalScale;
        [Tooltip("Optional: assign a child Transform that contains only visual elements. If set, that child will be moved/scaled on hover while the root hit area stays fixed.")]
        [SerializeField]
        private Transform visualRoot;
        private Vector3 originalVisualLocalScale;
        private Quaternion originalVisualLocalRot;
        private Vector3 originalVisualLocalPos;
        private bool baselineCaptured = false;
        // Logical slot index assigned by the hand spawner to preserve ordering
        [HideInInspector]
        public int slotIndex = -1;

        private Transform originalParent;
        private int originalSiblingIndex;
        private Transform siblingAfterOnHover;
        private UnityEngine.UI.LayoutGroup parentLayoutGroup;
        private bool parentLayoutGroupEnabledState = false;

        private Coroutine enterDelayCoroutine;
        private Coroutine animCoroutine;
        private Coroutine watchdogCoroutine;
        private Coroutine exitDelayCoroutine;
        private bool elevated = false;
        private bool isHovered = false;
        private bool isLifted = false;
        // whether the currently-running animation (if any) is an entering animation
        private bool activeAnimationEntering = false;

        // Maximum absolute pixels we'll allow for hoverOffset to avoid runaway values
        private const float kMaxHoverOffsetAbs = 100f;
        // the offset value actually used for the current hover (clamped at start)
        private float activeHoverOffset = 0f;
        
    // Hover canvas override state (optional)
    private Canvas hoverCanvas;
    private bool hoverCanvasCreated = false;
    private bool hoverCanvasHadOverride = false;
    private int hoverCanvasOriginalOrder = 0;

        private void OnValidate()
        {
            float clamped = Mathf.Clamp(hoverOffset, -kMaxHoverOffsetAbs, kMaxHoverOffsetAbs);
            if (Mathf.Abs(clamped - hoverOffset) > 0.001f)
            {
                Debug.LogWarning($"CardView.OnValidate '{name}': hoverOffset {hoverOffset} clamped to {clamped}");
                hoverOffset = clamped;
            }
            // ensure percent is in valid range
            float p = Mathf.Clamp01(hoverOffsetPercent);
            if (Mathf.Abs(p - hoverOffsetPercent) > 0.0001f)
            {
                Debug.LogWarning($"CardView.OnValidate '{name}': hoverOffsetPercent {hoverOffsetPercent} clamped to {p}");
                hoverOffsetPercent = p;
            }
        }

        private void Awake()
        {
            rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                originalAnchoredPos = rt.anchoredPosition;
                originalLocalPos = rt.localPosition;
                originalLocalScale = rt.localScale;
                // prefer to scale the first child (visual content) instead of the
                // root RectTransform so that pointer hit tests don't change when
                // we scale the visuals. Fall back to the root if no child found.
                if (rt.childCount > 0)
                    visualRoot = rt.GetChild(0);
                else
                    visualRoot = rt;
                originalVisualLocalScale = visualRoot.localScale;
                originalVisualLocalPos = visualRoot.localPosition;
            }
            // ensure hoverOffset is within safe bounds at runtime as well
            float clamped = Mathf.Clamp(hoverOffset, -kMaxHoverOffsetAbs, kMaxHoverOffsetAbs);
            if (Mathf.Abs(clamped - hoverOffset) > 0.001f && debugHover)
            {
                Debug.LogWarning($"CardView.Awake '{name}': hoverOffset {hoverOffset} clamped to {clamped} to avoid extreme movement");
                hoverOffset = clamped;
            }
            originalSiblingIndex = transform.GetSiblingIndex();
        }

        public void SetCard(CardSO so)
        {
            card = so;
            Refresh();
        }

        public void Refresh()
        {
            if (card == null) return;
            if (cardName) cardName.text = card.cardName;
            if (cardText) cardText.text = card.description;
            if (cardZone) cardZone.text = card.zone.ToString();
            if (artworkImage) artworkImage.sprite = card.artwork;
            if (zoneImage) zoneImage.sprite = card.zoneIcon;
            // allow baseline to be captured after visuals/positions are applied
            baselineCaptured = false;
        }

        public CardSO GetCard() => card;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isHovered) return;
            isHovered = true;

            // cancel any pending exit debounce so we don't flicker
            if (exitDelayCoroutine != null)
            {
                StopCoroutine(exitDelayCoroutine);
                exitDelayCoroutine = null;
            }

            if (rt == null) rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                // Don't overwrite the captured baseline if we already have one.
                // Overwriting here during an in-flight animation causes the baseline
                // to shift and prevents reliable snap-back.
                if (!baselineCaptured)
                {
                    originalAnchoredPos = rt.anchoredPosition;
                    originalLocalPos = rt.localPosition;
                    originalLocalScale = rt.localScale;
                    if (visualRoot != null)
                    {
                        originalVisualLocalScale = visualRoot.localScale;
                        originalVisualLocalPos = visualRoot.localPosition;
                        originalVisualLocalRot = visualRoot.localRotation;
                    }
                    baselineCaptured = true;
                }
            }

            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            siblingAfterOnHover = (originalParent != null && originalSiblingIndex + 1 < originalParent.childCount) ? originalParent.GetChild(originalSiblingIndex + 1) : null;

            if (enterDelayCoroutine != null) StopCoroutine(enterDelayCoroutine);
            if (hoverEnterDelay > 0f)
            {
                enterDelayCoroutine = StartCoroutine(EnterDelayRoutine());
            }
            else
            {
                StartHover();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // ignore spurious exits if the pointer is actually still over the rect
            if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, eventData.position, eventData.enterEventCamera))
            {
                if (debugHover) Debug.Log($"CardView.OnPointerExit '{name}': ignored exit; pointer still inside rect");
                return;
            }

            if (!isHovered) return;
            // mark not hovered immediately; the exit will be debounced so quick re-enters cancel it
            isHovered = false;

            if (exitDelayCoroutine != null) { StopCoroutine(exitDelayCoroutine); exitDelayCoroutine = null; }
            exitDelayCoroutine = StartCoroutine(ExitDelayRoutine());
        }

        private System.Collections.IEnumerator EnterDelayRoutine()
        {
            float d = hoverEnterDelay;
            if (debugHover) Debug.Log($"CardView.EnterDelay '{name}': delay={d}");
            if (d > 0f) yield return new WaitForSecondsRealtime(d);
            if (!isHovered) { enterDelayCoroutine = null; yield break; }
            enterDelayCoroutine = null;
            StartHover();
        }

        private System.Collections.IEnumerator ExitWatchdog()
        {
            // allow some time for the exit animation to complete; if it doesn't, force reset
            float wait = Mathf.Max(0.1f, hoverDuration * 1.5f);
            yield return new WaitForSecondsRealtime(wait);
            watchdogCoroutine = null;
            if (!isHovered && isLifted)
            {
                if (debugHover) Debug.Log($"CardView.ExitWatchdog '{name}': forcing ResetPositionImmediate (isHovered={isHovered} isLifted={isLifted})");
                ResetPositionImmediate();
            }
        }

        private System.Collections.IEnumerator ExitDelayRoutine()
        {
            float d = Mathf.Max(0f, hoverExitDelay);
            if (d > 0f) yield return new WaitForSecondsRealtime(d);
            exitDelayCoroutine = null;
            // if pointer re-entered while we were waiting, do nothing
            if (isHovered) yield break;

            // if an enter delay was pending, cancel and snap back
            if (enterDelayCoroutine != null)
            {
                StopCoroutine(enterDelayCoroutine);
                enterDelayCoroutine = null;
                if (animCoroutine != null) { StopCoroutine(animCoroutine); animCoroutine = null; }
                ResetPositionImmediate();
                yield break;
            }

            // if an animation is currently running, reverse it smoothly
            if (animCoroutine != null)
            {
                if (debugHover) Debug.Log($"CardView.ExitDelayRoutine '{name}': reversing in-progress animation");
                StopCoroutine(animCoroutine);
                animCoroutine = StartCoroutine(AnimateHover(false));
                if (watchdogCoroutine != null) { StopCoroutine(watchdogCoroutine); watchdogCoroutine = null; }
                watchdogCoroutine = StartCoroutine(ExitWatchdog());
                yield break;
            }

            // if already lifted, animate back
            if (isLifted)
            {
                if (debugHover) Debug.Log($"CardView.ExitDelayRoutine '{name}': animating back (was lifted)");
                animCoroutine = StartCoroutine(AnimateHover(false));
                if (watchdogCoroutine != null) { StopCoroutine(watchdogCoroutine); watchdogCoroutine = null; }
                watchdogCoroutine = StartCoroutine(ExitWatchdog());
                yield break;
            }

            // otherwise snap back immediately
            ResetPositionImmediate();
        }

        private void StartHover()
        {
            // determine and freeze the effective hover offset we'll use for this hover
            if (useRelativeHoverOffset && rt != null)
            {
                // Compute a visual pixel height for the rect by using world corners
                // and converting to screen-space. Then compute the pixel offset as
                // a percent of that screen height and convert back to the local
                // anchored units we animate. This is robust to Canvas scaling and
                // parent transforms.
                float percent = Mathf.Clamp01(hoverOffsetPercent);
                try
                {
                    var canvas = rt.GetComponentInParent<Canvas>();
                    Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

                    Vector3[] worldCorners = new Vector3[4];
                    rt.GetWorldCorners(worldCorners);
                    // top = corners[1], bottom = corners[0]
                    Vector2 topScreen = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[1]);
                    Vector2 bottomScreen = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[0]);
                    float screenHeight = Mathf.Abs(topScreen.y - bottomScreen.y);
                    float pixelOffset = screenHeight * percent;

                    // convert a screen delta into a local delta in the parent's local space
                    RectTransform parentRect = rt.parent as RectTransform;
                    if (parentRect != null)
                    {
                        Vector2 centerWorld = RectTransformUtility.WorldToScreenPoint(cam, (worldCorners[0] + worldCorners[2]) * 0.5f);
                        Vector2 centerLocal;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, centerWorld, cam, out centerLocal);
                        Vector2 offsetLocal;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, centerWorld + new Vector2(0, pixelOffset), cam, out offsetLocal);
                        float localDelta = offsetLocal.y - centerLocal.y;
                        activeHoverOffset = localDelta;
                        if (debugHover)
                        {
                            Debug.Log($"CardView.StartHover '{name}': relative percent={percent} screenHeight={screenHeight} pxOffset={pixelOffset} -> localOffset={activeHoverOffset}");
                        }
                    }
                    else
                    {
                        // fallback: use rect.height in local units
                        activeHoverOffset = rt.rect.height * percent;
                        if (debugHover) Debug.Log($"CardView.StartHover '{name}': parentRect null, fallback activeHoverOffset={activeHoverOffset}");
                    }
                }
                catch (System.Exception ex)
                {
                    activeHoverOffset = rt.rect.height * percent;
                    if (debugHover) Debug.LogWarning($"CardView.StartHover '{name}': failed to compute relative offset precisely, fallback to rt.rect.height*percent ({ex.Message})");
                }
            }
            else
            {
                activeHoverOffset = Mathf.Clamp(hoverOffset, -kMaxHoverOffsetAbs, kMaxHoverOffsetAbs);
                if (Mathf.Abs(activeHoverOffset - hoverOffset) > 0.001f && debugHover)
                {
                    Debug.LogWarning($"CardView.StartHover '{name}': hoverOffset {hoverOffset} clamped to {activeHoverOffset} to avoid extreme movement");
                }
            }

            // Note: Do not change sibling or canvas here. We want the card to remain
            // at its original logical slot while it animates up. The bring-to-front
            // action will happen after the enter animation completes.
            if (debugHover && artworkImage != null)
            {
                Debug.Log($"CardView.StartHover '{name}': artworkIsChildOfRT={artworkImage.rectTransform.IsChildOf(rt)} elevated={elevated}");
            }
            // disable parent layout group so it doesn't fight our animated positions
            try
            {
                if (parentLayoutGroup == null)
                    parentLayoutGroup = rt != null ? rt.GetComponentInParent<UnityEngine.UI.LayoutGroup>() : null;
                if (parentLayoutGroup != null && parentLayoutGroup.enabled)
                {
                    parentLayoutGroupEnabledState = parentLayoutGroup.enabled;
                    parentLayoutGroup.enabled = false;
                }
            }
            catch { }
            // cancel any pending watchdog
            if (watchdogCoroutine != null) { StopCoroutine(watchdogCoroutine); watchdogCoroutine = null; }
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(AnimateHover(true));
        }

        private void ResetPositionImmediate()
        {
            if (animCoroutine != null) { StopCoroutine(animCoroutine); animCoroutine = null; }
            if (rt != null)
            {
                // Restore the root RectTransform position (but do NOT move the
                // visible child if we animated it) so the hit area stays stable.
                if (useLocalPosition)
                {
                    rt.localPosition = originalLocalPos;
                }
                else
                {
                    rt.anchoredPosition = originalAnchoredPos;
                }

                // restore visual transforms we may have animated
                try
                {
                    if (visualRoot != null)
                    {
                        visualRoot.localScale = originalVisualLocalScale;
                        visualRoot.localPosition = originalVisualLocalPos;
                        visualRoot.localRotation = originalVisualLocalRot;
                    }
                    else
                    {
                        rt.localScale = originalLocalScale;
                    }
                }
                catch { rt.localScale = originalLocalScale; }
            }
            // restore sibling
            try
            {
                var parent = transform.parent;
                if (parent != null && restoreSiblingOnExit)
                {
                    if (siblingAfterOnHover != null && siblingAfterOnHover.parent == parent)
                        transform.SetSiblingIndex(siblingAfterOnHover.GetSiblingIndex());
                    else
                        transform.SetSiblingIndex(Mathf.Clamp(originalSiblingIndex, 0, parent.childCount - 1));
                }
            }
            catch { }
            // ensure any layout group we disabled is re-enabled
            try
            {
                if (parentLayoutGroup != null)
                {
                    parentLayoutGroup.enabled = parentLayoutGroupEnabledState;
                    parentLayoutGroup = null;
                }
            }
            catch { }
            // If we're inside an ArcLayoutGroup, ask it to re-arrange so cards snap back to the curve.
            try
            {
                var arc = GetComponentInParent<ArcLayoutGroup>();
                if (arc != null)
                {
                    // animate the arc rearrange so cards lerp back to their original positions/rotations
                    float arrangeDur = Mathf.Max(0.06f, hoverDuration * 1.5f);
                    try { StartCoroutine(arc.AnimateArrange(arrangeDur)); }
                    catch { try { arc.Arrange(); } catch { } }
                }
            }
            catch { }
            if (debugHover) Debug.Log($"CardView.ResetPositionImmediate '{name}': reset to anchored={originalAnchoredPos} local={originalLocalPos}");
            isLifted = false;
            elevated = false;
        }

        private System.Collections.IEnumerator AnimateHover(bool entering)
        {
            // mark animation direction
            activeAnimationEntering = entering;
            if (rt == null) yield break;
            float elapsed = 0f;
            float duration = Mathf.Max(0.001f, hoverDuration);

                if (useLocalPosition)
                {
                    // If we have a separate visual root, animate that instead of the
                    // root RectTransform so the pointer hit area doesn't move.
                    if (visualRoot != null && visualRoot != rt)
                    {
                        Vector3 start = visualRoot.localPosition;
                        Vector3 target = entering ? originalVisualLocalPos + new Vector3(0, activeHoverOffset, 0) : originalVisualLocalPos;
                        // lock X to the start X so only Y changes during hover
                        target.x = start.x;
                        // lock X to the start position so we only move vertically while hovered
                        target.x = start.x;
                        if (debugHover) Debug.Log($"CardView.AnimateHover '{name}' visual start={start} target={target} entering={entering}");
                        Vector3 startScale = visualRoot.localScale;
                        Vector3 targetScale = entering ? originalVisualLocalScale * hoverScale : originalVisualLocalScale;
                        Quaternion startRot = visualRoot.localRotation;
                        Quaternion targetRot = entering ? Quaternion.identity : originalVisualLocalRot;

                        while (elapsed < duration)
                        {
                            elapsed += Time.unscaledDeltaTime;
                            float t = Mathf.Clamp01(elapsed / duration);
                            t = t * t * (3f - 2f * t);
                            visualRoot.localPosition = Vector3.Lerp(start, target, t);
                            visualRoot.localScale = Vector3.Lerp(startScale, targetScale, t);
                            visualRoot.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                            yield return null;
                        }

                        visualRoot.localPosition = target;
                        visualRoot.localScale = targetScale;
                        visualRoot.localRotation = targetRot;
                    }
                    else
                    {
                        Vector3 start = rt.localPosition;
                        // Use the captured baseline as the hover target so repeated enters don't accumulate
                        Vector3 target = entering ? originalLocalPos + new Vector3(0, activeHoverOffset, 0) : originalLocalPos;
                        // lock X to avoid unintended horizontal movement
                        target.x = start.x;
                        if (debugHover) Debug.Log($"CardView.AnimateHover '{name}' useLocal start={start} target={target} entering={entering}");
                        Vector3 startScale = rt.localScale;
                        Vector3 targetScale = entering ? originalLocalScale * hoverScale : originalLocalScale;
                        Quaternion startRot = rt.localRotation;
                        Quaternion targetRot = entering ? Quaternion.identity : originalVisualLocalRot;

                        while (elapsed < duration)
                        {
                            elapsed += Time.unscaledDeltaTime;
                            float t = Mathf.Clamp01(elapsed / duration);
                            t = t * t * (3f - 2f * t);
                            rt.localPosition = Vector3.Lerp(start, target, t);
                            rt.localScale = Vector3.Lerp(startScale, targetScale, t);
                            rt.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                            yield return null;
                        }

                        rt.localPosition = target;
                        rt.localScale = targetScale;
                        rt.localRotation = targetRot;
                    }
            }
                else
                {
                    // When anchoredPosition mode is used, prefer to animate the
                    // visual root if available so the root RT (hit area) stays put.
                    if (visualRoot != null && visualRoot != rt)
                    {
                        Vector3 start = visualRoot.localPosition;
                        Vector3 target = entering ? originalVisualLocalPos + new Vector3(0, activeHoverOffset, 0) : originalVisualLocalPos;
                        if (debugHover) Debug.Log($"CardView.AnimateHover '{name}' visual(start anchored mode) start={start} target={target} entering={entering}");
                        Vector3 startScale = visualRoot.localScale;
                        Vector3 targetScale = entering ? originalVisualLocalScale * hoverScale : originalVisualLocalScale;
                        Quaternion startRot = visualRoot.localRotation;
                        Quaternion targetRot = entering ? Quaternion.identity : originalVisualLocalRot;

                        while (elapsed < duration)
                        {
                            elapsed += Time.unscaledDeltaTime;
                            float t = Mathf.Clamp01(elapsed / duration);
                            t = t * t * (3f - 2f * t);
                            visualRoot.localPosition = Vector3.Lerp(start, target, t);
                            visualRoot.localScale = Vector3.Lerp(startScale, targetScale, t);
                            visualRoot.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                            yield return null;
                        }

                        visualRoot.localPosition = target;
                        visualRoot.localScale = targetScale;
                        visualRoot.localRotation = targetRot;
                    }
                    else
                    {
                        Vector2 start = rt.anchoredPosition;
                        // Use the captured baseline as the hover target so repeated enters don't accumulate
                        Vector2 target = entering ? originalAnchoredPos + new Vector2(0, activeHoverOffset) : originalAnchoredPos;
                        // lock X component so hover only affects Y
                        target.x = start.x;
                        if (debugHover) Debug.Log($"CardView.AnimateHover '{name}' anchored start={start} target={target} entering={entering}");
                        Vector3 startScale = rt.localScale;
                        Vector3 targetScale = entering ? originalLocalScale * hoverScale : originalLocalScale;
                        Quaternion startRot = rt.localRotation;
                        Quaternion targetRot = entering ? Quaternion.identity : originalVisualLocalRot;

                        while (elapsed < duration)
                        {
                            elapsed += Time.unscaledDeltaTime;
                            float t = Mathf.Clamp01(elapsed / duration);
                            t = t * t * (3f - 2f * t);
                            rt.anchoredPosition = Vector2.Lerp(start, target, t);
                            rt.localScale = Vector3.Lerp(startScale, targetScale, t);
                            rt.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                            yield return null;
                        }

                        rt.anchoredPosition = target;
                        rt.localScale = targetScale;
                        rt.localRotation = targetRot;
                    }
            }

            if (entering)
            {
                // mark lifted and bring to front now that animation completed
                isLifted = true;
                try { BringToFrontImmediate(); } catch { }
            }
            else
            {
                isLifted = false;
            }
            activeAnimationEntering = false;

            // on exit, restore sibling
            if (!entering)
            {
                try
                {
                    var parent = transform.parent;
                    if (parent != null && restoreSiblingOnExit)
                    {
                        if (siblingAfterOnHover != null && siblingAfterOnHover.parent == parent)
                            transform.SetSiblingIndex(siblingAfterOnHover.GetSiblingIndex());
                        else
                            transform.SetSiblingIndex(Mathf.Clamp(originalSiblingIndex, 0, parent.childCount - 1));
                    }
                }
                catch { }
                // re-enable parent layout group if we disabled it
                try
                {
                    if (parentLayoutGroup != null)
                    {
                        parentLayoutGroup.enabled = parentLayoutGroupEnabledState;
                        parentLayoutGroup = null;
                    }
                }
                catch { }
                // clear elevated flag: we've restored original hierarchy/layout
                elevated = false;
            }

            // After exit animation completes, ensure the arc reflows (animated) so children lerp
            if (!entering)
            {
                try
                {
                    var arc = GetComponentInParent<ArcLayoutGroup>();
                    if (arc != null)
                    {
                        float arrangeDur = Mathf.Max(0.06f, hoverDuration * 1.5f);
                        try { StartCoroutine(arc.AnimateArrange(arrangeDur)); }
                        catch { try { arc.Arrange(); } catch { } }
                    }
                }
                catch { }
            }

            // debug final state
            if (debugHover)
            {
                Debug.Log($"CardView.AnimateHover '{name}': finished entering={entering} anchored={rt.anchoredPosition} local={rt.localPosition} isLifted={isLifted}");
            }

            // cancel watchdog if exit completed normally
            if (watchdogCoroutine != null) { StopCoroutine(watchdogCoroutine); watchdogCoroutine = null; }

            // restore any Canvas override we applied for hover
            try
            {
                if (hoverCanvas != null)
                {
                    if (debugHover) Debug.Log($"CardView.RestoreHoverCanvas '{name}': hoverCanvasCreated={hoverCanvasCreated} hadOverride={hoverCanvasHadOverride} originalOrder={hoverCanvasOriginalOrder}");
                    if (hoverCanvasCreated)
                    {
                        Destroy(hoverCanvas);
                    }
                    else
                    {
                        hoverCanvas.overrideSorting = hoverCanvasHadOverride;
                        hoverCanvas.sortingOrder = hoverCanvasOriginalOrder;
                    }
                    hoverCanvas = null;
                    hoverCanvasCreated = false;
                }
            }
            catch { }

            animCoroutine = null;
        }

        private void BringToFrontImmediate()
        {
            if (elevated) return;
            if (debugHover) Debug.Log($"CardView.BringToFrontImmediate '{name}': useCanvas={useCanvasOverrideOnHover} hoverOrder={hoverSortingOrder}");
            if (useCanvasOverrideOnHover)
            {
                try
                {
                    hoverCanvas = GetComponent<Canvas>();
                    if (hoverCanvas != null)
                    {
                        hoverCanvasHadOverride = hoverCanvas.overrideSorting;
                        hoverCanvasOriginalOrder = hoverCanvas.sortingOrder;
                    }
                    else
                    {
                        hoverCanvas = gameObject.AddComponent<Canvas>();
                        hoverCanvasCreated = true;
                        hoverCanvasHadOverride = false;
                        hoverCanvasOriginalOrder = 0;
                    }
                    hoverCanvas.overrideSorting = true;
                    hoverCanvas.sortingOrder = hoverSortingOrder;
                }
                catch { }
            }
            else
            {
                try { transform.SetAsLastSibling(); } catch { }
            }
            elevated = true;
        }

        private System.Collections.IEnumerator DelayedArrange(ArcLayoutGroup arc)
        {
            // wait one frame to let layout/sibling updates settle, then re-arrange
            yield return new WaitForEndOfFrame();
            if (arc != null)
            {
                try { arc.Arrange(); } catch { }
            }
        }
    }
}
