using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class to handle evaluation of the map, pathfinding and breath casting
/// </summary>
public static class AI
{
    //heatmap tells how passable a certain square is on the map
    //x tells how much hard walls touch the cell, y tells the height of the ledge and z tells if the area is dangerous
    //illegal or out of map heat will be -1,-1,-1
    public static Vector3[,] heatmap = new Vector3[0, 0];

    public static Vector3 GetHeat(Vec2I position) { return GetHeat(position.x, position.y); }
    public static Vector3 GetHeat(int posX, int posY)
    {
        int x = posX - TheGrid.origoX;
        int y = posY - TheGrid.origoY;

        if (x < 0 || x >= TheGrid.sizeX) return new Vector3(-1, -1, -1);
        if (y < 0 || y >= TheGrid.sizeY) return new Vector3(-1, -1, -1);

        return heatmap[x, y];
    }

    //does a cell lies inside map
    public static bool CanPath(Vec2I position) { return CanPath(position.x, position.y); }
    public static bool CanPath(int x, int y)
    {
        Vector3 heat = GetHeat(x, y);
        if (heat.x == -1)
            return false;

        return true;
    }

    //evaluates movement cost based on heatmap, even tho monsters don't get damage from environment it seem more natural that they avoid those areas
    public static int GetTravelCost(Vec2I position) { return GetTravelCost(position.x, position.y); }
    public static int GetTravelCost(int posX, int posY)
    {
        Vector3 heat = GetHeat(posX, posY);

        if (heat.x == -1)
            return 1000;

        if (heat.x >= 1)
            return 1000;

        if (heat.x >= .5f)
            return 100;

        if (heat.y >= 1f)
            return 60;

        if (heat.z > 0f)
            return 50;

        if (heat.y >= .75f)
            return 45;

        if (heat.y >= .5f)
            return 20;

        if (heat.y >= .25f)
            return 15;

        return 10;
    }

    //evaluates the height difference between cells
    public static bool HasLedge(Vec2I from, Vec2I to, bool ignoreDynamicSectors = false)
    {
        return HasLedge(new Vector3(from.x + .5f, 0, from.y + .5f), new Vector3(to.x + .5f, 0, to.y + .5f), ignoreDynamicSectors);
    }
    public static bool HasLedge(Vector3 from, Vector3 to, bool ignoreDynamicSectors = false)
    {
        Triangle t0 = TheGrid.GetExactTriangle(from);
        Triangle t1 = TheGrid.GetExactTriangle(to);

        if (t0 == null || t1 == null)
            return false;

        if (ignoreDynamicSectors)
            if (t0.sector.Dynamic || t1.sector.Dynamic)
                return false;

        return Mathf.Abs(t0.sector.floorHeight - t1.sector.floorHeight) > GameManager.maxStairHeight;
    }

    //tests cell height difference against GameManager.maxStairHeight
    public static bool FloorDifference(Vec2I from, Vec2I to, bool ignoreDynamicSectors)
    {
        float lowestFrom = float.MaxValue;
        float highestTo = float.MinValue;

        foreach (Sector s in TheGrid.GetNearbySectors(from.x, from.y))
        {
            if (ignoreDynamicSectors)
                if (s.Dynamic)
                    continue;

            if (s.floorHeight < lowestFrom)
                lowestFrom = s.floorHeight;
        }

        foreach (Sector s in TheGrid.GetNearbySectors(to.x, to.y))
        {
            if (ignoreDynamicSectors)
                if (s.Dynamic)
                    continue;

            if (s.floorHeight > highestTo)
                highestTo = s.floorHeight;
        }

        if (lowestFrom == float.MaxValue || highestTo == float.MinValue)
            return false;

        return Mathf.Abs(lowestFrom - highestTo) > GameManager.maxStairHeight;
    }

    public static void CalculateHeat(Vec2I cell) { CalculateHeat(cell.x, cell.y); }
    public static void CalculateHeat(int posX, int posY)
    {
        int x = posX - TheGrid.origoX;
        int y = posY - TheGrid.origoY;

        if (x < 0 || x >= TheGrid.sizeX) return;
        if (y < 0 || y >= TheGrid.sizeY) return;

        _CalculateHeat(x, y);
    }

    private static void _CalculateHeat(int x, int y)
    {
        Vector3 heat = Vector3.zero;

        //outside of map bounds
        if (TheGrid.existenceBox.data[x, y] == false)
        {
            heat.x = -1f;
            heat.y = -1f;
            goto skipall;
        }

        Vector3 pos1 = new Vector3(TheGrid.origoX + x, 0, TheGrid.origoY + y) + Vector3.one * .5f;

        //the position didn't hit any sector floor, mark it as impassable
        Triangle t = TheGrid.GetExactTriangle(pos1);
        float h = 0;
        if (t != null)
            h = t.sector.floorHeight + GameManager.maxStairHeight;
        else
        {
            heat.x = 1f;
            heat.y = 1f;
            goto skipcapsule;
        }

        //if a hard wall passes through the cell, add some x value to the heatmap
        foreach (Linedef l in TheGrid.linedefs[x, y])
            if (l.Back == null || (l.flags & (1 << 0)) != 0 && !(l.lineType == 1 || l.lineType == 4))
            {
                heat.x += .5f;
                break;
            }

        pos1.y = h;
        Vector3 pos2 = pos1 + Vector3.up * .5f;

        //capsulecast inside cell touched a physical solid, add some x value to the heatmap
        if (Physics.CheckCapsule(pos1, pos2, .499f, ~((1 << 10) | (1 << 11)), QueryTriggerInteraction.Ignore))
            heat.x += .5f;

        skipcapsule:

        //evaluate the sectors inside the cell
        float lowestf = float.MaxValue;
        float highestf = float.MinValue;
        float lowestc = float.MaxValue;
        float highestc = float.MinValue;
        foreach (Sector s in TheGrid.sectors[x, y])
        {
            if (s.floorHeight < lowestf)
                lowestf = s.floorHeight;

            if (s.floorHeight > highestf)
                highestf = s.floorHeight;

            if (s.ceilingHeight < lowestc)
                lowestc = s.ceilingHeight;

            if (s.ceilingHeight > highestc)
                highestc = s.ceilingHeight;

            //dangerous area, add some z value to the heatmap
            if (s.specialType == 4 || s.specialType == 5 || s.specialType == 7 || s.specialType == 16)
                heat.z = 1f;
        }

        //there is a difference between sector heights inside the cell, add some y value to the heatmap
        if (lowestf != float.MaxValue)
        {
            float diff = highestf - lowestf;

            if (diff > MapLoader._16units)
                heat.y += .25f;

            if (diff > MapLoader._32units)
                heat.y += .25f;

            if (diff > MapLoader._64units)
                heat.y += .25f;

            if (diff > MapLoader._96units)
                heat.y += .25f;

            if (lowestc != float.MaxValue)
            {
                float gap = lowestc - highestf;

                if (gap < MapLoader._32units)
                    heat.y = 1f;
            }
        }

        skipall:
        heatmap[x, y] = heat;
    }

    public static void CreateHeatmap()
    {
        heatmap = new Vector3[TheGrid.sizeX, TheGrid.sizeY];

        for (int y = 0; y < TheGrid.sizeY; y++)
            for (int x = 0; x < TheGrid.sizeX; x++)
                _CalculateHeat(x, y);
    }

    public class PathStep : IBinaryHeapItem<PathStep>
    {
        public Vec2I position;

        public int fullCost;
        public int travelCost;
        public int heuristic;
        private int _heapIndex;
        public int HeapIndex { get { return _heapIndex; } set { _heapIndex = value; } }
        public int CompareTo(PathStep target)
        {
            int compare = fullCost.CompareTo(target.fullCost);

            if (compare == 0)
                compare = heuristic.CompareTo(target.heuristic);

            //need to be flipped since binaryheap stores highest number first
            return -compare;
        }

        public PathStep(Vec2I Position, int TravelCost, int Heuristic)
        {
            position = Position;
            travelCost = TravelCost;
            heuristic = Heuristic;
            fullCost = travelCost + heuristic;
        }

        public PathStep(Vec2I Position, int Heuristic)
        {
            position = Position;
            heuristic = Heuristic;
            fullCost = heuristic;
        }
    }

    //optimized A* pathfinding
    public static bool GetPath(Vec2I StartPoint, Vec2I EndPoint, int maxDistance, out Vec2I[] path)
    {
        if (StartPoint == EndPoint)
        {
            path = new Vec2I[1];
            path[0] = EndPoint;
            return true;
        }

        path = null;

        if (AxMath.RogueDistance(StartPoint, EndPoint) > maxDistance) return false;
        if (GetHeat(StartPoint).x == -1) return false;
        if (GetHeat(EndPoint).x == -1) return false;

        //init arrays
        int arraySize = maxDistance * 2 + 1;
        bool[,] closedCheck = new bool[arraySize, arraySize];
        bool[,] openCheck = new bool[arraySize, arraySize];
        Vec2I[,] parents = new Vec2I[arraySize, arraySize];
        PathStep[,] openArray = new PathStep[arraySize, arraySize];

        //set start point
        BinaryHeap<PathStep> openList = new BinaryHeap<PathStep>(arraySize * arraySize);
        openList.Add(new PathStep(StartPoint, AxMath.WeightedDistance(StartPoint, EndPoint)));
        openCheck[maxDistance, maxDistance] = true;
        parents[maxDistance, maxDistance] = StartPoint;

        bool found = false;
        while (openList.ItemCount > 0)
        {
            //get top of heap
            PathStep current = openList.RemoveFirst();
            closedCheck[current.position.x - StartPoint.x + maxDistance, current.position.y - StartPoint.y + maxDistance] = true;

            foreach (Vec2I neighbor in current.position.neighbors)
            {
                //calculate array position
                int arrayX = neighbor.x - StartPoint.x + maxDistance;
                int arrayY = neighbor.y - StartPoint.y + maxDistance;

                //cull disallowed
                if (AxMath.RogueDistance(neighbor, StartPoint) > maxDistance) continue;
                if (closedCheck[arrayX, arrayY]) continue;

                //found target
                if (neighbor == EndPoint)
                {
                    parents[arrayX, arrayY] = current.position;
                    found = true;
                    goto finalize;
                }

                if (!CanPath(neighbor))
                    continue;

                //calculate cost
                int travelCost = current.travelCost + AxMath.WeightedDistance(current.position, neighbor);
                int heuristic = AxMath.WeightedDistance(neighbor, EndPoint);
                int fullCost = travelCost + heuristic;

                //check if we can update parent to better 
                if (openCheck[arrayX, arrayY])
                    if (openArray[arrayX, arrayY].travelCost > travelCost)
                    {
                        openArray[arrayX, arrayY].travelCost = travelCost;
                        openArray[arrayX, arrayY].heuristic = heuristic;
                        openArray[arrayX, arrayY].fullCost = fullCost;
                        parents[arrayX, arrayY] = current.position;
                        openList.UpdateItem(openArray[arrayX, arrayY]);
                        continue;
                    }
                    else
                        continue;

                //priority sorted by heap
                PathStep step = new PathStep(neighbor, travelCost, heuristic);
                openList.Add(step);
                openCheck[arrayX, arrayY] = true;
                openArray[arrayX, arrayY] = step;
                parents[arrayX, arrayY] = current.position;
            }
        }

    finalize:
        if (found)
        {
            SingleLinkedList<Vec2I> list = new SingleLinkedList<Vec2I>();

            Vec2I current = EndPoint;
            while (current != StartPoint)
            {
                list.InsertFront(current);
                current = parents[current.x - StartPoint.x + maxDistance, current.y - StartPoint.y + maxDistance];
            }
            //list.InsertFront(current); //adds the starting point to the path
            path = list.ToArray();
            return true;
        }

        return false;
    }

    //breadth-first fill algorithm, funny note: years ago when I was learning pathfinding I misread the name to be breath-first, hence the name
    public static void FillBreath(ref BreathArea breath, ref List<ThingController> monstersList, bool cullByTrueDistance = false)
    {
        int maxDistance = breath.maxDistance;
        int arraySize = breath.size;
        Vec2I StartPoint = breath.position;

        breath.Invalidate();

        if (GetHeat(StartPoint).x == -1)
            return;

        //init arrays
        bool[,] closedCheck = new bool[arraySize, arraySize];
        bool[,] openCheck = new bool[arraySize, arraySize];

        //set start point
        SingleLinkedList<Vec2I> openList = new SingleLinkedList<Vec2I>();
        openList.InsertFront(StartPoint);
        openCheck[maxDistance, maxDistance] = true;

        breath.exist[maxDistance, maxDistance] = true;
        breath.steps[maxDistance, maxDistance] = 0;
        breath.distance[maxDistance, maxDistance] = 0;
        breath.direction[maxDistance, maxDistance] = 0;

        List<ThingController> monsters = new List<ThingController>();

        SingleLinkedList<int> randomNeighbors = new SingleLinkedList<int>();
        int maxStepDistance = maxDistance * 10;

        while (openList.Count > 0)
        {
            //get top of heap
            Vec2I current = openList.RemoveHead();
            int ax = current.x - StartPoint.x + maxDistance;
            int ay = current.y - StartPoint.y + maxDistance;
            int currentDistance = breath.distance[ax, ay];
            int currentSteps = breath.steps[ax, ay];
            closedCheck[ax, ay] = true;

            TheGrid.GetNearbyMonsters(current, 0).Perform((n) => { monsters.Add(n.Data); });

            if (cullByTrueDistance)
                if (currentDistance >= maxStepDistance)
                    continue;

            Vector3 currentHeat = GetHeat(current);

            //no propagation through solids
            if (currentHeat.x >= 1f)
                if (current != StartPoint)
                    continue;

            //don't hassle, shuffle
            for (int i = 1; i < 9; i++)
                if (Random.value > .5f)
                    randomNeighbors.InsertFront(i);
                else
                    randomNeighbors.InsertBack(i);

            while (randomNeighbors.Count > 0)
            {
                int i = randomNeighbors.RemoveHead();
                Vec2I neighbor = current + Vec2I.directions[i];

                //calculate array position
                int arrayX = neighbor.x - StartPoint.x + maxDistance;
                int arrayY = neighbor.y - StartPoint.y + maxDistance;

                //cull disallowed
                if (AxMath.RogueDistance(neighbor, StartPoint) > maxDistance) continue;
                if (openCheck[arrayX, arrayY]) continue;
                if (closedCheck[arrayX, arrayY]) continue;

                if (!CanPath(neighbor))
                    continue;

                openList.InsertBack(neighbor);
                openCheck[arrayX, arrayY] = true;

                //reverse direction to point towards the source of breath
                int p = i + 4;
                if (p > 8) p -= 8;

                breath.exist[arrayX, arrayY] = true;
                breath.direction[arrayX, arrayY] = p;
                breath.distance[arrayX, arrayY] = currentDistance + StepDistance(i);
                breath.steps[arrayX, arrayY] = currentSteps + 1;
            }
        }

        monstersList = monsters;
    }

    //diagonal moves are ~sqrt(2) more costly
    public static int StepDistance(int direction)
    {
        return direction % 2 == 0 ? 14 : 10;
    }

    //same as previous, but take into account movement cost of cells
    public static void FillPlayerBreath(ref BreathArea breath, ref List<ThingController> monstersList, bool cullByTrueDistance = false)
    {
        int maxDistance = breath.maxDistance;
        int arraySize = breath.size;
        Vec2I StartPoint = breath.position;

        breath.Invalidate();
        monstersList.Clear();

        if (GetHeat(StartPoint).x == -1)
            return;

        //init arrays
        bool[,] closedCheck = new bool[arraySize, arraySize];
        bool[,] openCheck = new bool[arraySize, arraySize];
        PathStep[,] openArray = new PathStep[arraySize, arraySize];

        //set start point
        BinaryHeap<PathStep> openList = new BinaryHeap<PathStep>(arraySize * arraySize);
        openList.Add(new PathStep(StartPoint, 0));
        openCheck[maxDistance, maxDistance] = true;

        breath.exist[maxDistance, maxDistance] = true;
        breath.steps[maxDistance, maxDistance] = 0;
        breath.distance[maxDistance, maxDistance] = 0;
        breath.direction[maxDistance, maxDistance] = 0;

        List<ThingController> monsters = new List<ThingController>();

        int maxStepDistance = maxDistance * 10;

        while (openList.ItemCount > 0)
        {
            //get top of heap
            PathStep current = openList.RemoveFirst();
            int ax = current.position.x - StartPoint.x + maxDistance;
            int ay = current.position.y - StartPoint.y + maxDistance;
            int currentDistance = breath.distance[ax, ay];
            int currentSteps = breath.steps[ax, ay];
            closedCheck[ax, ay] = true;

            TheGrid.GetNearbyMonsters(current.position, 0).Perform((n) => { monsters.Add(n.Data); });

            if (cullByTrueDistance)
                if (currentDistance >= maxStepDistance)
                    continue;

            Vector3 currentHeat = GetHeat(current.position);

            //no propagation through solids
            if (currentHeat.x >= 1f)
                if (current.position != StartPoint)
                    continue;

            for (int i = 1; i < 9; i++)
            {
                Vec2I neighbor = current.position + Vec2I.directions[i];

                //calculate array position
                int arrayX = neighbor.x - StartPoint.x + maxDistance;
                int arrayY = neighbor.y - StartPoint.y + maxDistance;

                //cull disallowed
                if (AxMath.RogueDistance(neighbor, StartPoint) > maxDistance) continue;
                if (openCheck[arrayX, arrayY]) continue;
                if (closedCheck[arrayX, arrayY]) continue;

                if (!CanPath(neighbor))
                    continue;

                if (HasLedge(current.position, neighbor, false))
                    continue;

                //calculate cost
                int travelCost = current.travelCost + GetTravelCost(neighbor);
                int heuristic = AxMath.WeightedDistance(neighbor, StartPoint);
                int fullCost = travelCost + heuristic;

                //reverse direction to point towards the source of breath
                int p = i + 4;
                if (p > 8) p -= 8;

                //check if we can update parent to better 
                if (openCheck[arrayX, arrayY])
                    if (openArray[arrayX, arrayY].travelCost > travelCost)
                    {
                        openArray[arrayX, arrayY].travelCost = travelCost;
                        openArray[arrayX, arrayY].heuristic = heuristic;
                        openArray[arrayX, arrayY].fullCost = fullCost;
                        breath.direction[arrayX, arrayY] = p;
                        breath.distance[arrayX, arrayY] = currentDistance + StepDistance(i);
                        breath.steps[arrayX, arrayY] = currentSteps + 1;
                        openList.UpdateItem(openArray[arrayX, arrayY]);
                        continue;
                    }
                    else
                        continue;

                //priority sorted by heap
                PathStep step = new PathStep(neighbor, travelCost, heuristic);
                openList.Add(step);
                openArray[arrayX, arrayY] = step;
                openCheck[arrayX, arrayY] = true;

                breath.exist[arrayX, arrayY] = true;
                breath.direction[arrayX, arrayY] = p;
                breath.distance[arrayX, arrayY] = currentDistance + StepDistance(i);
                breath.steps[arrayX, arrayY] = currentSteps + 1;
            }
        }

        monstersList = monsters;
    }

    // tries to make pathfinding more natural like, instead of hugging walls the object tries to move toward corners and doors.
    public static void NaturalizePath(ref Vec2I[] path, int maxSteps)
    {
        if (path.Length < 3)
            return;

        if (maxSteps < 2)
            return;

        SingleLinkedList<Vec2I> naturalized = new SingleLinkedList<Vec2I>();

        int s = 0;
        int e = 2;

        Vec2I[] lastLine = new Vec2I[0];

        while (e < path.Length)
        {
            int steps = 0;

        again:
            Vec2I[] line = AxMath.RogueLine(path[s], path[e]).ToArray();
            for (int i = 0; i < line.Length; i++)
                if (!CanPath(line[i]))
                    goto failed;

            if (e < path.Length - 1 && steps <= maxSteps)
            {
                lastLine = line;
                steps++;
                e++;
                goto again;
            }

        failed:
            if (e > s + 2)
            {
                for (int i = 0; i < lastLine.Length; i++)
                    naturalized.InsertBack(lastLine[i]);

                s = e;
                e = s + 2;
                continue;
            }

            naturalized.InsertBack(path[s]);
            s++;
            e = s + 2;
        }

        while (s < path.Length)
            naturalized.InsertBack(path[s++]);

        path = naturalized.ToArray();
    }
}
