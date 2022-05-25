using UnityEngine;
using System.Collections.Generic;

public class Triangle
{
    public Sector sector;

    public Vector2[] vertices = new Vector2[3];
    public Triangle() { }
    public Triangle(Vector2 a, Vector2 b, Vector2 c) { vertices = new Vector2[3] { a, b, c }; }
    public Vector2 Center { get { return (vertices[0] + vertices[1] + vertices[2]) / 3; } }
    public bool ContainsPoint(Vector2 point) { return AxMath.PointInTriangle(point, vertices[0], vertices[1], vertices[2]); }
    public bool ContainsPoint(float x, float y) { return AxMath.PointInTriangle(new Vector2(x, y), vertices[0], vertices[1], vertices[2]); }
    public float GetAreaUnsigned() { return AxMath.TriangleAreaUnsigned(vertices[0], vertices[1], vertices[2]); }
    public float GetAreaSigned() { return AxMath.TriangleAreaSigned(vertices[0], vertices[1], vertices[2]); }

    public Vector2 RandomPoint
    {
        get
        {
            float r1 = Random.value;
            float r2 = Random.value;

            return new Vector2(
            (1 - Mathf.Sqrt(r1)) * vertices[0].x + (Mathf.Sqrt(r1) * (1 - r2)) * vertices[1].x + (Mathf.Sqrt(r1) * r2) * vertices[2].x,
            (1 - Mathf.Sqrt(r1)) * vertices[0].y + (Mathf.Sqrt(r1) * (1 - r2)) * vertices[1].y + (Mathf.Sqrt(r1) * r2) * vertices[2].y
            );
        }
    }

    public BooleanBox SelectBox
    {
        get
        {
            Vec2I[] l0 = AxMath.CartesianLineF(vertices[0], vertices[1]).ToArray();
            Vec2I[] l1 = AxMath.CartesianLineF(vertices[0], vertices[2]).ToArray();
            Vec2I[] l2 = AxMath.CartesianLineF(vertices[1], vertices[2]).ToArray();

            if (l0.Length == 0)
                l0 = new Vec2I[1] { new Vec2I(Mathf.FloorToInt(vertices[0].x), Mathf.FloorToInt(vertices[0].y)) };

            if (l1.Length == 0)
                l1 = new Vec2I[1] { new Vec2I(Mathf.FloorToInt(vertices[1].x), Mathf.FloorToInt(vertices[1].y)) };

            if (l2.Length == 0)
                l2 = new Vec2I[1] { new Vec2I(Mathf.FloorToInt(vertices[2].x), Mathf.FloorToInt(vertices[2].y)) };


            int minX, minY, maxX, maxY;

            //get bounds
            Vec2I l0min = Vec2I.CreateMin(l0[0], l0[l0.Length - 1]);
            Vec2I l1min = Vec2I.CreateMin(l1[0], l1[l1.Length - 1]);
            Vec2I l2min = Vec2I.CreateMin(l2[0], l2[l2.Length - 1]);
            Vec2I l0max = Vec2I.CreateMax(l0[0], l0[l0.Length - 1]);
            Vec2I l1max = Vec2I.CreateMax(l1[0], l1[l1.Length - 1]);
            Vec2I l2max = Vec2I.CreateMax(l2[0], l2[l2.Length - 1]);
            minX = Mathf.Min(l0min.x, Mathf.Min(l1min.x, l2min.x));
            minY = Mathf.Min(l0min.y, Mathf.Min(l1min.y, l2min.y));
            maxX = Mathf.Max(l0max.x, Mathf.Max(l1max.x, l2max.x));
            maxY = Mathf.Max(l0max.y, Mathf.Max(l1max.y, l2max.y));

            BooleanBox bbox = new BooleanBox(new Vec2I(minX, minY), new Vec2I(maxX - minX + 1, maxY - minY + 1));

            //fill edges
            foreach (Vec2I l in l0) bbox.SetValue(l, true);
            foreach (Vec2I l in l1) bbox.SetValue(l, true);
            foreach (Vec2I l in l2) bbox.SetValue(l, true);

            //fill inside
            for (int y = 0; y < bbox.size.y; y++)
                for (int x = 0; x < bbox.size.x; x++)
                    if (AxMath.PointInTriangle(new Vector2(bbox.origo.x + x, bbox.origo.y + y) + Vector2.one * .5f, vertices[0], vertices[1], vertices[2]))
                        bbox.data[x, y] = true;

            return bbox;
        }
    }
}