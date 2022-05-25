using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Clips two polygons using Weiler-Atherton polygon intersection
/// </summary>
public static class PolygonIntersect
{
    #region Loopable List
    public static DeepPoint NextAfter(List<DeepPoint> list, int i)
    {
        return list[(i + 1) % list.Count];
    }

    public static int IndexAfter(List<DeepPoint> list, int i)
    {
        return (i + 1) % list.Count;
    }

    public static DeepPoint PrevBefore(List<DeepPoint> list, int i)
    {
        return list[((i - 1) + list.Count) % list.Count];
    }

    public static int PrevIndex(List<DeepPoint> list, int i)
    {
        return ((i - 1) + list.Count) % list.Count;
    }
    #endregion

    public static bool LineIntersection(DeepPoint start1, DeepPoint end1, DeepPoint start2, DeepPoint end2, out List<Vector2> output)
    {
        output = new List<Vector2>();
        float det;
        float A1, B1, C1;
        float A2, B2, C2;

        A1 = end1.p.y - start1.p.y;
        B1 = start1.p.x - end1.p.x;
        C1 = A1 * start1.p.x + B1 * start1.p.y;

        A2 = end2.p.y - start2.p.y;
        B2 = start2.p.x - end2.p.x;
        C2 = A2 * start2.p.x + B2 * start2.p.y;

        det = A1 * B2 - A2 * B1;

        if (det == 0)
        {
            float mTop = end1.p.y - start1.p.y;
            float mBot = end1.p.x - start1.p.x;

            float b1 = end1.p.y - (mTop * end1.p.x) / mBot;
            float b2 = end2.p.y - (mTop * end2.p.x) / mBot;

            if (b1 != b2) return false;

            bool overlap = false;

            //there will possibly be two points
            float cxMin = Mathf.Min(start2.p.x, end2.p.x);
            float cxMax = Mathf.Max(start2.p.x, end2.p.x);

            float cyMin = Mathf.Min(start2.p.y, end2.p.y);
            float cyMax = Mathf.Max(start2.p.y, end2.p.y);

            if (cxMin <= start1.p.x && start1.p.x <= cxMax
            && cyMin <= start1.p.y && start1.p.y <= cyMax)
            {
                //use start1
                output.Add(start1.p);
                overlap = true;
            }
            else
            {
                //use start2
                output.Add(start2.p);
            }

            if (cxMin <= end1.p.x && end1.p.x <= cxMax
                && cyMin <= end1.p.y && end1.p.y <= cyMax)
            {
                //use end1
                output.Add(end1.p);
                overlap = true;
            }
            else
            {
                //use end2
                output.Add(end2.p);
            }

            return overlap;
        }

        Vector2 i = new Vector2(B2 * C1 - B1 * C2, A1 * C2 - A2 * C1);

        i.x /= det;
        i.y /= det;

        float xMin = Mathf.Min(start2.p.x, end2.p.x);
        float xMax = Mathf.Max(start2.p.x, end2.p.x);

        float yMin = Mathf.Min(start2.p.y, end2.p.y);
        float yMax = Mathf.Max(start2.p.y, end2.p.y);

        float xMin2 = Mathf.Min(start1.p.x, end1.p.x);
        float xMax2 = Mathf.Max(start1.p.x, end1.p.x);

        float yMin2 = Mathf.Min(start1.p.y, end1.p.y);
        float yMax2 = Mathf.Max(start1.p.y, end1.p.y);

        if (xMin <= i.x && i.x <= xMax && yMin <= i.y && i.y <= yMax && xMin2 <= i.x && i.x <= xMax2 && yMin2 <= i.y && i.y <= yMax2)
        {
            output.Add(i);
            return true;
        }

        return false;
    }

    //
    public static void Clip(Vector2[] clip, Vector2[] shape, out List<List<Vector2>> result)
    {
        result = new List<List<Vector2>>();

        List<DeepPoint> deepShape = new List<DeepPoint>();
        List<DeepPoint> deepClip = new List<DeepPoint>();

        foreach (Vector2 v in shape)
            deepShape.Add(new DeepPoint(v, DeepPoint.PointType.Normal, DeepPoint.InOrOut(v, clip)));

        foreach (Vector2 v in clip)
            deepClip.Add(new DeepPoint(v, DeepPoint.PointType.Normal, DeepPoint.InOrOut(v, shape)));

        for (int i = 0; i < deepShape.Count; i++)
        {
            DeepPoint p1 = deepShape[i];
            DeepPoint p2 = NextAfter(deepShape, i);

            //check for intersections
            for (int j = 0; j < deepClip.Count; j++)
            {
                DeepPoint c1 = deepClip[j];
                DeepPoint c2 = NextAfter(deepClip, j);

                List<Vector2> interOutput;
                if (LineIntersection(p1, p2, c1, c2, out interOutput))
                {
                    foreach (Vector2 inter in interOutput)
                    {
                        //This ensures that we have the same intersection added to both (avoid precision errors)
                        DeepPoint intersection = new DeepPoint(inter, DeepPoint.PointType.Intersection, DeepPoint.PointStatus.Undetermined);

                        if (inter == p1.p || inter == p2.p || inter == c1.p || inter == c2.p)
                        {
                            if (inter == p1.p && inter == c1.p)
                            {
                                p1.overlap = true;
                                c1.overlap = true;
                                p1.type = DeepPoint.PointType.Intersection;
                                c1.type = DeepPoint.PointType.Intersection;
                                continue;
                            }
                            else if (inter != p2.p && inter != c1.p && inter != c2.p)
                            {
                                if (inter == p1.p)
                                {
                                    p1.overlap = true;
                                    p1.type = DeepPoint.PointType.Intersection;
                                }
                                else p1.intersections.Add(intersection);

                                c1.intersections.Add(intersection);
                            }
                        }
                        else
                        {
                            //TODO: if intersection is same as p1,p2,c1,c2 -> make a note of it?
                            p1.intersections.Add(intersection);
                            c1.intersections.Add(intersection);
                        }
                    }
                }
            }

            //sort intersections by distance to p1
            p1.SortIntersections();


            //loop through intersections between p1 and p2
            for (int j = 0; j < p1.intersections.Count; j++)
            {
                DeepPoint intersection = p1.intersections[j];

                //if there's a previous intersection
                if (j > 0)
                {
                    DeepPoint prev = p1.intersections[j - 1];
                    if (prev.status == DeepPoint.PointStatus.In) intersection.status = DeepPoint.PointStatus.Out; //set inter pStatus to Out
                    else intersection.status = DeepPoint.PointStatus.In; //set inter as In
                }
                else if (p1.status == DeepPoint.PointStatus.In) intersection.status = DeepPoint.PointStatus.Out; //set inter as Out
                else intersection.status = DeepPoint.PointStatus.In; //set inter as In
            }
        }

        //sort all intersections in clip
        for (int i = 0; i < deepClip.Count; i++)
        {
            deepClip[i].SortIntersections();
        }

        IntegrateIntersections(ref deepShape);
        IntegrateIntersections(ref deepClip);

        //Use these to jump from list to list
        Dictionary<Vector2, int> shapeIntersectionToClipIndex = new Dictionary<Vector2, int>();
        Dictionary<Vector2, int> clipIntersectionToShapeIndex = new Dictionary<Vector2, int>();
        BuildIntersectionMap(ref shapeIntersectionToClipIndex, deepShape, ref clipIntersectionToShapeIndex, deepClip);

        //start from entering points
        List<int> iEntering = new List<int>();

        //Get entering intersections
        for (int i = 0; i < deepShape.Count; i++)
        {
            DeepPoint point = deepShape[i];
            if (point.overlap || (point.type == DeepPoint.PointType.Intersection && point.status == DeepPoint.PointStatus.In))
                iEntering.Add(i);
        }

        List<List<DeepPoint>> output = new List<List<DeepPoint>>();
        List<DeepPoint> currentShape = new List<DeepPoint>();

        bool allEnteringAreOverlap = true;
        foreach (int i in iEntering)
        {
            if (!deepShape[i].overlap)
            {
                allEnteringAreOverlap = false;
                break;
            }
        }

        bool hasNonOverlapIntersections = false;

        foreach (DeepPoint p in deepShape)
        {
            if (!p.overlap && p.type == DeepPoint.PointType.Intersection)
            {
                hasNonOverlapIntersections = true;
                break;
            }
        }

        //handle special cases
        if ((iEntering.Count == 0 || allEnteringAreOverlap) && !hasNonOverlapIntersections)
        {
            bool allInside = true;
            foreach (DeepPoint p in deepShape)
            {
                if (p.status != DeepPoint.PointStatus.In && !p.overlap)
                {
                    allInside = false;
                    break;
                }
            }

            if (allInside)
            {
                foreach (DeepPoint p in deepShape)
                {
                    currentShape.Add(p);
                }
            }
            else
            {
                //check that deepClip are all inside
                allInside = true;
                foreach (DeepPoint p in deepClip)
                {
                    if (p.status != DeepPoint.PointStatus.In && !p.overlap)
                    {
                        allInside = false;
                        break;
                    }
                }

                if (allInside)
                {
                    foreach (DeepPoint p in deepClip)
                    {
                        currentShape.Add(p);
                    }
                }
                else return;
            }

            output.Add(currentShape);
            return;
        }

        //TODO: add method to ignore entering points that were included in an output shape already

        //go through all of our entering points
        for (int mainCount = 0; mainCount < iEntering.Count; mainCount++)
        {
            int goToIndex = iEntering[mainCount];

            bool complete = false;

            while (!complete)
            {
                //loop through all shape points starting at goToIndex
                for (int iCount = goToIndex; iCount < deepShape.Count + goToIndex; iCount++)
                {
                    int i = iCount % deepShape.Count;
                    DeepPoint p1 = deepShape[i];
                    DeepPoint p2 = NextAfter(deepShape, i);

                    if (p1.overlap)
                    {
                        DeepPoint prev = PrevBefore(deepShape, i);
                        if (prev.overlap || prev.status == DeepPoint.PointStatus.Out)
                            p1.tempStatus = DeepPoint.PointStatus.In;
                        else p1.tempStatus = DeepPoint.PointStatus.Out;
                    }
                    else p1.tempStatus = p1.status;

                    if (p2.overlap)
                    {
                        if (p1.overlap || p1.status == DeepPoint.PointStatus.Out)
                            p2.tempStatus = DeepPoint.PointStatus.In;
                        else p2.tempStatus = DeepPoint.PointStatus.Out;
                    }
                    else p2.tempStatus = p2.status;

                    if (p1.type == DeepPoint.PointType.Normal)
                    {
                        if (p1.tempStatus == DeepPoint.PointStatus.In)
                        {
                            //break when we get back to start
                            if (currentShape.Count > 0 && currentShape[0].p == p1.p)
                            {
                                complete = true;
                                break;
                            }

                            currentShape.Add(p1);

                            //point2 must be heading outwards
                            if (p2.type == DeepPoint.PointType.Intersection)
                            {
                                //go to clipPoints loop and start from intersection
                                //goToIndex = shapeIntersectionToClipIndex[p2.p] + 1;
                                //break;
                            }
                        }
                        //we don't care about point2 here
                        //if point1 is an outside normal point,
                        //	then point2 must either be an outside normal point OR an intersection going inwards.
                        //		The former doesn't not need to be handled, the latter will be handled upon looping

                    }
                    else //p1 is an intersection
                    {
                        //break when we get back to start
                        if (currentShape.Count > 0 && currentShape[0].p == p1.p)
                        {
                            complete = true;
                            break;
                        }

                        //we must add point 1 since it's on the border
                        currentShape.Add(p1);

                        //exiting
                        if (p1.tempStatus == DeepPoint.PointStatus.Out)
                        {
                            //go to clipPoints loop and start from after intersection;
                            goToIndex = (shapeIntersectionToClipIndex[p1.p] + 1) % deepClip.Count;
                            break;
                        }
                    }
                } //end deepShape for

                //break while loop if complete
                if (complete) break;

                //loop through all clip points starting at goToIndex
                //we should only get here from a go to from shapePoints
                for (int iCount = goToIndex; iCount < deepClip.Count + goToIndex; iCount++)
                {
                    int i = iCount % deepClip.Count;
                    DeepPoint p1 = deepClip[i];
                    DeepPoint p2 = NextAfter(deepClip, i);

                    if (p1.overlap)
                    {
                        DeepPoint prev = PrevBefore(deepClip, i);
                        if (prev.overlap || prev.status == DeepPoint.PointStatus.Out)
                            p1.tempStatus = DeepPoint.PointStatus.In;
                        else p1.tempStatus = DeepPoint.PointStatus.Out;
                    }
                    else p1.tempStatus = p1.status;

                    if (p2.overlap)
                    {
                        if (p1.overlap || p1.status == DeepPoint.PointStatus.Out)
                            p2.tempStatus = DeepPoint.PointStatus.In;
                        else p2.tempStatus = DeepPoint.PointStatus.Out;
                    }
                    else p2.tempStatus = p2.status;

                    if (p1.type == DeepPoint.PointType.Intersection)
                    {
                        //break when we get back to start
                        if (currentShape.Count > 0 && currentShape[0].p == p1.p)
                        {
                            complete = true;
                            break;
                        }

                        //we must add point 1 since it's on the border
                        currentShape.Add(p1);

                        //if it was going inwards
                        if (p1.tempStatus == DeepPoint.PointStatus.In)
                        {
                            //go to shapePoints loop and start from after point1
                            goToIndex = (clipIntersectionToShapeIndex[p1.p] + 1) % deepShape.Count;
                            break;
                        }
                    }
                    else //p1 is normal
                    {
                        if (p1.tempStatus == DeepPoint.PointStatus.In)
                        {
                            //break when we get back to start
                            if (currentShape.Count > 0 && currentShape[0].p == p1.p)
                            {
                                complete = true;
                                break;
                            }

                            //we must add point 1 since it's on the border
                            currentShape.Add(p1);
                        }
                    }
                } //end deepClip for
            }//end while loop

            output.Add(currentShape);
            currentShape = new List<DeepPoint>();
        }//end main for loop

        //remove duplicate points
        for (int iOutput = 0; iOutput < output.Count; iOutput++)
        {
            List<DeepPoint> points = output[iOutput];
            for (int i = 0; i < points.Count; i++)
            {
                //remove duplicates
                if (points[i].p == NextAfter(points, i).p)
                {
                    //remove current
                    points.RemoveAt(i);
                    i--;
                }
            }

            if (points.Count < 3)
            {
                output.Remove(points);
                iOutput--;
            }
        }

        foreach (List<DeepPoint> o in output)
        {
            List<Vector2> vertices = new List<Vector2>();

            foreach (DeepPoint p in o)
                vertices.Add(p.p);

            result.Add(vertices);
        }

    } //end doClip

    private static void IntegrateIntersections(ref List<DeepPoint> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            DeepPoint normal = list[i];
            for (int j = 0; j < normal.intersections.Count; j++)
            {
                DeepPoint intersection = normal.intersections[j];

                list.Insert(i + 1, intersection);
                i++;
            }
        }
    }

    private static void BuildIntersectionMap(ref Dictionary<Vector2, int> fromMap, List<DeepPoint> from, ref Dictionary<Vector2, int> toMap, List<DeepPoint> to)
    {
        for (int i = 0; i < from.Count; i++)
        {
            DeepPoint point = from[i];

            if (point.type == DeepPoint.PointType.Intersection)
            {
                //we can do both at once since we should have the same number of intersections

                for (int j = 0; j < to.Count; j++)
                {
                    if (to[j].p == point.p) fromMap.Add(point.p, j);
                }
                toMap.Add(point.p, i);
            }
        }
    }
}

public class DeepPoint
{
    public Vector2 p;
    public PointType type;
    public PointStatus status;
    public bool overlap = false;
    public PointStatus tempStatus;

    public List<DeepPoint> intersections;

    public enum PointType
    {
        Normal,
        Intersection
    }

    public enum PointStatus
    {
        In,
        Out,
        Undetermined
    }

    public DeepPoint() { }

    public DeepPoint(Vector2 point, PointType pType, PointStatus pStatus)
    {
        p = point;

        type = pType;
        status = pStatus;

        intersections = new List<DeepPoint>();
    }

    public void SortIntersections()
    {
        if (intersections.Count <= 1) return;

        //sort by closest to point = first
        intersections.Sort((p1, p2) =>
        {

            float d1 = DistanceSquared(p1, this);
            float d2 = DistanceSquared(p2, this);
            if (d1 < d2) return -1;
            else if (d1 > d2) return 1;
            else return 0;
        });
    }

    public static float DistanceSquared(DeepPoint p1, DeepPoint p2)
    {
        return (p1.p - p2.p).sqrMagnitude;
    }

    public static float Distance(Vector2 p1, Vector2 p2)
    {
        return (p1 - p2).magnitude;
    }

    public static PointStatus InOrOut(Vector2 point, Vector2[] shape)
    {
        //a point that we can guarantee is outside of our shape
        Vector2 outside = new Vector2(float.MaxValue, float.MaxValue);

        //find the leftmost and topmost bounds
        for (int i = 0; i < shape.Length; i++)
        {
            Vector2 p = shape[i];

            if (p.x < outside.x)
                outside.x = p.x;

            if (p.y < outside.y)
                outside.y = p.y;
        }

        //push them towards zero to avoid overflow errors
        outside.x -= 20;
        outside.y -= 20;

        HashSet<Vector2> intersections = new HashSet<Vector2>();

        int intersectionCount = 0;
        for (int i = 0; i < shape.Length; i++)
        {
            Vector2 c1 = shape[i];
            Vector2 c2 = shape[(i + 1) % shape.Length];

            if (Distance(point, c1) + Distance(point, c2) == Distance(c1, c2))
                return PointStatus.In; 

            float det;
            float A1, B1, C1;
            float A2, B2, C2;

            A1 = c2.y - c1.y;
            B1 = c1.x - c2.x;
            C1 = A1 * c1.x + B1 * c1.y;

            A2 = outside.y - point.y;
            B2 = point.x - outside.x;
            C2 = A2 * point.x + B2 * point.y;

            det = A1 * B2 - A2 * B1;
            if (det == 0)
                return PointStatus.Undetermined;

            Vector2 intersect = new Vector2(B2 * C1 - B1 * C2, A1 * C2 - A2 * C1);
            intersect.x /= det;
            intersect.y /= det;

            float xMin = Mathf.Min(c1.x, c2.x);
            float xMax = Mathf.Max(c1.x, c2.x);

            float yMin = Mathf.Min(c1.y, c2.y);
            float yMax = Mathf.Max(c1.y, c2.y);

            float xMin2 = Mathf.Min(point.x, outside.x);
            float xMax2 = Mathf.Max(point.x, outside.x);

            float yMin2 = Mathf.Min(point.y, outside.y);
            float yMax2 = Mathf.Max(point.y, outside.y);

            if (xMin <= intersect.x && intersect.x <= xMax
                && yMin <= intersect.y && intersect.y <= yMax
                && xMin2 <= intersect.x && intersect.x <= xMax2
                && yMin2 <= intersect.y && intersect.y <= yMax2
                && !intersections.Contains(intersect))
            {
                intersectionCount++;
                intersections.Add(intersect);
            }
        }

        if (intersectionCount % 2 == 0)
            return PointStatus.Out;
        else
            return PointStatus.In;
    }
}