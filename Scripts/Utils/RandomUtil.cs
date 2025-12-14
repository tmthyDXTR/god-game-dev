using UnityEngine;

public static class RandomUtil
{
    // Pick an index from a probability chart. Chart may not sum to 1; negatives are clamped.
    public static int PickIndexFromChart(float[] chart)
    {
        if (chart == null || chart.Length == 0) return 0;
        float total = 0f;
        for (int i = 0; i < chart.Length; i++)
            if (chart[i] > 0f) total += chart[i];
        if (total <= 0f) return 0;
        float roll = Random.value; // [0,1)
        float cumulative = 0f;
        for (int i = 0; i < chart.Length; i++)
        {
            float p = Mathf.Max(0f, chart[i]) / total;
            cumulative += p;
            if (roll < cumulative)
                return i;
        }
        return chart.Length - 1;
    }
}
