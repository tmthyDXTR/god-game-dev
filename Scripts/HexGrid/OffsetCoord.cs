using System;

namespace HexGrid
{
    public struct OffsetCoord
    {
        public readonly int Col;
        public readonly int Row;

        public OffsetCoord(int col, int row)
        {
            Col = col;
            Row = row;
        }

        public const int EVEN = +1;
        public const int ODD = -1;

        // For flat top hexes (q offset)
        public static OffsetCoord QOffsetFromCube(int offset, Hex h)
        {
            if (offset != EVEN && offset != ODD)
                throw new ArgumentException("Offset must be EVEN or ODD");
            int col = h.Q;
            int row = h.R + ((h.Q + offset * (h.Q & 1)) / 2);
            return new OffsetCoord(col, row);
        }

        public static Hex QOffsetToCube(int offset, OffsetCoord h)
        {
            if (offset != EVEN && offset != ODD)
                throw new ArgumentException("Offset must be EVEN or ODD");
            int q = h.Col;
            int r = h.Row - ((h.Col + offset * (h.Col & 1)) / 2);
            int s = -q - r;
            return new Hex(q, r, s);
        }

        // For pointy top hexes (r offset)
        public static OffsetCoord ROffsetFromCube(int offset, Hex h)
        {
            if (offset != EVEN && offset != ODD)
                throw new ArgumentException("Offset must be EVEN or ODD");
            int col = h.Q + ((h.R + offset * (h.R & 1)) / 2);
            int row = h.R;
            return new OffsetCoord(col, row);
        }

        public static Hex ROffsetToCube(int offset, OffsetCoord h)
        {
            if (offset != EVEN && offset != ODD)
                throw new ArgumentException("Offset must be EVEN or ODD");
            int q = h.Col - ((h.Row + offset * (h.Row & 1)) / 2);
            int r = h.Row;
            int s = -q - r;
            return new Hex(q, r, s);
        }
    }
}
