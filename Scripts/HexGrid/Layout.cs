using System;
using System.Collections.Generic;

namespace HexGrid
{
    public struct Layout
    {
        public readonly Orientation Orientation;
        public readonly Point Size;
        public readonly Point Origin;

        public Layout(Orientation orientation, Point size, Point origin)
        {
            Orientation = orientation;
            Size = size;
            Origin = origin;
        }

        public Point HexToPixel(Hex h)
        {
            var M = Orientation;
            double x = (M.F0 * h.Q + M.F1 * h.R) * Size.X;
            double y = (M.F2 * h.Q + M.F3 * h.R) * Size.Y;
            return new Point(x + Origin.X, y + Origin.Y);
        }

        public FractionalHex PixelToHexFractional(Point p)
        {
            var M = Orientation;
            var pt = new Point((p.X - Origin.X) / Size.X, (p.Y - Origin.Y) / Size.Y);
            double q = M.B0 * pt.X + M.B1 * pt.Y;
            double r = M.B2 * pt.X + M.B3 * pt.Y;
            return new FractionalHex(q, r, -q - r);
        }

        public Hex PixelToHexRounded(Point p)
        {
            return PixelToHexFractional(p).Round();
        }

        public Point HexCornerOffset(int corner)
        {
            var size = Size;
            double angle = 2.0 * Math.PI * (Orientation.StartAngle + corner) / 6.0;
            return new Point(size.X * Math.Cos(angle), size.Y * Math.Sin(angle));
        }

        public List<Point> PolygonCorners(Hex h)
        {
            var corners = new List<Point>(6);
            var center = HexToPixel(h);
            for (int i = 0; i < 6; i++)
            {
                var offset = HexCornerOffset(i);
                corners.Add(new Point(center.X + offset.X, center.Y + offset.Y));
            }
            return corners;
        }
    }
}
