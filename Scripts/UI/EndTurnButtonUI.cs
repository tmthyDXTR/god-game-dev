using UnityEngine;
using UnityEngine.UI;
using Managers;

namespace UI
{
    public class EndTurnButtonUI : MonoBehaviour
    {
        public TurnManager turnManager;
        private Button endTurnButton;

        private void Start()
        {
            // Create button
            GameObject buttonObj = new GameObject("EndTurnButton");
            buttonObj.transform.SetParent(transform);
            endTurnButton = buttonObj.AddComponent<Button>();
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 0.2f, 1f);
            endTurnButton.targetGraphic = image;

            // Set button size and position (top right corner)
            var rect = endTurnButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(430, -200);
            rect.sizeDelta = new Vector2(120, 40);

            // Add button text
            GameObject textObj = new GameObject("ButtonText");
            textObj.transform.SetParent(buttonObj.transform);
            var text = textObj.AddComponent<Text>();
            text.text = "End Turn";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Find TurnManager if not assigned
            if (turnManager == null)
                turnManager = FindFirstObjectByType<TurnManager>();

            // Add click listener
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }

        private void OnEndTurnClicked()
        {
            if (turnManager != null)
                turnManager.EndTurn();
        }
    }
}
