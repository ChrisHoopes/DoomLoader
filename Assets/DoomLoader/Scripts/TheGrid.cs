using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optimized grids that stores objects that touch a cell
/// </summary>
public static class TheGrid
{
    public static List<Triangle>[,] triangles;
    public static List<Sector>[,] sectors;
    public static List<Linedef>[,] linedefs;
    public static DoubleLinkedList<ThingController>[,] monsterThings;
    public static DoubleLinkedList<ThingController>[,] neutralThings;
    public static DoubleLinkedList<ThingController>[,] decorThings;
    public static DoubleLinkedList<ThingController>[,] itemThings;
    public static BooleanBox existenceBox = new BooleanBox();
    public static int origoX;
    public static int origoY;
    public static int endX;
    public static int endY;
    public static int sizeX;
    public static int sizeY;

    //needs to be called after map has been loaded
    public static void Init()
    {
        origoX = Mathf.FloorToInt((float)MapLoader.minX / MapLoader.sizeDividor) - 1;
        origoY = Mathf.FloorToInt((float)MapLoader.minY / MapLoader.sizeDividor) - 1;
        endX = Mathf.CeilToInt((float)MapLoader.maxX / MapLoader.sizeDividor) + 1;
        endY = Mathf.CeilToInt((float)MapLoader.maxY / MapLoader.sizeDividor) + 1;

        sizeX = endX - origoX;
        sizeY = endY - origoY;

        triangles = new List<Triangle>[sizeX, sizeY];
        sectors = new List<Sector>[sizeX, sizeY];
        linedefs = new List<Linedef>[sizeX, sizeY];
        monsterThings = new DoubleLinkedList<ThingController>[sizeX, sizeY];
        neutralThings = new DoubleLinkedList<ThingController>[sizeX, sizeY];
        decorThings = new DoubleLinkedList<ThingController>[sizeX, sizeY];
        itemThings = new DoubleLinkedList<ThingController>[sizeX, sizeY];

        for (int y = 0; y < sizeY; y++)
            for (int x = 0; x < sizeX; x++)
            {
                triangles[x, y] = new List<Triangle>();
                sectors[x, y] = new List<Sector>();
                linedefs[x, y] = new List<Linedef>();
                monsterThings[x, y] = new DoubleLinkedList<ThingController>();
                neutralThings[x, y] = new DoubleLinkedList<ThingController>();
                decorThings[x, y] = new DoubleLinkedList<ThingController>();
                itemThings[x, y] = new DoubleLinkedList<ThingController>();
            }

        existenceBox = new BooleanBox(new Vec2I(origoX, origoY), new Vec2I(sizeX, sizeY));
    }

    /// <summary>
    /// Gets sector triangle from this point. Returns null if no sector found.
    /// </summary>
    public static Triangle GetExactTriangle(Vector3 position) { return GetExactTriangle(position.x, position.z); }
    public static Triangle GetExactTriangle(float posX, float posY)
    {
        foreach (Triangle t in GetNearbyTriangles(posX, posY))
            if (t.ContainsPoint(posX, posY))
                return t;

        return null;
    }

    /// <summary>
    /// Gets nearby triangles that can touch this point, use Triangle.ContainsPoint to verify
    /// </summary>
    public static List<Triangle> GetNearbyTriangles(Vector3 position) { return GetNearbyTriangles(position.x, position.z); }
    public static List<Triangle> GetNearbyTriangles(float posX, float posY)
    {
        int x = Mathf.FloorToInt(posX) - origoX;
        int y = Mathf.FloorToInt(posY) - origoY;

        if (x < 0 || x >= sizeX) return new List<Triangle>();
        if (y < 0 || y >= sizeY) return new List<Triangle>();

        return triangles[x, y];
    }

    /// <summary>
    /// Gets nearby triangles that can touch the point and surrounding grid cells, extend must be > 0
    /// </summary>
    public static List<Triangle> GetMoreNearbyTriangles(Vector3 position, int extend) { return GetMoreNearbyTriangles(position.x, position.z, extend); }
    public static List<Triangle> GetMoreNearbyTriangles(float posX, float posY, int extend)
    {
        List<Triangle> list = new List<Triangle>();

        int gridX = Mathf.FloorToInt(posX) - origoX;
        int gridY = Mathf.FloorToInt(posY) - origoY;

        for (int y = gridY - extend; y < gridY + extend; y++)
            for (int x = gridX - extend; x < gridX + extend; x++)
                if (x >= 0 && x < sizeX && y >= 0 && y < sizeY)
                    list.AddRange(triangles[x, y]);

        return list;
    }

    /// <summary>
    /// Gets nearby sectors that can touch this point, test against sector triangles to verify
    /// </summary>
    public static List<Sector> GetNearbySectors(Vector3 position) { return GetNearbySectors(position.x, position.z); }
    public static List<Sector> GetNearbySectors(float posX, float posY)
    {
        int x = Mathf.FloorToInt(posX) - origoX;
        int y = Mathf.FloorToInt(posY) - origoY;

        if (x < 0 || x >= sizeX) return new List<Sector>();
        if (y < 0 || y >= sizeY) return new List<Sector>();

        return sectors[x, y];
    }

    /// <summary>
    /// Gets nearby sectors that can touch the point and surrounding grid cells, extend must be > 0
    /// </summary>
    public static List<Sector> GetMoreNearbySectors(Vector3 position, int extend) { return GetMoreNearbySectors(position.x, position.z, extend); }
    public static List<Sector> GetMoreNearbySectors(float posX, float posY, int extend)
    {
        List<Sector> list = new List<Sector>();

        int gridX = Mathf.FloorToInt(posX) - origoX;
        int gridY = Mathf.FloorToInt(posY) - origoY;

        for (int y = gridY - extend; y < gridY + extend; y++)
            for (int x = gridX - extend; x < gridX + extend; x++)
                if (x >= 0 && x < sizeX && y >= 0 && y < sizeY)
                    list.AddRange(sectors[x, y]);

        return list;
    }

    /// <summary>
    /// Gets nearby linedefs that can touch this point
    /// </summary>
    public static List<Linedef> GetNearbyLinedefs(Vector3 position) { return GetNearbyLinedefs(position.x, position.z); }
    public static List<Linedef> GetNearbyLinedefs(float posX, float posY)
    {
        int x = Mathf.FloorToInt(posX) - origoX;
        int y = Mathf.FloorToInt(posY) - origoY;

        if (x < 0 || x >= sizeX) return new List<Linedef>();
        if (y < 0 || y >= sizeY) return new List<Linedef>();

        return linedefs[x, y];
    }

    /// <summary>
    /// Gets nearby linedefs that can touch the point and surrounding grid cells, extend must be > 0
    /// </summary>
    public static List<Linedef> GetMoreNearbyLinedefs(Vector3 position, int extend) { return GetMoreNearbyLinedefs(position.x, position.z, extend); }
    public static List<Linedef> GetMoreNearbyLinedefs(float posX, float posY, int extend)
    {
        List<Linedef> list = new List<Linedef>();

        int gridX = Mathf.FloorToInt(posX) - origoX;
        int gridY = Mathf.FloorToInt(posY) - origoY;

        for (int y = gridY - extend; y < gridY + extend; y++)
            for (int x = gridX - extend; x < gridX + extend; x++)
                if (x >= 0 && x < sizeX && y >= 0 && y < sizeY)
                    list.AddRange(linedefs[x, y]);

        return list;
    }

    /// <summary>
    /// Gets nearby monsters that can touch the point and surrounding grid cells
    /// </summary>
    public static DoubleLinkedList<ThingController> GetNearbyMonsters(Vector3 position, int extend) { return GetNearbyMonsters(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.z), extend); }
    public static DoubleLinkedList<ThingController> GetNearbyMonsters(float posX, float posY, int extend) { return GetNearbyMonsters(Mathf.FloorToInt(posX), Mathf.FloorToInt(posY), extend); }
    public static DoubleLinkedList<ThingController> GetNearbyMonsters(Vec2I position, int extend) { return GetNearbyMonsters(position.x, position.y, extend); }
    public static DoubleLinkedList<ThingController> GetNearbyMonsters(int posX, int posY, int extend)
    {
        int gridX = posX - origoX;
        int gridY = posY - origoY;

        if (extend == 0)
        {
            if (gridX < 0 && gridX >= sizeX && gridY < 0 && gridY >= sizeY)
                return new DoubleLinkedList<ThingController>();

            return monsterThings[gridX, gridY];
        }

        DoubleLinkedList<ThingController> list = new DoubleLinkedList<ThingController>();
        for (int y = gridY - extend; y < gridY + extend; y++)
            for (int x = gridX - extend; x < gridX + extend; x++)
                if (x >= 0 && x < sizeX && y >= 0 && y < sizeY)
                    monsterThings[x, y].Perform((n) => { list.InsertFront(n.Data); });
        return list;
    }


    /// <summary>
    /// Gets nearby decors that can touch the point and surrounding grid cells
    /// </summary>
    public static DoubleLinkedList<ThingController> GetNearbyDecorThings(Vector3 position, int extend) { return GetNearbyDecorThings(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.z), extend); }
    public static DoubleLinkedList<ThingController> GetNearbyDecorThings(float posX, float posY, int extend) { return GetNearbyDecorThings(Mathf.FloorToInt(posX), Mathf.FloorToInt(posY), extend); }
    public static DoubleLinkedList<ThingController> GetNearbyDecorThings(Vec2I position, int extend) { return GetNearbyDecorThings(position.x, position.y, extend); }
    public static DoubleLinkedList<ThingController> GetNearbyDecorThings(int posX, int posY, int extend)
    {
        int gridX = posX - origoX;
        int gridY = posY - origoY;

        if (extend == 0)
        {
            if (gridX < 0 && gridX >= sizeX && gridY < 0 && gridY >= sizeY)
                return new DoubleLinkedList<ThingController>();

            return decorThings[gridX, gridY];
        }

        DoubleLinkedList<ThingController> list = new DoubleLinkedList<ThingController>();

        for (int y = gridY - extend; y < gridY + extend; y++)
            for (int x = gridX - extend; x < gridX + extend; x++)
                if (x >= 0 && x < sizeX && y >= 0 && y < sizeY)
                    decorThings[x, y].Perform((n) => { list.InsertFront(n.Data); });

        return list;
    }


    /// <summary>
    /// Gets nearby items that can touch the point and surrounding grid cells
    /// </summary>
    public static DoubleLinkedList<ThingController> GetNearbyItems(Vector3 position, int extend) { return GetNearbyItems(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.z), extend); }
    public static DoubleLinkedList<ThingController> GetNearbyItems(float posX, float posY, int extend) { return GetNearbyItems(Mathf.FloorToInt(posX), Mathf.FloorToInt(posY), extend); }
    public static DoubleLinkedList<ThingController> GetNearbyItems(Vec2I position, int extend) { return GetNearbyItems(position.x, position.y, extend); }
    public static DoubleLinkedList<ThingController> GetNearbyItems(int posX, int posY, int extend)
    {
        int gridX = posX - origoX;
        int gridY = posY - origoY;

        if (extend == 0)
        {
            if (gridX < 0 && gridX >= sizeX && gridY < 0 && gridY >= sizeY)
                return new DoubleLinkedList<ThingController>();

            return itemThings[gridX, gridY];
        }

        DoubleLinkedList<ThingController> list = new DoubleLinkedList<ThingController>();

        for (int y = gridY - extend; y < gridY + extend; y++)
            for (int x = gridX - extend; x < gridX + extend; x++)
                if (x >= 0 && x < sizeX && y >= 0 && y < sizeY)
                    itemThings[x, y].Perform((n) => { list.InsertFront(n.Data); });

        return list;
    }

    /// <summary>
    /// Gets nearby neutrals that can touch the point and surrounding grid cells
    /// </summary>
    public static DoubleLinkedList<ThingController> GetNearbyNeutralThings(Vector3 position, int extend) { return GetNearbyNeutralThings(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.z), extend); }
    public static DoubleLinkedList<ThingController> GetNearbyNeutralThings(float posX, float posY, int extend) { return GetNearbyNeutralThings(Mathf.FloorToInt(posX), Mathf.FloorToInt(posY), extend); }
    public static DoubleLinkedList<ThingController> GetNearbyNeutralThings(Vec2I position, int extend) { return GetNearbyNeutralThings(position.x, position.y, extend); }
    public static DoubleLinkedList<ThingController> GetNearbyNeutralThings(int posX, int posY, int extend)
    {
        int gridX = posX - origoX;
        int gridY = posY - origoY;

        if (extend == 0)
        {
            if (gridX < 0 && gridX >= sizeX && gridY < 0 && gridY >= sizeY)
                return new DoubleLinkedList<ThingController>();

            return neutralThings[gridX, gridY];
        }

        DoubleLinkedList<ThingController> list = new DoubleLinkedList<ThingController>();

        for (int y = gridY - extend; y < gridY + extend; y++)
            for (int x = gridX - extend; x < gridX + extend; x++)
                if (x >= 0 && x < sizeX && y >= 0 && y < sizeY)
                    neutralThings[x, y].Perform((n) => { list.InsertFront(n.Data); });

        return list;
    }
}
