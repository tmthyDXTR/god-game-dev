using UnityEngine;
using GodBeast;
using UI;

namespace Managers
{
    public class TurnManager : MonoBehaviour
    {
        public GodBeast.GodBeast godBeast;
        public DebugMenuUI debugMenuUI;
        public int turnCount = 0;
        public int sapWarningThreshold = 1;
        public bool isGameOver = false;

        private void Start()
        {
            if (godBeast == null)
                godBeast = FindFirstObjectByType<GodBeast.GodBeast>();
            if (debugMenuUI == null)
                debugMenuUI = FindFirstObjectByType<DebugMenuUI>();
        }

        public void EndTurn()
        {
            if (isGameOver) return;
            turnCount++;
            ConsumeSap();
            // Add other end turn logic here (e.g., Warden food, Bloom spread)
        }

        private void ConsumeSap()
        {
            if (godBeast == null) return;
            godBeast.ConsumeSap(1);
            if (debugMenuUI != null)
                debugMenuUI.UpdateSap(godBeast.sap);
            if (godBeast.sap <= sapWarningThreshold && debugMenuUI != null)
                debugMenuUI.ShowSapWarning(godBeast.sap);
            if (godBeast.sap <= 0)
            {
                isGameOver = true;
                if (debugMenuUI != null)
                    debugMenuUI.ShowGameOver("God-beast has died (Sap = 0)");
            }
        }
    }
}
