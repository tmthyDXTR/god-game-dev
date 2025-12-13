using UnityEngine;

namespace HexGrid
{
    /// <summary>
    /// Lightweight settlement metadata attached to a tile.
    /// Kept intentionally minimal so cards or managers can extend behavior.
    /// </summary>
    public class Settlement : MonoBehaviour
    {
        public string settlementId;
        public string settlementOwner = "Player";
        public int settlementLevel = 1;
        public bool isMobile = true;
        public bool hasSettlement => !string.IsNullOrEmpty(settlementId);

        private void Reset()
        {
            settlementId = System.Guid.NewGuid().ToString();
        }
    }
}
