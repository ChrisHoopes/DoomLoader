using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Breath
{
    public Vec2I position;
    public int direction;
    public int distance;
    public int steps;

    public Breath(Vec2I Position, int Direction, int Distance, int Steps)
    {
        position = Position;
        direction = Direction;
        distance = Distance;
        steps = Steps;
    }
}

[System.Serializable]
public class BreathArea
{
    public Vec2I position;
    private int _maxDistance;
    public int maxDistance { get { return _maxDistance; } }
    private int _size;
    public int size { get { return _size; } }
    public bool[,] exist;
    public int[,] direction;
    public int[,] distance;
    public int[,] steps;

    public BreathArea(int MaxDistance)
    {
        _maxDistance = MaxDistance;
        _size = MaxDistance * 2 + 1;

        exist = new bool[_size, _size];
        direction = new int[_size, _size];
        distance = new int[_size, _size];
        steps = new int[_size, _size];
    }

    public Breath GetBreath(Vec2I position) { return GetBreath(position.x, position.y); }
    public Breath GetBreath(int posX, int posY)
    {
        int gridX = posX - position.x + maxDistance;
        int gridY = posY - position.y + maxDistance;

        if (gridX < 0 || gridX >= size || gridY < 0 || gridY >= size)
            return null;

        return exist[gridX, gridY] ? new Breath(new Vec2I(posX, posY), direction[gridX, gridY], distance[gridX, gridY], steps[gridX, gridY]) : null;
    }

    public List<Breath> GetNearbyBreaths(Vec2I position, int extend) { return GetNearbyBreaths(position.x, position.y, extend); }
    public List<Breath> GetNearbyBreaths(int posX, int posY, int extend)
    {
        List<Breath> list = new List<Breath>();

        if (extend == 0)
        {
            int gridX = posX - position.x + maxDistance;
            int gridY = posY - position.y + maxDistance;

            if (gridX >= 0 && gridX < size && gridY >= 0 && gridY < size)
                if (exist[gridX, gridY])
                    list.Add(new Breath(new Vec2I(posX, posY), direction[gridX, gridY], distance[gridX, gridY], steps[gridX, gridY]));

            return list;
        }

        for (int y = -extend; y <= extend; y++)
            for (int x = -extend; x <= extend; x++)
            {
                if (y == 0 && x == 0)
                    continue;

                int gridX = posX - position.x + maxDistance + x;
                int gridY = posY - position.y + maxDistance + y;

                if (gridX >= 0 && gridX < size && gridY >= 0 && gridY < size)
                    if (exist[gridX, gridY])
                        list.Add(new Breath(new Vec2I(posX + x, posY + y), direction[gridX, gridY], distance[gridX, gridY], steps[gridX, gridY]));
            }

        return list;
    }

    public SingleLinkedList<Breath> BreathList
    {
        get
        {
            SingleLinkedList<Breath> list = new SingleLinkedList<Breath>();

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (exist[x, y])
                        list.InsertFront(new Breath(new Vec2I(x - maxDistance + position.x, y - maxDistance + position.y), direction[x, y], distance[x, y], steps[x, y]));

            return list;
        }
    }

    public void Invalidate()
    {
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                exist[x, y] = false;
    }
}
