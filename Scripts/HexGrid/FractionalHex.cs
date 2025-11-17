using System;

namespace HexGrid
{
    public struct FractionalHex
    {
        public readonly double Q;
        public readonly double R;
        public readonly double S;

        public FractionalHex(double q, double r, double s)
        {
            Q = q;
            R = r;
            S = s;
        }

        public FractionalHex(double q, double r) : this(q, r, -q - r) { }

        public Hex Round()
        {
            int q = (int)Math.Round(Q);
            int r = (int)Math.Round(R);
            int s = (int)Math.Round(S);
            double q_diff = Math.Abs(q - Q);
            double r_diff = Math.Abs(r - R);
            double s_diff = Math.Abs(s - S);
            if (q_diff > r_diff && q_diff > s_diff)
                q = -r - s;
            else if (r_diff > s_diff)
                r = -q - s;
            else
                s = -q - r;
            return new Hex(q, r, s);
        }

        public static FractionalHex Lerp(Hex a, Hex b, double t)
        {
            double lerp(double x, double y, double t_) => x * (1 - t_) + y * t_;
            return new FractionalHex(
                lerp(a.Q, b.Q, t),
                lerp(a.R, b.R, t),
                lerp(a.S, b.S, t)
            );
        }
    }
}
