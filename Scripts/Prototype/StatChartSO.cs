using UnityEngine;

namespace Prototype
{
    [CreateAssetMenu(menuName = "Population/StatChart", fileName = "NewStatChart")]
    public class StatChartSO : ScriptableObject
    {
        [Header("Starting Traits")]
        public float[] startingTraitsAmountChart;
        public int[] startingTraitsAmountValues;

        [Header("Health")]
        public float[] healthChart;
        public int[] healthValues;

        [Header("Strength")]
        public float[] strengthChart;
        public int[] strengthValues;
    }
}
