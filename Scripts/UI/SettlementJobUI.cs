using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HexGrid;
using TMPro;

/// <summary>
/// UI panel for assigning jobs from a settlement or agents.
/// Click on a settlement to open job queue panel.
/// Click on an agent to assign their job type.
/// </summary>
public class SettlementJobUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera used to render world (usually main camera).")]
    public Camera worldCamera;
    
    [Header("Settlement Panel")]
    [Tooltip("Panel GameObject for settlement job assignment (initially inactive).")]
    public GameObject jobPanel;
    [Tooltip("Text showing settlement name and info.")]
    public TextMeshProUGUI settlementInfoText;
    [Tooltip("Text showing current job queue.")]
    public TextMeshProUGUI jobQueueText;

    [Header("Settlement Job Buttons")]
    public Button gatherWoodButton;
    public Button gatherFoodButton;
    public Button haulButton;
    public Button closeButton;

    [Header("Agent Panel")]
    [Tooltip("Panel for assigning agent job types (initially inactive).")]
    public GameObject agentPanel;
    [Tooltip("Text showing agent info.")]
    public TextMeshProUGUI agentInfoText;
    [Tooltip("Button to assign agent as Gatherer.")]
    public Button assignGathererButton;
    [Tooltip("Button to assign agent as Hauler.")]
    public Button assignHaulerButton;
    [Tooltip("Close button for agent panel.")]
    public Button agentCloseButton;

    [Header("Raycast")]
    [Tooltip("Layer mask for tiles.")]
    public LayerMask tileLayerMask;

    // Currently selected
    Settlement selectedSettlement;
    PopulationAgent selectedAgent;

    void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        if (jobPanel != null) jobPanel.SetActive(false);
        if (agentPanel != null) agentPanel.SetActive(false);

        // Wire up settlement button listeners
        if (gatherWoodButton != null)
            gatherWoodButton.onClick.AddListener(OnGatherWoodClicked);
        if (gatherFoodButton != null)
            gatherFoodButton.onClick.AddListener(OnGatherFoodClicked);
        if (haulButton != null)
            haulButton.onClick.AddListener(OnHaulClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        // Wire up agent button listeners
        if (assignGathererButton != null)
            assignGathererButton.onClick.AddListener(OnAssignGathererClicked);
        if (assignHaulerButton != null)
            assignHaulerButton.onClick.AddListener(OnAssignHaulerClicked);
        if (agentCloseButton != null)
            agentCloseButton.onClick.AddListener(CloseAgentPanel);
    }

    void Update()
    {
        // Check for left mouse click (not over UI)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // If clicking on UI, ignore
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            TrySelect();
        }

        // Escape to close panels
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePanel();
            CloseAgentPanel();
        }

        // Update displays if panels are open
        if (jobPanel != null && jobPanel.activeSelf && selectedSettlement != null)
        {
            UpdateJobQueueDisplay();
        }
        if (agentPanel != null && agentPanel.activeSelf && selectedAgent != null)
        {
            UpdateAgentInfo();
        }
    }

    void TrySelect()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mousePos = new Vector3(mouseScreen.x, mouseScreen.y, worldCamera.nearClipPlane);
        Vector3 worldPoint = worldCamera.ScreenToWorldPoint(mousePos);
        Vector2 worldPoint2D = new Vector2(worldPoint.x, worldPoint.y);

        // First check for agents (they have priority)
        var agentHits = Physics2D.RaycastAll(worldPoint2D, Vector2.zero, 0f);
        foreach (var h in agentHits)
        {
            if (h.collider == null) continue;
            var agent = h.collider.GetComponentInParent<PopulationAgent>();
            if (agent != null)
            {
                OpenAgentPanel(agent);
                return;
            }
        }

        // Then check for settlements
        RaycastHit2D hit = Physics2D.Raycast(worldPoint2D, Vector2.zero, 0f, tileLayerMask);
        if (hit.collider != null)
        {
            var tile = hit.collider.GetComponent<HexTile>();
            if (tile != null)
            {
                var settlement = tile.GetComponent<Settlement>();
                if (settlement != null)
                {
                    OpenPanel(settlement);
                    return;
                }
            }
        }
    }

    void OpenPanel(Settlement settlement)
    {
        selectedSettlement = settlement;
        if (jobPanel != null)
        {
            jobPanel.SetActive(true);
            UpdateSettlementInfo();
            UpdateJobQueueDisplay();
        }
    }

    public void ClosePanel()
    {
        if (jobPanel != null)
            jobPanel.SetActive(false);
        selectedSettlement = null;
    }

    void UpdateSettlementInfo()
    {
        if (settlementInfoText == null || selectedSettlement == null) return;

        string info = $"<b>Settlement</b>\n";
        info += $"Owner: {selectedSettlement.settlementOwner}\n";
        info += $"Level: {selectedSettlement.settlementLevel}\n";
        info += $"Stored: ";
        
        bool first = true;
        foreach (var kvp in selectedSettlement.storedResources)
        {
            if (!first) info += ", ";
            info += $"{kvp.Key}: {kvp.Value}";
            first = false;
        }
        if (first) info += "None";

        settlementInfoText.text = info;
    }

    void UpdateJobQueueDisplay()
    {
        if (jobQueueText == null || selectedSettlement == null) return;

        string queueInfo = $"<b>Job Queue ({selectedSettlement.QueuedJobCount})</b>\n";
        
        // Note: We can only peek, not iterate the queue without modifying it
        // For display purposes, just show the count and next job type
        var nextJob = selectedSettlement.PeekNextJob();
        if (nextJob != null)
        {
            queueInfo += $"Next: {nextJob.type} ({nextJob.resource})";
        }
        else
        {
            queueInfo += "Empty";
        }

        jobQueueText.text = queueInfo;
    }

    void OnGatherWoodClicked()
    {
        if (selectedSettlement == null) return;

        var job = new Job
        {
            type = JobType.Gather,
            resource = Managers.ResourceManager.GameResource.Materials,
            amount = 1,
            priority = 0
        };
        selectedSettlement.EnqueueJob(job);
        Debug.Log($"SettlementJobUI: Queued Gather Wood job at {selectedSettlement.name}");
        UpdateJobQueueDisplay();
    }

    void OnGatherFoodClicked()
    {
        if (selectedSettlement == null) return;

        var job = new Job
        {
            type = JobType.Gather,
            resource = Managers.ResourceManager.GameResource.Food,
            amount = 1,
            priority = 0
        };
        selectedSettlement.EnqueueJob(job);
        Debug.Log($"SettlementJobUI: Queued Gather Food job at {selectedSettlement.name}");
        UpdateJobQueueDisplay();
    }

    void OnHaulClicked()
    {
        if (selectedSettlement == null) return;

        var job = new Job
        {
            type = JobType.Haul,
            resource = Managers.ResourceManager.GameResource.Materials,
            amount = 1,
            priority = 1
        };
        selectedSettlement.EnqueueJob(job);
        Debug.Log($"SettlementJobUI: Queued Haul job at {selectedSettlement.name}");
        UpdateJobQueueDisplay();
    }

    // ========== Agent Panel ==========

    void OpenAgentPanel(PopulationAgent agent)
    {
        selectedAgent = agent;
        ClosePanel(); // Close settlement panel if open
        
        if (agentPanel != null)
        {
            agentPanel.SetActive(true);
            UpdateAgentInfo();
        }
    }

    public void CloseAgentPanel()
    {
        if (agentPanel != null)
            agentPanel.SetActive(false);
        selectedAgent = null;
    }

    void UpdateAgentInfo()
    {
        if (agentInfoText == null || selectedAgent == null) return;

        string info = $"<b>Agent: {selectedAgent.name}</b>\n";
        info += $"Assigned Job: <color=yellow>{selectedAgent.assignedJobType}</color>\n";
        info += $"Resource: {selectedAgent.assignedResource}\n";
        info += $"State: {selectedAgent.agentState}\n";
        info += $"Auto-Repeat: {(selectedAgent.autoRepeatJob ? "Yes" : "No")}";

        agentInfoText.text = info;
    }

    void OnAssignGathererClicked()
    {
        if (selectedAgent == null) return;

        selectedAgent.assignedJobType = JobType.Gather;
        selectedAgent.assignedResource = Managers.ResourceManager.GameResource.Materials;
        Debug.Log($"SettlementJobUI: Assigned {selectedAgent.name} as Gatherer (Materials)");
        
        // If agent is idle, start their new job immediately
        if (selectedAgent.agentState == PopulationAgent.AgentState.Idle && selectedAgent.autoRepeatJob)
        {
            selectedAgent.TryStartAssignedJob();
        }
        
        UpdateAgentInfo();
    }

    void OnAssignHaulerClicked()
    {
        if (selectedAgent == null) return;

        selectedAgent.assignedJobType = JobType.Haul;
        selectedAgent.assignedResource = Managers.ResourceManager.GameResource.Materials;
        Debug.Log($"SettlementJobUI: Assigned {selectedAgent.name} as Hauler (Materials)");
        
        // If agent is idle, start their new job immediately
        if (selectedAgent.agentState == PopulationAgent.AgentState.Idle && selectedAgent.autoRepeatJob)
        {
            selectedAgent.TryStartAssignedJob();
        }
        
        UpdateAgentInfo();
    }
}
