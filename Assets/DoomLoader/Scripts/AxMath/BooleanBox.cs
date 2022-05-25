/// <summary>
/// Data structure that holds an array of boolean variables
/// </summary>
[System.Serializable]
public class BooleanBox
{
    //boolean operators for compounding two boxes
    public enum Operation
    {
        AllFalse,
        AllTrue,
        And,
        Or,
        Xor,
        Nor,
        CloneA,
        CloneB
    }

    public Vec2I origo;
    public readonly Vec2I size;
    public Vec2I end { get { return origo + size - Vec2I.one; } }
    public bool[,] data;

    public BooleanBox() { }

    public BooleanBox(Vec2I Origo, Vec2I Size)
    {
        origo = Origo;
        size = Size;

        data = new bool[size.x, size.y];
    }

    public BooleanBox(Vec2I Origo, bool[,] Data)
    {
        origo = Origo;
        size = new Vec2I(Data.GetLength(0), Data.GetLength(1));
        data = Data;
    }

    //check against min and max corner
    public bool Contains(Vec2I position) { return Contains(position.x, position.y); }
    public bool Contains(int x, int y)
    {
        if (origo.AnyHigher(x, y))
            return false;

        if (end.AnyLower(x, y))
            return false;

        return true;
    }

    //try to set value inside grid
    public void SetValue(Vec2I position, bool value) { SetValue(position.x, position.y, value); }
    public void SetValue(int x, int y, bool value)
    {
        if (Contains(x, y))
            data[x - origo.x, y - origo.y] = value;
    }

    //try to get value from grid, returns false if out of grid
    public bool GetValue(Vec2I position) { return GetValue(position.x, position.y); }
    public bool GetValue(int x, int y)
    {
        if (Contains(x, y))
            return data[x - origo.x, y - origo.y];

        return false;
    }

    //creates a new boolean box from two, returns false if boxes don't intersect, uses boolean operator to decide the resulting data
    public static bool Intersection(BooleanBox a, BooleanBox b, out BooleanBox result, Operation operation)
    {
        result = null;

        //no intersection
        if (a.end.AnyLowerOrEqual(b.origo) || b.origo.AnyHigherOrEqual(a.end) || b.end.AnyLowerOrEqual(a.origo) || a.origo.AnyHigherOrEqual(b.end))
            return false;

        //check if other box encapsulates other or create a cross-section
        if (a.Contains(b.origo) && a.Contains(b.end))
            result = new BooleanBox(b.origo, b.size);
        else if (b.Contains(a.origo) && b.Contains(a.end))
            result = new BooleanBox(a.origo, a.size);
        else
        {
            Vec2I start = Vec2I.CreateMax(a.origo, b.origo);
            Vec2I end = Vec2I.CreateMin(a.end, b.end);
            result = new BooleanBox(start, end - start);
        }

        //offsets
        Vec2I ao = result.origo - a.origo;
        Vec2I bo = result.origo - b.origo;

        //boolean method
        switch (operation)
        {
            default:
            case Operation.AllFalse:
                break;

            case Operation.AllTrue:
                for (int y = 0; y < result.size.y; y++)
                    for (int x = 0; x < result.size.x; x++)
                        result.data[x, y] = true;
                break;

            case Operation.And:
                for (int y = 0; y < result.size.y; y++)
                    for (int x = 0; x < result.size.x; x++)
                        result.data[x, y] = a.data[ao.x + x, ao.y + y] && b.data[bo.x + x, bo.y + y];
                break;

            case Operation.Or:
                for (int y = 0; y < result.size.y; y++)
                    for (int x = 0; x < result.size.x; x++)
                        result.data[x, y] = a.data[ao.x + x, ao.y + y] || b.data[bo.x + x, bo.y + y];
                break;

            case Operation.Xor:
                for (int y = 0; y < result.size.y; y++)
                    for (int x = 0; x < result.size.x; x++)
                        result.data[x, y] = a.data[ao.x + x, ao.y + y] != b.data[bo.x + x, bo.y + y];
                break;

            case Operation.Nor:
                for (int y = 0; y < result.size.y; y++)
                    for (int x = 0; x < result.size.x; x++)
                        result.data[x, y] = !a.data[ao.x + x, ao.y + y] && !b.data[bo.x + x, bo.y + y];
                break;

            case Operation.CloneA:
                for (int y = 0; y < result.size.y; y++)
                    for (int x = 0; x < result.size.x; x++)
                        result.data[x, y] = a.data[ao.x + x, ao.y + y];
                break;

            case Operation.CloneB:
                for (int y = 0; y < result.size.y; y++)
                    for (int x = 0; x < result.size.x; x++)
                        result.data[x, y] = b.data[bo.x + x, bo.y + y];
                break;
        }

        return true;
    }

    //adds many boxes together with AND true data values 
    public static BooleanBox Combine(BooleanBox[] boxes)
    {
        if (boxes.Length == 0)
            return null;

        if (boxes.Length == 1)
            return boxes[0];

        Vec2I origo = boxes[0].origo;
        Vec2I end = boxes[0].end;

        for (int i = 1; i < boxes.Length; i++)
        {
            if (boxes[i] == null)
                continue;

            origo = Vec2I.CreateMin(origo, boxes[i].origo);
            end = Vec2I.CreateMax(end, boxes[i].end);
        }

        BooleanBox result = new BooleanBox(origo, end - origo + Vec2I.one);

		for (int i = 0; i < boxes.Length; i++)
			if (boxes[i] == null)
				continue;
			else
				for (int y = 0; y < result.size.y; y++)
					for (int x = 0; x < result.size.x; x++)
                        if (boxes[i].GetValue(origo + new Vec2I(x, y)))
							result.data[x, y] = true;

        return result;
    }
}