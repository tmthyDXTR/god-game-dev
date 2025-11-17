using System;

namespace HexGrid
{
    public struct Hex : IEquatable<Hex>
    {
        public readonly int Q;
        public readonly int R;
        public readonly int S;

        // For serialization and external access
        public int q => Q;
        public int r => R;
        public int s => S;

        public Hex(int q, int r, int s)
        {
            if (q + r + s != 0)
                throw new ArgumentException("q + r + s must be 0");
            Q = q;
            R = r;
            S = s;
        }

        public Hex(int q, int r) : this(q, r, -q - r) { }

        public bool Equals(Hex other)
        {
            return Q == other.Q && R == other.R && S == other.S;
        }

        public override bool Equals(object obj)
        {
            return obj is Hex other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Q;
                hash = (hash * 397) ^ R;
                hash = (hash * 397) ^ S;
                return hash;
            }
        }

        public static bool operator ==(Hex a, Hex b) => a.Equals(b);
        public static bool operator !=(Hex a, Hex b) => !a.Equals(b);

        public static Hex operator +(Hex a, Hex b) => new Hex(a.Q + b.Q, a.R + b.R, a.S + b.S);
        public static Hex operator -(Hex a, Hex b) => new Hex(a.Q - b.Q, a.R - b.R, a.S - b.S);
        public static Hex operator *(Hex a, int k) => new Hex(a.Q * k, a.R * k, a.S * k);

        public int Length() => (Math.Abs(Q) + Math.Abs(R) + Math.Abs(S)) / 2;
        public int Distance(Hex b) => (this - b).Length();

        private static readonly Hex[] Directions =
        {
            new Hex(1, 0, -1), new Hex(1, -1, 0), new Hex(0, -1, 1),
            new Hex(-1, 0, 1), new Hex(-1, 1, 0), new Hex(0, 1, -1)
        };

        public static Hex Direction(int direction)
        {
            if (direction < 0 || direction > 5)
                throw new ArgumentOutOfRangeException(nameof(direction));
            return Directions[direction];
        }

        public Hex Neighbor(int direction)
        {
            return this + Direction(direction);
        }

        public Hex RotateLeft() => new Hex(-S, -Q, -R);
        public Hex RotateRight() => new Hex(-R, -S, -Q);
    }
}
