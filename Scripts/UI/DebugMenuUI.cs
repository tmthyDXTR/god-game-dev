using UnityEngine;
using UnityEngine.UI;
using HexGrid;

namespace UI
{
    public class DebugMenuUI : MonoBehaviour
    {
        public GodBeast.GodBeast godBeast;
        public int inventoryFood = 0;
        public int inventorySap = 0;

        private Canvas canvas;
        private Image foodIconImage;
        private Text foodCountText;
        private Image sapIconImage;
        private Text sapCountText;

        public Sprite foodIconSprite;
        public Sprite sapIconSprite;

        private void Awake()
        {
            canvas = new GameObject("DebugCanvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.gameObject.AddComponent<GraphicRaycaster>();

            // Food icon and count
            GameObject foodIconObj = new GameObject("FoodIcon");
            foodIconObj.transform.SetParent(canvas.transform);
            foodIconImage = foodIconObj.AddComponent<Image>();
            foodIconImage.sprite = foodIconSprite;
            foodIconImage.rectTransform.anchorMin = new Vector2(0, 1);
            foodIconImage.rectTransform.anchorMax = new Vector2(0, 1);
            foodIconImage.rectTransform.pivot = new Vector2(0, 1);
            foodIconImage.rectTransform.anchoredPosition = new Vector2(10, -40);
            foodIconImage.rectTransform.sizeDelta = new Vector2(32, 32);

            GameObject foodCountObj = new GameObject("FoodCount");
            foodCountObj.transform.SetParent(canvas.transform);
            foodCountText = foodCountObj.AddComponent<Text>();
            foodCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foodCountText.fontSize = 18;
            foodCountText.alignment = TextAnchor.UpperLeft;
            foodCountText.color = Color.white;
            foodCountText.rectTransform.anchorMin = new Vector2(0, 1);
            foodCountText.rectTransform.anchorMax = new Vector2(0, 1);
            foodCountText.rectTransform.pivot = new Vector2(0, 1);
            foodCountText.rectTransform.anchoredPosition = new Vector2(50, -40);

            // Sap icon and count
            GameObject sapIconObj = new GameObject("SapIcon");
            sapIconObj.transform.SetParent(canvas.transform);
            sapIconImage = sapIconObj.AddComponent<Image>();
            sapIconImage.sprite = sapIconSprite;
            sapIconImage.rectTransform.anchorMin = new Vector2(0, 1);
            sapIconImage.rectTransform.anchorMax = new Vector2(0, 1);
            sapIconImage.rectTransform.pivot = new Vector2(0, 1);
            sapIconImage.rectTransform.anchoredPosition = new Vector2(10, -80);
            sapIconImage.rectTransform.sizeDelta = new Vector2(32, 32);

            GameObject sapCountObj = new GameObject("SapCount");
            sapCountObj.transform.SetParent(canvas.transform);
            sapCountText = sapCountObj.AddComponent<Text>();
            sapCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sapCountText.fontSize = 18;
            sapCountText.alignment = TextAnchor.UpperLeft;
            sapCountText.color = Color.white;
            sapCountText.rectTransform.anchorMin = new Vector2(0, 1);
            sapCountText.rectTransform.anchorMax = new Vector2(0, 1);
            sapCountText.rectTransform.pivot = new Vector2(0, 1);
            sapCountText.rectTransform.anchoredPosition = new Vector2(50, -80);
        }

        private void Update()
        {
            // Update inventory counts only
            foodCountText.text = inventoryFood.ToString();
            sapCountText.text = inventorySap.ToString();
        }

        // Call this to animate resource collection (icon move and count up)
        public void AnimateResourceCollection(Vector3 worldStart, ResourceType type, int amount)
        {
            // Convert world position to screen position
            Vector3 screenStart = Camera.main.WorldToScreenPoint(worldStart);
            RectTransform targetRect = (type == ResourceType.Food) ? foodIconImage.rectTransform : sapIconImage.rectTransform;
            Vector3 targetScreen = targetRect.position;

            // Create icon to animate
            GameObject iconObj = new GameObject("CollectIcon");
            iconObj.transform.SetParent(canvas.transform);
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.sprite = (type == ResourceType.Food) ? foodIconSprite : sapIconSprite;
            iconImage.rectTransform.sizeDelta = new Vector2(32, 32);
            iconImage.rectTransform.position = screenStart;

            // Animate to target (simple lerp)
            StartCoroutine(MoveIconAndCountUp(iconImage.rectTransform, targetScreen, type, amount));
        }

        private System.Collections.IEnumerator MoveIconAndCountUp(RectTransform iconRect, Vector3 target, ResourceType type, int amount)
        {
            float duration = 0.5f;
            float elapsed = 0f;
            Vector3 start = iconRect.position;
            while (elapsed < duration)
            {
                iconRect.position = Vector3.Lerp(start, target, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            iconRect.position = target;
            Destroy(iconRect.gameObject);
            // Count up
            if (type == ResourceType.Food)
                inventoryFood += amount;
            else if (type == ResourceType.Sap)
                inventorySap += amount;
        }

        public void UpdateSap(int sap)
        {
            inventorySap = sap;
            sapCountText.text = sap.ToString();
        }

        public void ShowSapWarning(int sap)
        {
            // Simple warning: change sap text color to yellow if low
            if (sap <= 1)
                sapCountText.color = Color.yellow;
            else
                sapCountText.color = Color.white;
        }

        public void ShowGameOver(string message)
        {
            // Show game over message (could be a popup, here just change sap text to red)
            sapCountText.color = Color.red;
            sapCountText.text = message;
        }
    }
}
