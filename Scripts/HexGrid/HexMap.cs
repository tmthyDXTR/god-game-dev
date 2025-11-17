using System.Collections.Generic;

namespace HexGrid
{
    public static class HexMap
    {
        // Parallelogram shape
        public static HashSet<Hex> Parallelogram(int q1, int q2, int r1, int r2)
        {
            var map = new HashSet<Hex>();
            for (int q = q1; q <= q2; q++)
                for (int r = r1; r <= r2; r++)
                    map.Add(new Hex(q, r, -q - r));
            return map;
        }

        // Triangle shape
        public static HashSet<Hex> Triangle(int size)
        {
            var map = new HashSet<Hex>();
            for (int q = 0; q <= size; q++)
                for (int r = 0; r <= size - q; r++)
                    map.Add(new Hex(q, r, -q - r));
            return map;
        }

        // Hexagon shape
        public static HashSet<Hex> Hexagon(int N)
        {
            var map = new HashSet<Hex>();
            for (int q = -N; q <= N; q++)
            {
                int r1 = System.Math.Max(-N, -q - N);
                int r2 = System.Math.Min(N, -q + N);
                for (int r = r1; r <= r2; r++)
                    map.Add(new Hex(q, r, -q - r));
            }
            return map;
        }

        // Rectangle shape (pointy top)
        public static HashSet<Hex> RectanglePointy(int left, int right, int top, int bottom)
        {
            var map = new HashSet<Hex>();
            for (int r = top; r <= bottom; r++)
            {
                int r_offset = r >> 1;
                for (int q = left - r_offset; q <= right - r_offset; q++)
                    map.Add(new Hex(q, r, -q - r));
            }
            return map;
        }

        // Rectangle shape (flat top)
        public static HashSet<Hex> RectangleFlat(int left, int right, int top, int bottom)
        {
            var map = new HashSet<Hex>();
            for (int q = left; q <= right; q++)
            {
                int q_offset = q >> 1;
                for (int r = top - q_offset; r <= bottom - q_offset; r++)
                    map.Add(new Hex(q, r, -q - r));
            }
            return map;
        }
    }
}
