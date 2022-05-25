using UnityEngine;
using System.Collections.Generic;

public static class AxMath
{
    public const float TAU = Mathf.PI * 2;
    public const float HalfPI = Mathf.PI / 2;
    public const float ThirdOfPI = Mathf.PI / 3;
    public const float SquareOfTwo = 1.41421356237f;
    public const float SquareOfThree = 1.73205080757f;
    public const float Diagonal = SquareOfTwo / 2;

    //takes the four bytes from a 32-bit integer
    public static byte[] Int2bytes(int composite)
    {
        return new byte[4]
        {
            (byte)(composite),
            (byte)(composite >> 8),
            (byte)(composite >> 16),
            (byte)(composite >> 24)
        };
    }

    //creates an integer from bytes, supports anything from 0-4 bytes, but no more
    public static int Bytes2int(byte[] bytes)
    {
        int pos = 0;
        int result = 0;
        int count = Mathf.Min(bytes.Length, 4);

        for (int i = 0; i < count; i++)
        {
            result |= ((int)bytes[i]) << pos;
            pos += 8;
        }

        return result;
    }

    //returns zero when x == y
    public static float SafeAtan2(float y, float x)
    {
        float value;

        if (x == 0)
        {
            if (y == 0) return 0;
            if (y > 0) return HalfPI;
            return -HalfPI;
        }
        if (y == 0)
        {
            if (x > 0) return 0;
            return Mathf.PI;
        }

        if (Mathf.Abs(x) >= Mathf.Abs(y))
        {
            value = Mathf.Atan(y / x);
            if (x < 0.0)
                if (y >= 0.0)
                    value += Mathf.PI;
                else
                    value -= Mathf.PI;
            return value;
        }

        value = -Mathf.Atan(x / y);

        if (y < 0) value -= HalfPI;
        else value += HalfPI;

        return value;
    }

    //returns x-positive when length == 0
    public static void SafeNormalize(ref float x, ref float y)
    {
        if (x == 0 && y == 0)
        {
            x = 1;
            return;
        }

        float distance = Mathf.Sqrt(x * x + y * y);
        x = x / distance;
        y = y / distance;
    }

    //returns Vec2I direction between two points
    public static int Atan2Direction(Vec2I origo, Vec2I target)
    {
        if (origo == target) return 0;

        return (int)(((SafeAtan2(target.y - origo.y, target.x - origo.x) + Mathf.PI) / TAU * 8) % 8 + 1);
    }

    //returns best step direction towards target
    public static int RogueDirection(Vec2I origo, Vec2I target)
    {
        if (origo.x == target.x && origo.y == target.y)
            return 0;

        Vec2I d = target - origo;
        int dx = Mathf.Abs(d.x);
        int dy = Mathf.Abs(d.y);

        return (RogueDistance(origo, target) / 2 >= Mathf.Abs(dx - dy) ?
            (d.x > 0 ? (d.y > 0 ? 6 : 4) : (d.y > 0 ? 8 : 2)) :
            (dx > dy ? (d.x > 0 ? 5 : 1) : (d.y > 0 ? 7 : 3)));
    }

    //clamps value to be equal or between min and max
    public static int Clamp(int value, int min, int max)
    {
        return (value < min) ? min : (value > max) ? max : value;
    }

    //clamps value to be equal or between min and max
    public static void Clamp(ref int value, int min, int max)
    {
        value = (value < min) ? min : (value > max) ? max : value;
    }

    //how many eight-directional movement steps to target
    public static int RogueDistance(Vec2I a, Vec2I b) { return RogueDistance(a.x, a.y, b.x, b.y); }
    public static int RogueDistance(int x0, int y0, int x1, int y1)
    {
        return Mathf.Max(Mathf.Abs(x0 - x1), Mathf.Abs(y0 - y1));
    }

    //how many cardinal movement steps to target
    public static int ManhattanDistance(Vec2I a, Vec2I b) { return ManhattanDistance(a.x, a.y, b.x, b.y); }
    public static int ManhattanDistance(int x0, int y0, int x1, int y1)
    {
        return Mathf.Abs(x0 - x1) + Mathf.Abs(y0 - y1);
    }

    //true euclidean distance
    public static int TrueDistance(Vec2I a, Vec2I b) { return TrueDistance(a.x, a.y, b.x, b.y); }
    public static int TrueDistance(int x0, int y0, int x1, int y1)
    {
        int x = Mathf.Abs(x0 - x1);
        int y = Mathf.Abs(y0 - y1);

        return (int)Mathf.Sqrt(x * x + y * y);
    }

    //pseudo-euclidean distance
    public static int WeightedDistance(Vec2I a, Vec2I b) { return WeightedDistance(a.x, a.y, b.x, b.y); }
    public static int WeightedDistance(int x1, int y1, int x2, int y2)
    {
        int distX = (Mathf.Abs(x1 - x2));
        int distY = (Mathf.Abs(y1 - y2));

        if (distX > distY)
            return 14 * distY + 10 * (distX - distY);
        else
            return 14 * distX + 10 * (distY - distX);
    }

    //swaps two values
    private static void Swap(ref int x, ref int y)
    {
        x = x + y;
        y = x - y;
        x = x - y;
    }

    //show eight-directional steps to target
    public static SingleLinkedList<Vec2I> RogueLine(Vec2I origo, Vec2I target)
    {
        SingleLinkedList<Vec2I> list = new SingleLinkedList<Vec2I>();

        Vec2I pos = origo;
        list.InsertBack(pos);
        while (pos != target)
        {
            pos += Vec2I.directions[RogueDirection(pos, target)];
            list.InsertBack(pos);
        }

        return list;
    }

    //bresenham line in integer grid
    public static Vec2I[] CartesianLine(Vec2I origo, Vec2I target) { return CartesianLine(origo.x, origo.y, target.x, target.y); }
    public static Vec2I[] CartesianLine(int x0, int y0, int x1, int y1)
    {
        bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);
        if (steep)
        {
            Swap(ref x0, ref y0);
            Swap(ref x1, ref y1);
        }

        bool flip = false;
        if (x0 > x1)
        {
            Swap(ref x0, ref x1);
            Swap(ref y0, ref y1);
            flip = true;
        }

        int dx = x1 - x0;
        int dy = Mathf.Abs(y1 - y0);
        int error = dx / 2;
        int ystep = (y0 < y1) ? 1 : -1;
        int y = y0;

        Vec2I[] array = new Vec2I[x1 - x0 + 1];

        if (flip)
        {
            int i = array.Length - 1;
            for (int x = x0; x <= x1; x++)
            {
                array[i--] = steep ? new Vec2I(y, x) : new Vec2I(x, y);

                error = error - dy;
                if (error < 0)
                {
                    y += ystep;
                    error += dx;
                }
            }
        }
        else
        {
            int i = 0;
            for (int x = x0; x <= x1; x++)
            {
                array[i++] = steep ? new Vec2I(y, x) : new Vec2I(x, y);

                error = error - dy;
                if (error < 0)
                {
                    y += ystep;
                    error += dx;
                }
            }
        }

        return array;
    }

    //bresenham line with floating pointers
    public static SingleLinkedList<Vec2I> CartesianLineF(Vector2 start, Vector2 end) { return CartesianLineF(start.x, start.y, end.x, end.y); }
    public static SingleLinkedList<Vec2I> CartesianLineF(float x0, float y0, float x1, float y1)
    {
        SingleLinkedList<Vec2I> list = new SingleLinkedList<Vec2I>();

        float vx = x1 - x0;
        float vy = y1 - y0;

        float dx = Mathf.Sqrt(1 + Mathf.Pow(vy / vx, 2));
        float dy = Mathf.Sqrt(1 + Mathf.Pow(vx / vy, 2));

        float ix = Mathf.Floor(x0);
        float iy = Mathf.Floor(y0);

        float sx, ex;
        if (vx < 0)
        {
            sx = -1;
            ex = (x0 - ix) * dx;
        }
        else
        {
            sx = 1;
            ex = (ix + 1 - x0) * dx;
        }

        float sy, ey;
        if (vy < 0)
        {
            sy = -1;
            ey = (y0 - iy) * dy;
        }
        else
        {
            sy = 1;
            ey = (iy + 1 - y0) * dy;
        }

        float len = Mathf.Sqrt(vx * vx + vy * vy);

        while (Mathf.Min(ex, ey) <= len)
        {
            list.InsertBack(new Vec2I(Mathf.RoundToInt(ix), Mathf.RoundToInt(iy)));
            if (ex < ey)
            {
                ex += dx;
                ix += sx;
            }
            else
            {
                ey += dy;
                iy += sy;
            }
        }

        list.InsertBack(new Vec2I(Mathf.RoundToInt(ix), Mathf.RoundToInt(iy)));

        return list;
    }

    //pseudo-euclidean circle
    public static SingleLinkedList<Vec2I> WeightedCircleFilled(int cx, int cy, int r) { return WeightedCircleFilled(new Vec2I(cx, cy), r); }
    public static SingleLinkedList<Vec2I> WeightedCircleFilled(Vec2I center, int radius)
    {
        SingleLinkedList<Vec2I> list = new SingleLinkedList<Vec2I>();

        for (int y = center.y - radius; y <= center.y + radius; y++)
            for (int x = center.x - radius; x <= center.x + radius; x++)
                if (WeightedDistance(x, y, center.x, center.y) <= radius)
                    list.InsertFront(new Vec2I(x, y));

        return list;
    }

    //true euclidean circle
    public static SingleLinkedList<Vec2I> CartesianCircleFilled(Vec2I center, int radius) { return CartesianCircleFilled(center.x, center.y, radius); }
    public static SingleLinkedList<Vec2I> CartesianCircleFilled(int cx, int cy, int r)
    {
        SingleLinkedList<Vec2I> list = new SingleLinkedList<Vec2I>();

        for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) < r * r)
                    list.InsertFront(new Vec2I(x, y));

        return list;
    }

    //aliased euclidean circle perimeter
    public static SingleLinkedList<Vec2I> CartesianCircleEdge(Vec2I center, int radius) { return CartesianCircleEdge(center.x, center.y, radius); }
    public static SingleLinkedList<Vec2I> CartesianCircleEdge(int cx, int cy, int r)
    {
        SingleLinkedList<Vec2I> list = new SingleLinkedList<Vec2I>();

        int x = r - 1;
        int y = 0;
        int dx = 1;
        int dy = 1;
        int err = dx - (r << 1);

        while (x >= y)
        {
            list.InsertFront(new Vec2I(cx + x, cy + y));
            list.InsertFront(new Vec2I(cx + y, cy + x));
            list.InsertFront(new Vec2I(cx - y, cy + x));
            list.InsertFront(new Vec2I(cx - x, cy + y));
            list.InsertFront(new Vec2I(cx - x, cy - y));
            list.InsertFront(new Vec2I(cx - y, cy - x));
            list.InsertFront(new Vec2I(cx + y, cy - x));
            list.InsertFront(new Vec2I(cx + x, cy - y));

            if (err <= 0)
            {
                y++;
                err += dy;
                dy += 2;
            }
            if (err > 0)
            {
                x--;
                dx += 2;
                err += (-r << 1) + dx;
            }
        }

        return list;
    }

    //standard lerp
    public static float Lerp(float a, float b, float s) { return (a + s * (b - a)); }
    public static int Lerp(int a, int b, float s) { return Mathf.RoundToInt((a + s * (b - a))); }

    //reflect vector over normal
    public static Vector2 Reflect(Vector2 vector, Vector2 normal)
    { return vector - 2 * Vector2.Dot(vector, normal) * normal; }

    //check if two lines intersect
    public static bool LinesIntercect(Line2 a, Line2 b)
    {
        //component lengths
        float ALX = a.end.x - a.start.x;
        float ALY = a.end.y - a.start.y;
        float BLX = b.end.x - b.start.x;
        float BLY = b.end.y - b.start.y;

        //separations
        float SXS = a.start.x - b.start.x;
        float SYS = a.start.y - b.start.y;

        //projection
        float d = BLY * ALX - BLX * ALY;
        float u = BLX * SYS - BLY * SXS;
        float v = ALX * SYS - ALY * SXS;

        //parallel lines can't intersect
        if (d == 0) return false;

        //flip signs
        if (d < 0) { d = -d; u = -u; v = -v; }

        //check order
        return (0 <= u && u <= d) && (0 <= v && v <= d);
    }

    //find closest point on line to a position
    public static Vector2 ClosestPointOnLine(Line2 line, Vector2 position)
    {
        //avoid division by zero
        if (line.start == line.end)
            return line.start;

        Vector2 lineVector = line.end - line.start;

        float t = Vector2.Dot(lineVector, position - line.start) / lineVector.sqrMagnitude;

        if (t < 0) return line.start;
        if (t > 1) return line.end;

        return line.start + (t * lineVector);
    }

    //get the point where two lines will meet, returns false if lines are parallel
    public static bool LinesConvergencePoint(Line2 a, Line2 b, out Vector2 result)
    {
        //transform
        float ALY = a.end.y - a.start.y;
        float ANX = a.start.x - a.end.x;
        float ACP = ALY * a.start.x + ANX * a.start.y;
        float BLY = b.end.y - b.start.y;
        float BNX = b.start.x - b.end.x;
        float BCP = BLY * b.start.x + BNX * b.start.y;

        //calculate slope
        float delta = ALY * BNX - BLY * ANX;

        //lines are parallel
        if (delta == 0) { result = Vector2.zero; return false; }

        //project
        result = new Vector2((BNX * ACP - ANX * BCP) / delta, (ALY * BCP - BLY * ACP) / delta);
        return true;
    }

    //distance from point to line segment
    public static float DistanceToLine(Vector2 point, Line2 line)
    {
        //avoid division by zero
        if (line.start == line.end)
            return (point - line.start).magnitude;

        Vector2 lineVector = line.end - line.start;
        Vector2 startToPoint = point - line.start;

        float t = Vector2.Dot(lineVector, startToPoint) / lineVector.sqrMagnitude;

        if (t < 0) return startToPoint.magnitude;           //beyond start
        if (t > 1) return (point - line.end).magnitude;     //beyond end

        Vector2 projection = line.start + t * lineVector;
        return (point - projection).magnitude;
    }

    //area of a triangle
    public static float TriangleAreaUnsigned(Vector2 a, Vector2 b, Vector2 c) { return Mathf.Abs(TriangleAreaSigned(a, b, c)); }
    public static float TriangleAreaSigned(Vector2 a, Vector2 b, Vector2 c)
    {
        return (a.x - c.x) * (a.y + c.y) / 2 + (b.x - a.x) * (b.y + a.y) / 2 + (c.x - b.x) * (c.y + b.y) / 2;
    }

    //handedness of the triangle
    public static float TriangleSign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    //checks against half plane of each side
    public static bool PointInTriangle(Vector2 point, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        bool b1, b2, b3;

        b1 = TriangleSign(point, v1, v2) <= 0;
        b2 = TriangleSign(point, v2, v3) <= 0;
        b3 = TriangleSign(point, v3, v1) <= 0;

        return ((b1 == b2) && (b2 == b3));
    }

    //makes sure a list of vertices is in clockwise order
    public static void ForceClockwiseOrder(List<Vector2> vertices)
    {
        //calculate area of the whole polygon
        float area = (vertices[0].x - vertices[vertices.Count - 1].x) * (vertices[0].y + vertices[vertices.Count - 1].y) / 2;
        for (int i = 0; i < vertices.Count - 1; i++)
            area += (vertices[i + 1].x - vertices[i].x) * (vertices[i + 1].y + vertices[i].y) / 2;

        if (area < 0)
            vertices.Reverse();
    }

    //makes sure a list of vertices is in counter-clockwise order
    public static void ForceCounterClockwiseOrder(List<Vector2> vertices)
    {
        //calculate area of the whole polygon
        float area = (vertices[0].x - vertices[vertices.Count - 1].x) * (vertices[0].y + vertices[vertices.Count - 1].y) / 2;
        for (int i = 0; i < vertices.Count - 1; i++)
            area += (vertices[i + 1].x - vertices[i].x) * (vertices[i + 1].y + vertices[i].y) / 2;

        if (area > 0)
            vertices.Reverse();
    }

    //triangulates clockwise vertex list into triangles, returns empty list in case of a problem
    public static List<Triangle> EarClip(List<Vector2> vertices)
    {
        List<Triangle> triangles = new List<Triangle>();

        //need at least three vertices to triangulate
        if (vertices.Count < 3) return triangles;

        //copy into temp list 
        List<Vector2> polygon = new List<Vector2>(vertices.Count);
        polygon.AddRange(vertices);

        //----\\
        //use these if you cannot guarentee a valid list
        //ForceClockwiseOrder(polygon);
        //if (polygon[polygon.Count - 1] == polygon[0]) polygon.RemoveAt(polygon.Count - 1); //removes the last one if it's same as first
        //RemoveDuplicates(polygon); //heavier version of above
        //----\\

        //as long as it has more than one triangle, keep removing ears
        while (polygon.Count > 3)
            if (!RemoveEar(polygon, triangles))
            {
                //algorithm failed, return empty list
                triangles.Clear();
                return triangles;
            }

        //add the last remaining triangle
        triangles.Add(new Triangle(polygon[0], polygon[1], polygon[2]));
        return triangles;
    }

    //removes an ear from the vertices list and adds it to triangles
    private static bool RemoveEar(List<Vector2> temp, List<Triangle> triangles)
    {
        //find the next ear, if it fails, eject
        int A = 0, B = 0, C = 0;
        if (!FindEar(temp, ref A, ref B, ref C))
            return false;

        triangles.Add(new Triangle(temp[A], temp[B], temp[C]));

        temp.Remove(temp[B]);
        return true;
    }

    //find the indices to form the next ear
    private static bool FindEar(List<Vector2> polygon, ref int A, ref int B, ref int C)
    {
        int num_points = polygon.Count;

        for (A = 0; A < num_points; A++)
        {
            B = (A + 1) % num_points;
            C = (B + 1) % num_points;

            if (FormsEar(polygon, A, B, C)) return true;
        }

        //there should always be at least two ears, this is a bad polygon
        return false;
    }

    //returns true if the three points form an ear.
    private static bool FormsEar(List<Vector2> polygon, int A, int B, int C)
    {
        //calculate area of the triangle
        float area = TriangleAreaSigned(polygon[A], polygon[B], polygon[C]);

        //check if counter-clockwise or too thin line
        if (area < .001f) return false;

        Triangle triangle = new Triangle(polygon[A], polygon[B], polygon[C]);

        //check if any of the remaining points lies within this triangle
        for (int i = 0; i < polygon.Count; i++)
        {
            //don't test against itself
            if ((i == A) || (i == B) || (i == C)) continue;

            //test if this triangle would envelop any remaining vertex
            if (triangle.ContainsPoint(polygon[i]))
                return false;

            //test if any of the remaining vertex would fall on this triangle's edges
            if (DistanceToLine(polygon[i], new Line2(polygon[A], polygon[B])) < .001f ||
                DistanceToLine(polygon[i], new Line2(polygon[B], polygon[C])) < .001f ||
                DistanceToLine(polygon[i], new Line2(polygon[C], polygon[A])) < .001f) return false;
        }

        return true;
    }

    //checks if two lines intersect each other and gives out the point of intersection
    public static bool LinesIntersectPoint(Line2 a, Line2 b, out Vector2 intersectionPoint)
    {
        //component lengths
        float ALX = a.end.x - a.start.x;
        float ALY = a.end.y - a.start.y;
        float BLX = b.end.x - b.start.x;
        float BLY = b.end.y - b.start.y;

        //separations
        float SXS = a.start.x - b.start.x;
        float SYS = a.start.y - b.start.y;

        //projection
        float delta = -BLX * ALY + ALX * BLY;
        float s = (-ALY * SXS + ALX * SYS) / delta;
        float t = (BLX * SYS - BLY * SXS) / delta;

        //lies within projection space
        if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
        {
            intersectionPoint = new Vector2(a.start.x + (t * ALX), a.start.y + (t * ALY));
            return true;
        }

        //no collision
        intersectionPoint = Vector2.zero;
        return false;
    }

    //angle between three points
    public static float GetAngle(Vector2 a, Vector2 b, Vector2 c)
    {
        float v1x = b.x - c.x;
        float v1y = b.y - c.y;
        float v2x = a.x - c.x;
        float v2y = a.y - c.y;

        return (Mathf.Atan2(v1x, v1y) - Mathf.Atan2(v2x, v2y));
    }

    //removes one copy of each pair of same vertices
    public static void RemoveDuplicates(List<Vector2> vertices)
    {
        again:
        for (int i = 0; i < vertices.Count; i++)
            for (int j = 0; j < vertices.Count; j++)
                if (i != j)
                    if (vertices[i] == vertices[j])
                    {
                        vertices.RemoveAt(i);
                        goto again; //list is corrupted, eject
                    }
    }

    //cross product between three points
    public static float CrossProductLength(Vector2 a, Vector2 b, Vector2 c)
    { return ((a.x - b.x) * (c.y - b.y) - (a.y - b.y) * (c.x - b.x)); }

    //winding order doesn't matter
    public static bool IsConvex(List<Vector2> vertices)
    {
        bool foundNegative = false;
        bool foundPositive = false;

        int verticeAmount = vertices.Count;
        for (int A = 0; A < verticeAmount; A++)
        {
            int B = (A + 1) % verticeAmount;
            int C = (B + 1) % verticeAmount;

            float cross_product = CrossProductLength(vertices[A], vertices[B], vertices[C]);

            if (cross_product < 0) foundNegative = true;
            else if (cross_product > 0) foundPositive = true;

            //have we made a clockwise and counterclockwise turn, if so, we are not convex
            if (foundNegative && foundPositive) return false;
        }

        return true;
    }

    //calculates weighted value of ha-hb-hc from the relation of point p against triangle a-b-c
    public static float BarycentricWeight(Vector2 p, Vector2 a, Vector2 b, Vector2 c, float ha, float hb, float hc)
    {
        Vector2 v0 = b - a, v1 = c - a, v2 = p - a;
        float d00 = Vector2.Dot(v0, v0);
        float d01 = Vector2.Dot(v0, v1);
        float d11 = Vector2.Dot(v1, v1);
        float d20 = Vector2.Dot(v2, v0);
        float d21 = Vector2.Dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;
        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;

        return ha * u + hb * v + hc * w;
    }

    //find the points of intersection
    public static int FindLineCircleIntersections(float cx, float cy, float radius, Line2 line, out Vector2 intersection1, out Vector2 intersection2)
    {
        float dx, dy, A, B, C, det, t;

        dx = line.end.x - line.start.x;
        dy = line.end.y - line.end.y;

        A = dx * dx + dy * dy;
        B = 2 * (dx * (line.start.x - cx) + dy * (line.start.y - cy));
        C = (line.start.x - cx) * (line.start.x - cx) + (line.start.y - cy) * (line.start.y - cy) - radius * radius;

        det = B * B - 4 * A * C;
        if ((A <= 0.0000001) || (det < 0))
        {
            //no solutions
            intersection1 = new Vector2(float.NaN, float.NaN);
            intersection2 = new Vector2(float.NaN, float.NaN);
            return 0;
        }
        else if (det == 0)
        {
            //one solution
            t = -B / (2 * A);
            intersection1 = new Vector2(line.start.x + t * dx, line.start.y + t * dy);
            intersection2 = new Vector2(float.NaN, float.NaN);
            return 1;
        }
        else
        {
            //two solutions
            t = (-B + Mathf.Sqrt(det)) / (2 * A);
            intersection1 = new Vector2(line.start.x + t * dx, line.start.y + t * dy);
            t = (-B - Mathf.Sqrt(det)) / (2 * A);
            intersection2 = new Vector2(line.start.x + t * dx, line.start.y + t * dy);
            return 2;
        }
    }

    //find the points where the two circles intersect
    public static int FindCircleCircleIntersections(float cx0, float cy0, float radius0, float cx1, float cy1, float radius1, out Vector2 intersection1, out Vector2 intersection2)
    {
        //find the distance between the centers
        float dx = cx0 - cx1;
        float dy = cy0 - cy1;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);

        //see how many solutions there are
        if (dist > radius0 + radius1)
        {
            //no solutions, the circles are too far apart
            intersection1 = new Vector2(float.NaN, float.NaN);
            intersection2 = new Vector2(float.NaN, float.NaN);
            return 0;
        }
        else if (dist < Mathf.Abs(radius0 - radius1))
        {
            //no solutions, one circle contains the other
            intersection1 = new Vector2(float.NaN, float.NaN);
            intersection2 = new Vector2(float.NaN, float.NaN);
            return 0;
        }
        else if ((dist == 0) && (radius0 == radius1))
        {
            //no solutions, the circles coincide
            intersection1 = new Vector2(float.NaN, float.NaN);
            intersection2 = new Vector2(float.NaN, float.NaN);
            return 0;
        }
        else
        {
            //find a and h
            float a = (radius0 * radius0 - radius1 * radius1 + dist * dist) / (2 * dist);
            float h = Mathf.Sqrt(radius0 * radius0 - a * a);

            //find P2
            float cx2 = cx0 + a * (cx1 - cx0) / dist;
            float cy2 = cy0 + a * (cy1 - cy0) / dist;

            //get the points P3
            intersection1 = new Vector2(cx2 + h * (cy1 - cy0) / dist, cy2 - h * (cx1 - cx0) / dist);
            intersection2 = new Vector2(cx2 - h * (cy1 - cy0) / dist, cy2 + h * (cx1 - cx0) / dist);

            //see if we have 1 or 2 solutions.
            if (dist == radius0 + radius1) return 1;
            return 2;
        }
    }

    //line->circle entry point
    public static bool LineCircleClosestIntersection(float cx, float cy, float radius, Line2 line, out Vector2 intersection)
    {
        Vector2 intersection1;
        Vector2 intersection2;
        int intersections = FindLineCircleIntersections(cx, cy, radius, line, out intersection1, out intersection2);

        if (intersections == 1)
        {
            intersection = intersection1;
            return true;
        }

        if (intersections == 2)
        {
            float dist1 = (intersection1 - line.start).sqrMagnitude;
            float dist2 = (intersection2 - line.start).sqrMagnitude;
            intersection = dist1 < dist2 ? intersection1 : intersection2;
            return true;
        }

        intersection = new Vector2(float.NaN, float.NaN);
        return false;
    }

    //is the point within rectangular area of line (like bounding box)
    public static bool PointWithinLineBox(Vector2 point, Line2 line)
    {
        return (Mathf.Min(line.start.x, line.end.x) <= point.x) && (point.x <= Mathf.Max(line.start.x, line.end.x)) &&
            (Mathf.Min(line.start.y, line.end.y) <= point.y) && (point.y <= Mathf.Max(line.start.y, line.end.y));
    }

    //distance against line projection by normal
    public static float PerpendicularDistanceToLine(Vector2 point, Line2 line)
    {
        //avoid division by zero
        if (line.start == line.end) return (point - line.start).magnitude;

        float dx = line.end.x - line.start.x;
        float dy = line.end.y - line.start.y;

        return Mathf.Abs(dx * (line.start.y - point.y) - (line.start.x - point.x) * dy) / Mathf.Sqrt(dx * dx + dy * dy);
    }

    //not the fastest way
    public static string RomanNumeral(int value)
    {
        string roman = "";

        for (int i = 0; i < value; i++)
            roman += "I";

        return (roman
            .Replace("IIIII", "V")
            .Replace("IIII", "IV")
            .Replace("VV", "X")
            .Replace("VIV", "IX")
            .Replace("XXXXX", "L")
            .Replace("XXXX", "XL")
            .Replace("LL", "C")
            .Replace("LXL", "XC")
            .Replace("CCCCC", "D")
            .Replace("CCCC", "CD")
            .Replace("DD", "M")
            .Replace("DCD", "CM")
            );
    }
}