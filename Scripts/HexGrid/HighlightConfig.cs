using UnityEngine;

namespace HexGrid
{
    // Centralized default highlight colors to avoid duplication across classes.
    public static class HighlightConfig
    {
        public static readonly Color Hover = new Color(1f, 1f, 0.5f, 1f); // light yellow, opaque
        public static readonly Color Select = new Color(1f, 1f, 0f, 0.7f); // yellow
        public static readonly Color Normal = Color.white;
    }
}
