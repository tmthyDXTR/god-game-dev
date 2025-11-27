using UnityEngine;
using UnityEngine.UI;

namespace Prototype.Cards
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class ArcLayoutGroup : MonoBehaviour
    {
        [Tooltip("Total arc angle in degrees (spread). 0 = straight line.")]
        public float arcAngle = 60f;
        [Tooltip("Radius in local units to place children along the arc")]
        public float radius = 200f;
        [Tooltip("Rotate children to align with the arc tangent")]
        public bool tiltChildren = true;
        [Tooltip("Multiplier for child tilt (1 = match arc angle, 0 = no tilt)")]
        public float tiltMultiplier = 0.6f;
        [Tooltip("If true, arrange children centered; otherwise start from left edge of arc")]
        public bool centered = true;
        [Tooltip("If true, automatically re-arrange in the editor when something changes")]
        public bool autoArrange = true;
        [Tooltip("If true, reorder the Transform sibling indices to match the logical slot order when arranging")]
        public bool reorderSiblings = true;

        private RectTransform rt;

        void OnEnable()
        {
            rt = GetComponent<RectTransform>();
            if (autoArrange) Arrange();
        }

        void Start()
        {
            // ensure arrange runs at runtime start as well
            if (Application.isPlaying && autoArrange)
                Arrange();
        }

        void OnValidate()
        {
            if (rt == null) rt = GetComponent<RectTransform>();
            if (autoArrange) Arrange();
        }

        void Update()
        {
            if (!Application.isPlaying && autoArrange) return; // avoid constant updates in edit mode
        }

        // Call this to recompute positions
        public void Arrange()
        {
            if (rt == null) rt = GetComponent<RectTransform>();
            int count = rt.childCount;
            if (count == 0) return;

            float totalAngle = Mathf.Max(0f, arcAngle);
            float startAngle = centered ? -totalAngle * 0.5f : 0f;
            float step = (count > 1) ? totalAngle / (count - 1) : 0f;

            // build an ordered list of children using CardView.slotIndex if present,
            // falling back to the current sibling order. This preserves logical slots
            // even if a child has been temporarily moved in the hierarchy.
            var children = new System.Collections.Generic.List<Transform>(count);
            for (int i = 0; i < count; ++i) children.Add(rt.GetChild(i));

            children.Sort((a, b) =>
            {
                var va = a.GetComponent<CardView>();
                var vb = b.GetComponent<CardView>();
                int ia = (va != null && va.slotIndex >= 0) ? va.slotIndex : a.GetSiblingIndex();
                int ib = (vb != null && vb.slotIndex >= 0) ? vb.slotIndex : b.GetSiblingIndex();
                return ia.CompareTo(ib);
            });

            for (int i = 0; i < children.Count; ++i)
            {
                Transform child = children[i];
                if (child == null) continue;

                float angleDeg = startAngle + step * i;
                float angleRad = Mathf.Deg2Rad * angleDeg;

                // compute position in local space: x = sin(angle) * radius, y = cos(angle) * radius
                float x = Mathf.Sin(angleRad) * radius;
                float y = Mathf.Cos(angleRad) * radius;

                RectTransform crt = child as RectTransform;
                // If the child is currently lifted (hovered), leave it alone so
                // hover animations are not overridden by layout reflow.
                var cv = child.GetComponent<CardView>();
                if (cv != null && cv.IsLifted)
                {
                    continue;
                }

                if (crt != null)
                {
                    crt.anchoredPosition = new Vector2(x, y);
                    if (tiltChildren)
                    {
                        float tilt = -angleDeg * tiltMultiplier;
                        crt.localRotation = Quaternion.Euler(0f, 0f, tilt);
                    }
                    else
                    {
                        crt.localRotation = Quaternion.identity;
                    }
                }
                else
                {
                    child.localPosition = new Vector3(x, y, child.localPosition.z);
                    if (tiltChildren) child.localRotation = Quaternion.Euler(0f, 0f, -angleDeg * tiltMultiplier);
                }
            }

            // optionally reorder sibling indices to match the logical ordering
            if (reorderSiblings)
            {
                for (int i = 0; i < children.Count; ++i)
                {
                    var child = children[i];
                    if (child == null) continue;
                    var cv = child.GetComponent<CardView>();
                    // avoid reordering a child that's currently lifted
                    if (cv != null && cv.IsLifted) continue;
                    try { child.SetSiblingIndex(i); } catch { }
                }
            }
        }

        // Animate children from their current transform to the positions/rotations
        // they would have after Arrange(). Useful for smooth reflows.
        public System.Collections.IEnumerator AnimateArrange(float duration)
        {
            if (rt == null) rt = GetComponent<RectTransform>();
            int count = rt.childCount;
            if (count == 0) yield break;

            float totalAngle = Mathf.Max(0f, arcAngle);
            float startAngle = centered ? -totalAngle * 0.5f : 0f;
            float step = (count > 1) ? totalAngle / (count - 1) : 0f;

            // build ordered list of children by slotIndex (fallback to current order)
            var children = new System.Collections.Generic.List<Transform>(count);
            for (int i = 0; i < count; ++i) children.Add(rt.GetChild(i));
            children.Sort((a, b) =>
            {
                var va = a.GetComponent<CardView>();
                var vb = b.GetComponent<CardView>();
                int ia = (va != null && va.slotIndex >= 0) ? va.slotIndex : a.GetSiblingIndex();
                int ib = (vb != null && vb.slotIndex >= 0) ? vb.slotIndex : b.GetSiblingIndex();
                return ia.CompareTo(ib);
            });

            // capture targets for ordered children
            Vector3[] targetPositions = new Vector3[count];
            Quaternion[] targetRotations = new Quaternion[count];
            for (int i = 0; i < children.Count; ++i)
            {
                float angleDeg = startAngle + step * i;
                float angleRad = Mathf.Deg2Rad * angleDeg;
                float x = Mathf.Sin(angleRad) * radius;
                float y = Mathf.Cos(angleRad) * radius;
                Transform child = children[i];
                RectTransform crt = child as RectTransform;
                var cv = child.GetComponent<CardView>();
                // If the child is lifted, keep its current transform as the
                // animation target so it won't be snapped back while hovered.
                if (cv != null && cv.IsLifted)
                {
                    if (crt != null)
                    {
                        targetPositions[i] = crt.localPosition;
                        targetRotations[i] = crt.localRotation;
                    }
                    else
                    {
                        targetPositions[i] = child.localPosition;
                        targetRotations[i] = child.localRotation;
                    }
                    continue;
                }

                if (crt != null)
                {
                    targetPositions[i] = new Vector3(x, y, crt.localPosition.z);
                    if (tiltChildren)
                    {
                        float tilt = -angleDeg * tiltMultiplier;
                        targetRotations[i] = Quaternion.Euler(0f, 0f, tilt);
                    }
                    else
                    {
                        targetRotations[i] = Quaternion.identity;
                    }
                }
                else
                {
                    targetPositions[i] = new Vector3(x, y, child.localPosition.z);
                    if (tiltChildren) targetRotations[i] = Quaternion.Euler(0f, 0f, -angleDeg * tiltMultiplier);
                    else targetRotations[i] = Quaternion.identity;
                }
            }

            // capture starts for ordered children
            Vector3[] startPositions = new Vector3[count];
            Quaternion[] startRotations = new Quaternion[count];
            for (int i = 0; i < children.Count; ++i)
            {
                Transform child = children[i];
                RectTransform crt = child as RectTransform;
                if (crt != null)
                {
                    startPositions[i] = crt.localPosition;
                    startRotations[i] = crt.localRotation;
                }
                else
                {
                    startPositions[i] = child.localPosition;
                    startRotations[i] = child.localRotation;
                }
            }

            float elapsed = 0f;
            float dur = Mathf.Max(0.0001f, duration);
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                // smoothstep-like ease
                t = t * t * (3f - 2f * t);
                for (int i = 0; i < children.Count; ++i)
                {
                    Transform child = children[i];
                    RectTransform crt = child as RectTransform;
                    Vector3 sp = startPositions[i];
                    Vector3 tp = targetPositions[i];
                    Quaternion sr = startRotations[i];
                    Quaternion tr = targetRotations[i];
                    if (crt != null)
                    {
                        crt.localPosition = Vector3.Lerp(sp, tp, t);
                        crt.localRotation = Quaternion.Slerp(sr, tr, t);
                    }
                    else
                    {
                        child.localPosition = Vector3.Lerp(sp, tp, t);
                        child.localRotation = Quaternion.Slerp(sr, tr, t);
                    }
                }
                yield return null;
            }

            // ensure final values
            for (int i = 0; i < children.Count; ++i)
            {
                Transform child = children[i];
                RectTransform crt = child as RectTransform;
                if (crt != null)
                {
                    crt.localPosition = targetPositions[i];
                    crt.localRotation = targetRotations[i];
                }
                else
                {
                    child.localPosition = targetPositions[i];
                    child.localRotation = targetRotations[i];
                }
            }

            // optionally reorder sibling indices to match the logical ordering after animation
            if (reorderSiblings)
            {
                for (int i = 0; i < children.Count; ++i)
                {
                    var child = children[i];
                    if (child == null) continue;
                    var cv = child.GetComponent<CardView>();
                    if (cv != null && cv.IsLifted) continue;
                    try { child.SetSiblingIndex(i); } catch { }
                }
            }
        }
    }
}
