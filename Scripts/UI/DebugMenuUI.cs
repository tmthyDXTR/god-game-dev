using UnityEngine;
using UnityEngine.UI;
using HexGrid;

namespace UI
{
    public class DebugMenuUI : MonoBehaviour
    {
        public GodBeast.GodBeast godBeast;
        // Note: UI displays global resources from ResourceManager when available.

        private Canvas canvas;
        private Image foodIconImage;
        private Text foodCountText;
        private Text foodDemandText;
        private Text foodChangeText;
        private Image materialsIconImage;
        private Text materialsCountText;
        private Text materialsChangeText;
        private Text faithDemandText;
        private Image faithIconImage;
        private Text faithCountText;
        private Text faithChangeText;

        public Sprite foodIconSprite;
        public Sprite materialIconSprite;
        public Sprite faithIconSprite;

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

            GameObject foodDemandObj = new GameObject("FoodDemand");
            foodDemandObj.transform.SetParent(canvas.transform);
            foodDemandText = foodDemandObj.AddComponent<Text>();
            foodDemandText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foodDemandText.fontSize = 12;
            foodDemandText.alignment = TextAnchor.UpperLeft;
            foodDemandText.color = Color.cyan;
            foodDemandText.rectTransform.anchorMin = new Vector2(0, 1);
            foodDemandText.rectTransform.anchorMax = new Vector2(0, 1);
            foodDemandText.rectTransform.pivot = new Vector2(0, 1);
            foodDemandText.rectTransform.anchoredPosition = new Vector2(120, -40);

            GameObject foodChangeObj = new GameObject("FoodChange");
            foodChangeObj.transform.SetParent(canvas.transform);
            foodChangeText = foodChangeObj.AddComponent<Text>();
            foodChangeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foodChangeText.fontSize = 12;
            foodChangeText.alignment = TextAnchor.UpperLeft;
            foodChangeText.color = Color.white;
            foodChangeText.rectTransform.anchorMin = new Vector2(0, 1);
            foodChangeText.rectTransform.anchorMax = new Vector2(0, 1);
            foodChangeText.rectTransform.pivot = new Vector2(0, 1);
            foodChangeText.rectTransform.anchoredPosition = new Vector2(50, -60);

            // Materials icon and count
            GameObject materialsIconObj = new GameObject("MaterialsIcon");
            materialsIconObj.transform.SetParent(canvas.transform);
            materialsIconImage = materialsIconObj.AddComponent<Image>();
            materialsIconImage.sprite = materialIconSprite;
            materialsIconImage.rectTransform.anchorMin = new Vector2(0, 1);
            materialsIconImage.rectTransform.anchorMax = new Vector2(0, 1);
            materialsIconImage.rectTransform.pivot = new Vector2(0, 1);
            materialsIconImage.rectTransform.anchoredPosition = new Vector2(10, -80);
            materialsIconImage.rectTransform.sizeDelta = new Vector2(32, 32);

            GameObject materialsCountObj = new GameObject("MaterialsCount");
            materialsCountObj.transform.SetParent(canvas.transform);
            materialsCountText = materialsCountObj.AddComponent<Text>();
            materialsCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            materialsCountText.fontSize = 18;
            materialsCountText.alignment = TextAnchor.UpperLeft;
            materialsCountText.color = Color.white;
            materialsCountText.rectTransform.anchorMin = new Vector2(0, 1);
            materialsCountText.rectTransform.anchorMax = new Vector2(0, 1);
            materialsCountText.rectTransform.pivot = new Vector2(0, 1);
            materialsCountText.rectTransform.anchoredPosition = new Vector2(50, -80);

            GameObject materialsChangeObj = new GameObject("MaterialsChange");
            materialsChangeObj.transform.SetParent(canvas.transform);
            materialsChangeText = materialsChangeObj.AddComponent<Text>();
            materialsChangeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            materialsChangeText.fontSize = 12;
            materialsChangeText.alignment = TextAnchor.UpperLeft;
            materialsChangeText.color = Color.white;
            materialsChangeText.rectTransform.anchorMin = new Vector2(0, 1);
            materialsChangeText.rectTransform.anchorMax = new Vector2(0, 1);
            materialsChangeText.rectTransform.pivot = new Vector2(0, 1);
            materialsChangeText.rectTransform.anchoredPosition = new Vector2(50, -100);

            // Faith icon and count
            GameObject faithIconObj = new GameObject("FaithIcon");
            faithIconObj.transform.SetParent(canvas.transform);
            faithIconImage = faithIconObj.AddComponent<Image>();
            faithIconImage.sprite = faithIconSprite; // reuse second sprite by default
            faithIconImage.rectTransform.anchorMin = new Vector2(0, 1);
            faithIconImage.rectTransform.anchorMax = new Vector2(0, 1);
            faithIconImage.rectTransform.pivot = new Vector2(0, 1);
            faithIconImage.rectTransform.anchoredPosition = new Vector2(10, -120);
            faithIconImage.rectTransform.sizeDelta = new Vector2(32, 32);

            GameObject faithCountObj = new GameObject("FaithCount");
            faithCountObj.transform.SetParent(canvas.transform);
            faithCountText = faithCountObj.AddComponent<Text>();
            faithCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            faithCountText.fontSize = 18;
            faithCountText.alignment = TextAnchor.UpperLeft;
            faithCountText.color = Color.white;
            faithCountText.rectTransform.anchorMin = new Vector2(0, 1);
            faithCountText.rectTransform.anchorMax = new Vector2(0, 1);
            faithCountText.rectTransform.pivot = new Vector2(0, 1);
            faithCountText.rectTransform.anchoredPosition = new Vector2(50, -120);

            GameObject faithChangeObj = new GameObject("FaithChange");
            faithChangeObj.transform.SetParent(canvas.transform);
            faithChangeText = faithChangeObj.AddComponent<Text>();
            faithChangeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            faithChangeText.fontSize = 12;
            faithChangeText.alignment = TextAnchor.UpperLeft;
            faithChangeText.color = Color.white;
            faithChangeText.rectTransform.anchorMin = new Vector2(0, 1);
            faithChangeText.rectTransform.anchorMax = new Vector2(0, 1);
            faithChangeText.rectTransform.pivot = new Vector2(0, 1);
            faithChangeText.rectTransform.anchoredPosition = new Vector2(50, -140);

            // Faith demand display (fallback/demand for god-beast resource)
            GameObject faithDemandObj = new GameObject("FaithDemand");
            faithDemandObj.transform.SetParent(canvas.transform);
            faithDemandText = faithDemandObj.AddComponent<Text>();
            faithDemandText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            faithDemandText.fontSize = 12;
            faithDemandText.alignment = TextAnchor.UpperLeft;
            faithDemandText.color = Color.cyan;
            faithDemandText.rectTransform.anchorMin = new Vector2(0, 1);
            faithDemandText.rectTransform.anchorMax = new Vector2(0, 1);
            faithDemandText.rectTransform.pivot = new Vector2(0, 1);
            faithDemandText.rectTransform.anchoredPosition = new Vector2(120, -120);
        }

        // Predict the net change for the given resource at the next ResourceTick.
        // Currently we calculate Food = gathered_from_tiles - consumption_demand.
        // Materials and Faith have no automatic tick-based changes in the MVP, so return 0.
        private int PredictNextTickChange(Managers.ResourceManager.GameResource res)
        {
            if (res == Managers.ResourceManager.GameResource.Food)
            {
                var gen = FindFirstObjectByType<HexGridGenerator>();
                var rt = FindFirstObjectByType<Managers.ResourceTickManager>();
                if (gen == null || gen.tiles == null || rt == null) return 0;

                int totalGather = 0;
                int totalPopulation = 0;
                foreach (var t in gen.tiles.Values)
                {
                    if (t == null) continue;
                    int pop = Mathf.Max(0, t.populationCount);
                    // Always count population for demand, even if tile has no available food to gather.
                    totalPopulation += pop;
                    if (pop <= 0) continue;
                    int avail = t.GetResourceAmount(Managers.ResourceManager.GameResource.Food);
                    if (avail <= 0) continue;
                    int want = pop * rt.gatherRatePerPerson;
                    int taken = Mathf.Min(avail, want);
                    totalGather += taken;
                }

                float demandF = totalPopulation * rt.foodPerPersonPerTick;
                int demand = Mathf.CeilToInt(demandF);
                int predicted = totalGather - demand;
                return predicted;
            }

            // Auto-tick toggle and manual tick button (bottom-left)
            GameObject tickToggleObj = new GameObject("TickToggleButton");
            tickToggleObj.transform.SetParent(canvas.transform);
            var tickToggleBtn = tickToggleObj.AddComponent<UnityEngine.UI.Button>();
            var tickToggleImg = tickToggleObj.AddComponent<Image>();
            tickToggleImg.color = new Color(0.2f, 0.2f, 0.6f, 1f);
            var tickToggleRect = tickToggleBtn.GetComponent<RectTransform>();
            tickToggleRect.anchorMin = new Vector2(0, 0);
            tickToggleRect.anchorMax = new Vector2(0, 0);
            tickToggleRect.pivot = new Vector2(0, 0);
            tickToggleRect.anchoredPosition = new Vector2(20, 20);
            tickToggleRect.sizeDelta = new Vector2(140, 28);

            GameObject tickToggleTextObj = new GameObject("TickToggleText");
            tickToggleTextObj.transform.SetParent(tickToggleObj.transform);
            var tickToggleText = tickToggleTextObj.AddComponent<Text>();
            tickToggleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tickToggleText.alignment = TextAnchor.MiddleCenter;
            tickToggleText.color = Color.white;
            tickToggleText.text = "Auto Tick: ?";
            tickToggleText.rectTransform.anchorMin = Vector2.zero;
            tickToggleText.rectTransform.anchorMax = Vector2.one;
            tickToggleText.rectTransform.offsetMin = Vector2.zero;
            tickToggleText.rectTransform.offsetMax = Vector2.zero;

            tickToggleBtn.onClick.AddListener(() =>
            {
                var rt = FindFirstObjectByType<Managers.ResourceTickManager>();
                if (rt == null) return;
                if (rt.autoTick) rt.DisableAutoTick(); else rt.EnableAutoTick();
                // update label
                tickToggleText.text = rt.autoTick ? "Auto Tick: Seconds" : "Auto Tick: EndTurn";
            });

            // Manual Tick Now button
            GameObject tickNowObj = new GameObject("TickNowButton");
            tickNowObj.transform.SetParent(canvas.transform);
            var tickNowBtn = tickNowObj.AddComponent<UnityEngine.UI.Button>();
            var tickNowImg = tickNowObj.AddComponent<Image>();
            tickNowImg.color = new Color(0.6f, 0.2f, 0.2f, 1f);
            var tickNowRect = tickNowBtn.GetComponent<RectTransform>();
            tickNowRect.anchorMin = new Vector2(0, 0);
            tickNowRect.anchorMax = new Vector2(0, 0);
            tickNowRect.pivot = new Vector2(0, 0);
            tickNowRect.anchoredPosition = new Vector2(170, 20);
            tickNowRect.sizeDelta = new Vector2(100, 28);

            GameObject tickNowTextObj = new GameObject("TickNowText");
            tickNowTextObj.transform.SetParent(tickNowObj.transform);
            var tickNowText = tickNowTextObj.AddComponent<Text>();
            tickNowText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tickNowText.alignment = TextAnchor.MiddleCenter;
            tickNowText.color = Color.white;
            tickNowText.text = "Tick Now";
            tickNowText.rectTransform.anchorMin = Vector2.zero;
            tickNowText.rectTransform.anchorMax = Vector2.one;
            tickNowText.rectTransform.offsetMin = Vector2.zero;
            tickNowText.rectTransform.offsetMax = Vector2.zero;

            tickNowBtn.onClick.AddListener(() =>
            {
                var rt = FindFirstObjectByType<Managers.ResourceTickManager>();
                if (rt == null) return;
                rt.TriggerTick();
                // refresh UI after manual tick
                var rm = Managers.ResourceManager.Instance;
                if (rm != null)
                {
                    RefreshAllResources();
                }
            });

            // initialize toggle label based on existing ResourceTickManager
            var existingRt = FindFirstObjectByType<Managers.ResourceTickManager>();
            if (existingRt != null)
                tickToggleText.text = existingRt.autoTick ? "Auto Tick: Seconds" : "Auto Tick: EndTurn";
            else
                tickToggleText.text = "Auto Tick: ?";

            // Force Continue button for debugging: clears TurnManager.isGameOver
            GameObject forceContObj = new GameObject("ForceContinueButton");
            forceContObj.transform.SetParent(canvas.transform);
            var forceBtn = forceContObj.AddComponent<UnityEngine.UI.Button>();
            var forceImg = forceContObj.AddComponent<Image>();
            forceImg.color = new Color(0.2f, 0.5f, 0.6f, 1f);
            var forceRect = forceBtn.GetComponent<RectTransform>();
            forceRect.anchorMin = new Vector2(0, 0);
            forceRect.anchorMax = new Vector2(0, 0);
            forceRect.pivot = new Vector2(0, 0);
            forceRect.anchoredPosition = new Vector2(280, 20);
            forceRect.sizeDelta = new Vector2(120, 28);

            GameObject forceTextObj = new GameObject("ForceContinueText");
            forceTextObj.transform.SetParent(forceContObj.transform);
            var forceText = forceTextObj.AddComponent<Text>();
            forceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            forceText.alignment = TextAnchor.MiddleCenter;
            forceText.color = Color.white;
            forceText.text = "Force Continue";
            forceText.rectTransform.anchorMin = Vector2.zero;
            forceText.rectTransform.anchorMax = Vector2.one;
            forceText.rectTransform.offsetMin = Vector2.zero;
            forceText.rectTransform.offsetMax = Vector2.zero;

            forceBtn.onClick.AddListener(() =>
            {
                var tm = FindFirstObjectByType<Managers.TurnManager>();
                if (tm == null) return;
                tm.isGameOver = false;
                Debug.Log("DebugMenuUI: Forced TurnManager.isGameOver = false");
                // Refresh UI state if present
                RefreshAllResources();
            });
            // No automatic tick changes for other resources in this version
            return 0;
        }

        private void Start()
        {
            // Try to subscribe to ResourceManager changes (if present)
            var rm = Managers.ResourceManager.Instance;
            if (rm != null)
            {
                rm.OnResourceChanged += OnGlobalResourceChanged;
                // initialize display
                RefreshAllResources();
            }
            // initialize fallback last-values
            InitializeLastAmounts();
        }

        private System.Collections.Generic.Dictionary<Managers.ResourceManager.GameResource, int> lastAmounts = new System.Collections.Generic.Dictionary<Managers.ResourceManager.GameResource, int>();

        private void InitializeLastAmounts()
        {
            lastAmounts.Clear();
            var rm = Managers.ResourceManager.Instance;
            if (rm != null)
            {
                foreach (Managers.ResourceManager.GameResource g in System.Enum.GetValues(typeof(Managers.ResourceManager.GameResource)))
                {
                    lastAmounts[g] = rm.GetAmount(g);
                }
            }
            else
            {
                // fallback: read from godBeast inventory if present (sap)
                if (godBeast != null && godBeast.data != null && godBeast.data.perTurnResource != null)
                {
                    // we can't access the god-beast inventory type directly here without knowledge of ResourceItem,
                    // keep sap last amount unset (0) and rely on UpdateSap fallback to update display later
                }
            }
            // refresh demand display once
            RefreshDemandDisplay();
        }

        private void OnDestroy()
        {
            var rm = Managers.ResourceManager.Instance;
            if (rm != null)
                rm.OnResourceChanged -= OnGlobalResourceChanged;
        }

        private void Update()
        {
            // Fallback: if ResourceManager isn't present, keep showing god-beast sap if provided
            if (Managers.ResourceManager.Instance == null)
            {
                if (godBeast != null)
                {
                    // attempt to show god-beast sap (if GodBeastData defines perTurnResource)
                    // TurnManager also updates sap via UpdateSap; this keeps a live fallback.
                    // No-op here; UpdateSap will be called by TurnManager.
                }
            }
        }

        // Call this to animate resource collection (icon move and count up)
        public void AnimateResourceCollection(Vector3 worldStart, Managers.ResourceManager.GameResource type, int amount)
        {
            // Convert world position to screen position
            Vector3 screenStart = Camera.main.WorldToScreenPoint(worldStart);
            RectTransform targetRect = (type == Managers.ResourceManager.GameResource.Food) ? foodIconImage.rectTransform : faithIconImage.rectTransform;
            Vector3 targetScreen = targetRect.position;

            // Create icon to animate
            GameObject iconObj = new GameObject("CollectIcon");
            iconObj.transform.SetParent(canvas.transform);
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.sprite = (type == Managers.ResourceManager.GameResource.Food) ? foodIconSprite : faithIconSprite;
            iconImage.rectTransform.sizeDelta = new Vector2(32, 32);
            iconImage.rectTransform.position = screenStart;

            // Animate to target (simple lerp)
            StartCoroutine(MoveIconAndCountUp(iconImage.rectTransform, targetScreen, type, amount));
        }

        private System.Collections.IEnumerator MoveIconAndCountUp(RectTransform iconRect, Vector3 target, Managers.ResourceManager.GameResource type, int amount)
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
            // Animation complete. Resource amounts are managed by ResourceManager or GodBeast;
            // UI will be updated via ResourceManager.OnResourceChanged or TurnManager.UpdateSap.
            // Nothing to do here for the global counters.
        }

        private void OnGlobalResourceChanged(Managers.ResourceManager.GameResource res)
        {
            RefreshResource(res);
        }

        public void RefreshAllResources()
        {
            var rm = Managers.ResourceManager.Instance;
            if (rm == null) return;
            RefreshResource(Managers.ResourceManager.GameResource.Food);
            RefreshResource(Managers.ResourceManager.GameResource.Materials);
            RefreshResource(Managers.ResourceManager.GameResource.Faith);
        }

        private int ComputeFoodDemand()
        {
            int totalPopulation = 0;
            var gen = FindFirstObjectByType<HexGridGenerator>();
            if (gen == null || gen.tiles == null) return 0;
            foreach (var t in gen.tiles.Values)
            {
                if (t == null) continue;
                totalPopulation += Mathf.Max(0, t.populationCount);
            }
            var rt = FindFirstObjectByType<Managers.ResourceTickManager>();
            float perPerson = (rt != null) ? rt.foodPerPersonPerTick : 0.5f;
            int demand = Mathf.CeilToInt(totalPopulation * perPerson);
            return demand;
        }

        private int ComputeSapDemand()
        {
            if (godBeast != null && godBeast.data != null && godBeast.data.perTurnResource != null)
            {
                return godBeast.data.perTurnAmount;
            }
            return 0;
        }

        // Update only the demand display (doesn't touch current amounts)
        private void RefreshDemandDisplay()
        {
            if (foodDemandText != null)
                foodDemandText.text = $"demand: {ComputeFoodDemand()}";
            if (faithDemandText != null)
            {
                int sd = ComputeSapDemand();
                faithDemandText.text = sd > 0 ? $"/turn: {sd}" : "";
            }
        }

        private void RefreshResource(Managers.ResourceManager.GameResource res)
        {
            var rm = Managers.ResourceManager.Instance;
            if (rm == null) return;
            int predicted = PredictNextTickChange(res);
            switch (res)
            {
                case Managers.ResourceManager.GameResource.Food:
                    {
                        int newAmt = rm.GetAmount(res);
                        int last = lastAmounts.ContainsKey(res) ? lastAmounts[res] : 0;
                        foodCountText.text = newAmt.ToString();
                        // Show predicted next-tick change (gather - consumption)
                        foodChangeText.text = (predicted >= 0 ? "+" : "") + predicted.ToString();
                        foodChangeText.color = predicted > 0 ? Color.green : (predicted < 0 ? Color.red : Color.white);
                        lastAmounts[res] = newAmt;
                        // update demand
                        int demand = ComputeFoodDemand();
                        foodDemandText.text = $"demand: {demand}";
                    }
                    break;
                case Managers.ResourceManager.GameResource.Materials:
                    {
                        int newAmt = rm.GetAmount(res);
                        int last = lastAmounts.ContainsKey(res) ? lastAmounts[res] : 0;
                        materialsCountText.text = newAmt.ToString();
                        materialsChangeText.text = (predicted >= 0 ? "+" : "") + predicted.ToString();
                        materialsChangeText.color = predicted > 0 ? Color.green : (predicted < 0 ? Color.red : Color.white);
                        lastAmounts[res] = newAmt;
                    }
                    break;
                case Managers.ResourceManager.GameResource.Faith:
                    {
                        int newAmt = rm.GetAmount(res);
                        int last = lastAmounts.ContainsKey(res) ? lastAmounts[res] : 0;
                        faithCountText.text = newAmt.ToString();
                        faithChangeText.text = (predicted >= 0 ? "+" : "") + predicted.ToString();
                        faithChangeText.color = predicted > 0 ? Color.green : (predicted < 0 ? Color.red : Color.white);
                        lastAmounts[res] = newAmt;
                    }
                    break;
                
            }
        }

        // Backwards-compatible fallback: TurnManager calls UpdateSap(sap).
        // If ResourceManager isn't present, show that value in the faith display (so UI still updates).
        public void UpdateSap(int sap)
        {
            if (Managers.ResourceManager.Instance == null)
            {
                var key = Managers.ResourceManager.GameResource.Faith;
                int last = lastAmounts.ContainsKey(key) ? lastAmounts[key] : 0;
                int delta = sap - last;
                if (faithCountText != null) faithCountText.text = sap.ToString();
                if (faithChangeText != null)
                {
                    faithChangeText.text = (delta >= 0 ? "+" : "") + delta.ToString();
                    faithChangeText.color = delta > 0 ? Color.green : (delta < 0 ? Color.red : Color.white);
                }
                lastAmounts[key] = sap;
            }
        }

        public void ShowSapWarning(int sap)
        {
            // Simple warning: change sap text color to yellow if low
            if (faithCountText == null) return;
            if (sap <= 1)
                faithCountText.color = Color.yellow;
            else
                faithCountText.color = Color.white;
        }

        public void ShowGameOver(string message)
        {
            // Show game over message (could be a popup, here just change sap text to red)
            if (faithCountText == null) return;
            faithCountText.color = Color.red;
            faithCountText.text = message;
        }
    }
}
