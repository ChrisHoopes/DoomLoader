using UnityEngine;

[System.Serializable]
public struct Vec2I
{
    public int x;
    public int y;

    public Vec2I(int X, int Y)
    {
        x = X;
        y = Y;
    }

    public static Vec2I zero { get { return new Vec2I(0, 0); } }
    public static Vec2I one { get { return new Vec2I(1, 1); } }
    public static Vec2I north { get { return new Vec2I(0, 1); } }
    public static Vec2I south { get { return new Vec2I(0, -1); } }
    public static Vec2I east { get { return new Vec2I(1, 0); } }
    public static Vec2I west { get { return new Vec2I(-1, 0); } }
    public static Vec2I northeast { get { return new Vec2I(1, 1); } }
    public static Vec2I southeast { get { return new Vec2I(1, -1); } }
    public static Vec2I southwest { get { return new Vec2I(-1, -1); } }
    public static Vec2I northwest { get { return new Vec2I(-1, 1); } }
    public static Vec2I illegalMin { get { return new Vec2I(int.MinValue, int.MinValue); } }
    public static Vec2I illegalMax { get { return new Vec2I(int.MaxValue, int.MaxValue); } }

    //   Directions 
    //
    //     8  7  6
    //      \ | /
    //       \|/
    //    1---0---5
    //       /|\
    //      / | \
    //     2  3  4

    public static readonly Vec2I[] directions = new Vec2I[9]
    {
                zero,
                west,
                southwest,
                south,
                southeast,
                east,
                northeast,
                north,
                northwest
    };

    //    Neighbors 
    //
    //     7  6  5
    //      \ | /
    //       \|/
    //    0---X---4
    //       /|\
    //      / | \
    //     1  2  3

    public Vec2I[] neighbors
    {
        get
        {
            return new Vec2I[8]
            {
                   this + west,
                   this + southwest,
                   this + south,
                   this + southeast,
                   this + east,
                   this + northeast,
                   this + north,
                   this + northwest
                };
        }
    }

    public static explicit operator Vector2(Vec2I v)
    {
        return new Vector2(v.x, v.y);
    }

    public static Vec2I Floor(Vector2 v)
    {
        return new Vec2I(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));
    }

    public static Vec2I Round(Vector2 v)
    {
        return new Vec2I(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
    }

    public override string ToString()
    {
        return x.ToString() + ", " + y.ToString();
    }

    public override bool Equals(object obj)
    {
        return obj is Vec2I && this == (Vec2I)obj;
    }

    public override int GetHashCode()
    {
        return x.GetHashCode() ^ y.GetHashCode();
    }

    public static bool operator ==(Vec2I a, Vec2I b)
    {
        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(Vec2I a, Vec2I b)
    {
        return !(a == b);
    }

    public static Vec2I operator +(Vec2I a, Vec2I b)
    {
        return new Vec2I(a.x + b.x, a.y + b.y);
    }

    public static Vec2I operator -(Vec2I a, Vec2I b)
    {
        return new Vec2I(a.x - b.x, a.y - b.y);
    }

    public static Vec2I operator *(Vec2I a, Vec2I b)
    {
        return new Vec2I(a.x * b.x, a.y * b.y);
    }

    public static Vec2I operator /(Vec2I a, Vec2I b)
    {
        return new Vec2I(a.x / b.x, a.y / b.y);
    }

    public static Vec2I operator *(Vec2I a, int s)
    {
        return new Vec2I(a.x * s, a.y * s);
    }

    public static Vec2I operator /(Vec2I a, int s)
    {
        return new Vec2I(a.x / s, a.y / s);
    }

    public static Vec2I operator -(Vec2I t)
    {
        return new Vec2I(-t.x, -t.y);
    }

    public bool AnyLower(Vec2I compareTo) { return AnyLower(compareTo.x, compareTo.y); }
    public bool AnyLower(int X, int Y)
    {
        return (x < X || y < Y);
    }

    public bool AnyHigher(Vec2I compareTo) { return AnyHigher(compareTo.x, compareTo.y); }
    public bool AnyHigher(int X, int Y)
    {
        return (x > X || y > Y);
    }

    public bool AnyLowerOrEqual(Vec2I compareTo) { return AnyLowerOrEqual(compareTo.x, compareTo.y); }
    public bool AnyLowerOrEqual(int X, int Y)
    {
        return (x <= X || y <= Y);
    }

    public bool AnyHigherOrEqual(Vec2I compareTo) { return AnyHigherOrEqual(compareTo.x, compareTo.y); }
    public bool AnyHigherOrEqual(int X, int Y)
    {
        return (x >= X || y >= Y);
    }

    public bool AllLower(Vec2I compareTo) { return AllLower(compareTo.x, compareTo.y); }
    public bool AllLower(int X, int Y)
    {
        return (x < X && y < Y);
    }

    public bool AllHigher(Vec2I compareTo) { return AllHigher(compareTo.x, compareTo.y); }
    public bool AllHigher(int X, int Y)
    {
        return (x > X && y > Y);
    }

    public bool AllLowerOrEqual(Vec2I compareTo) { return AllLowerOrEqual(compareTo.x, compareTo.y); }
    public bool AllLowerOrEqual(int X, int Y)
    {
        return (x <= X && y <= Y);
    }

    public bool AllHigherOrEqual(Vec2I compareTo) { return AllHigherOrEqual(compareTo.x, compareTo.y); }
    public bool AllHigherOrEqual(int X, int Y)
    {
        return (x >= X && y >= Y);
    }

    public static Vec2I CreateMin(Vec2I a, Vec2I b)
    {
        return new Vec2I(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
    }

    public static Vec2I CreateMax(Vec2I a, Vec2I b)
    {
        return new Vec2I(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }
}