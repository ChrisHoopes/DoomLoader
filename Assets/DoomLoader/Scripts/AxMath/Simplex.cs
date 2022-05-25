using UnityEngine;
using System.Collections.Generic;

public class CollidableShape
{
    public Vector2 position;
    public float scale;

    public class ConvexShape
    {
        public List<Vector2> ShapePoints = new List<Vector2>();
    }

    List<ConvexShape> ConvexShapes = new List<ConvexShape>();
    List<Vector2> CornerPoints = new List<Vector2>();

    public class Simplex
    {
        public struct Edge
        {
            public Line2 EdgeLine;
            public Vector2 Normal;
            public Vector2 Center;
        }

        public Vector2 ShapeCenter;
        public List<Edge> Edges = new List<Edge>();

        public bool IsPointInShape(Vector2 point)
        {
            foreach (Edge edge in Edges)
                if (Vector2.Dot((point - edge.Center).normalized, edge.Normal) > 0)
                    return false;

            return true;
        }
    }

    private class Corner
    {
        public Vector2 Position;
        public Vector2 RightPosition;
        public Vector2 LeftPosition;
        public Vector2 RightNormal;
        public Vector2 LeftNormal;
        public float Atan2;

        public bool CanSeePoint(Vector2 point)
        {
            Vector2 pointDirection = (point - Position).normalized;
            if (Vector2.Dot(RightNormal, pointDirection) > 0 || Vector2.Dot(LeftNormal, pointDirection) > 0) return true;
            else return false;
        }
    }

    public float CollisionNearDistance;

    private List<Corner> Corners = new List<Corner>();
    private List<Simplex> Shapes = new List<Simplex>();

    //gets the averaged center of shapes
    public Vector2 GetCenter
    {
        get
        {
            if (Shapes.Count == 0)
                return Vector2.zero;
            else if (Shapes.Count == 1)
                return Shapes[0].ShapeCenter;
            else
            {
                Vector2 center = new Vector2();
                foreach (Simplex shape in Shapes) center += shape.ShapeCenter;
                center /= Shapes.Count;
                return center;
            }
        }
    }

    private void InitializeShapes()
    {
        //clear previous
        Shapes.Clear();
        Corners.Clear();

        foreach (ConvexShape shape in ConvexShapes)
        {
            //can have a shape with only two points, this will result into two edges: the front and the back
            if (shape.ShapePoints.Count < 2) continue;

            //initialize shape
            Simplex simplex = new Simplex();
            Vector2 center = new Vector2();
            Vector2 previousPoint = position + shape.ShapePoints[shape.ShapePoints.Count - 1] * scale;

            foreach (Vector2 shapePoint in shape.ShapePoints)
            {
                //calculate main properties
                Simplex.Edge edge = new Simplex.Edge();
                edge.EdgeLine = new Line2(previousPoint, position + shapePoint * scale);
                edge.Center = (edge.EdgeLine.start + edge.EdgeLine.end) / 2;

                //rotate edge 90 degrees to get normal
                Vector2 edgeAsNormal = (edge.EdgeLine.end - edge.EdgeLine.start).normalized;
                edge.Normal = new Vector2(-edgeAsNormal.y, edgeAsNormal.x);

                //add to center collection
                center += shapePoint * scale;

                //next edge
                previousPoint = edge.EdgeLine.end;
                simplex.Edges.Add(edge);
            }

            //average center
            simplex.ShapeCenter = position + (center / shape.ShapePoints.Count);
            Shapes.Add(simplex);
        }

        //create corners
        if (CornerPoints.Count > 1)
        {
            List<Corner> tempCorners = new List<Corner>();

            Vector2 previousCorner = position + CornerPoints[CornerPoints.Count - 1] * scale;
            for (int i = 0; i < CornerPoints.Count; i++)
            {
                //form two walls
                Corner corner = new Corner();
                corner.LeftPosition = previousCorner;
                corner.Position = position + CornerPoints[i] * scale;
                corner.RightPosition = position + CornerPoints[i == CornerPoints.Count - 1 ? 0 : i + 1] * scale;

                //rotate edges 90 degrees counter-clockwise to get normals
                Vector2 leftAsNormal = (corner.Position - corner.LeftPosition).normalized;
                Vector2 rightAsNormal = (corner.RightPosition - corner.Position).normalized;
                corner.LeftNormal = new Vector2(-leftAsNormal.y, leftAsNormal.x);
                corner.RightNormal = new Vector2(-rightAsNormal.y, rightAsNormal.x);

                //calculate ATan
                Vector2 positionAsNormal = (corner.Position - GetCenter).normalized;
                corner.Atan2 = Mathf.Atan2(positionAsNormal.x, positionAsNormal.y);

                //check if bigger than current bound
                float range = (corner.Position - GetCenter).sqrMagnitude;
                if (range > CollisionNearDistance)
                    CollisionNearDistance = range;

                //add to collection
                previousCorner = corner.Position;
                tempCorners.Add(corner);
            }

            //reorder corners to start from lowest ATan value
            float lowestATan = (float)Mathf.PI;
            int startPos = 0;
            for (int i = 0; i < tempCorners.Count; i++)
                if (tempCorners[i].Atan2 < lowestATan)
                {
                    lowestATan = tempCorners[i].Atan2;
                    startPos = i;
                }
            while (tempCorners.Count > 0)
            {
                Corners.Add(tempCorners[startPos]);
                tempCorners.RemoveAt(startPos);
                if (startPos >= tempCorners.Count)
                    startPos = 0;
            }
        }
    }

    //used to prevent objects overlapping
    public bool IsPointInShapes(Vector2 point)
    {
        foreach (Simplex shape in Shapes)
            if (shape.IsPointInShape(point))
                return true;

        return false;
    }

    //main method for workers
    public bool IsPointInsideCorners(Vector2 point)
    {
        foreach (Corner corner in Corners)
            if (Vector2.Dot(corner.RightNormal, (point - corner.Position).normalized) > 0)
                return false;

        return Corners.Count > 0;
    }

    //tests if two collidables have intersecting shapes
    public bool ShapesCollide(CollidableShape targetEntity)
    {
        foreach (Simplex shape in Shapes)
            foreach (Simplex otherShape in targetEntity.Shapes)
            {
                //fast early exit, also needed to check if either shape is fully inside the other
                if (shape.IsPointInShape(otherShape.ShapeCenter)) return true;
                if (otherShape.IsPointInShape(shape.ShapeCenter)) return true;

                foreach (Simplex.Edge edge in shape.Edges)
                    foreach (Simplex.Edge otherEdge in otherShape.Edges)
                        if (AxMath.LinesIntercect(edge.EdgeLine, otherEdge.EdgeLine))
                            return true;
            }

        return false;
    }

    //----------------\\
    // Corner Walking \\
    //----------------\\
    public bool GetATanCorners(Vector2 point, out Vector2 leftCorner, out Vector2 rightCorner)
    {
        leftCorner = GetCenter;
        rightCorner = GetCenter;

        //avoid NaN and out of array
        if (point == GetCenter) return false;
        if (Corners.Count < 2) return false;

        Vector2 pointDirection = (point - GetCenter);
        float pointATan = Mathf.Atan2(pointDirection.x, pointDirection.y);
        foreach (Corner corner in Corners)
            if (pointATan < corner.Atan2)
            {
                rightCorner = corner.Position;
                leftCorner = corner.LeftPosition;
                return true;
            }

        //loop over
        leftCorner = Corners[0].LeftPosition;
        rightCorner = Corners[0].Position;
        return true;
    }

    public Vector2 GetBestNextCorner(Vector2 from, Vector2 target)
    {
        //non-collidable, keep moving
        if (Corners.Count == 0) return target;

        //find closest corner
        Corner closestCorner = Corners[0];
        float bestDistance = 1000f;
        foreach (Corner corner in Corners)
        {
            Vector2 selfDifference = corner.Position - from;
            float distanceToSelf = selfDifference.sqrMagnitude;

            if (distanceToSelf < bestDistance)
            {
                closestCorner = corner;
                bestDistance = distanceToSelf;
            }
        }

        //can detach directly
        if (closestCorner.CanSeePoint(target))
            return target;

        //are we on same sector
        Vector2 leftCornerTo, rightCornerTo;
        if (GetATanCorners(target, out leftCornerTo, out rightCornerTo))
            if (closestCorner.Position == leftCornerTo || closestCorner.Position == rightCornerTo)
                return target;

        //evaluate neighbors
        float closeDistance = (target - closestCorner.Position).sqrMagnitude;
        float leftDistance = (target - closestCorner.LeftPosition).sqrMagnitude;
        float rightDistance = (target - closestCorner.RightPosition).sqrMagnitude;

        //check if we stand on best corner
        if (closeDistance <= leftDistance && closeDistance <= rightDistance)
            return target;

        //move to better neighbor
        if (leftDistance < rightDistance)
            return closestCorner.LeftPosition;
        else
            return closestCorner.RightPosition;
    }

    public Vector2 GetBestInitialCorner(Vector2 from, Vector2 target)
    {
        //non-collidable, keep moving
        if (Corners.Count == 0) return target;

        //select corners by sector
        Vector2 leftCornerFrom, rightCornerFrom, leftCornerTarget, rightCornerTarget;
        if (GetATanCorners(from, out leftCornerFrom, out rightCornerFrom))
        {
            //get difference to corners
            Vector2 fromLeftToTarget = target - leftCornerFrom;
            Vector2 fromRightToTarget = target - rightCornerFrom;

            if (GetATanCorners(target, out leftCornerTarget, out rightCornerTarget))
            {
                //test to see if on same sector 
                if (leftCornerFrom == leftCornerTarget && rightCornerFrom == rightCornerTarget)
                    return target;

                //to avoid NaN
                if (leftCornerFrom != from && rightCornerFrom != from && leftCornerTarget != target && rightCornerTarget != target)
                    if (IsPointInsideCorners(from))
                    {
                        //turn to normals and rotate counter-clockwise to get frustum
                        Vector2 leftCorner = (leftCornerFrom - from).normalized;
                        Vector2 leftFrustum = new Vector2(-leftCorner.y, leftCorner.x);
                        Vector2 targetLeft = fromLeftToTarget.normalized;
                        Vector2 rightCorner = (rightCornerFrom - from).normalized;
                        Vector2 rightFrustum = new Vector2(-rightCorner.y, rightCorner.x);
                        Vector2 targetRight = fromRightToTarget.normalized;

                        //can we exit through frustum, guaranteed handedness since we are inside shape
                        if (Vector2.Dot(targetLeft, leftFrustum) <= 0 && 0 <= Vector2.Dot(targetRight, rightFrustum))
                            return target;
                    }

                //select shared corner
                if (leftCornerFrom == rightCornerTarget) return leftCornerFrom;
                if (rightCornerFrom == leftCornerTarget) return rightCornerFrom;
            }

            //return better corner
            return (fromLeftToTarget.sqrMagnitude < fromRightToTarget.sqrMagnitude) ? leftCornerFrom : rightCornerFrom;
        }

        //we are on center or not enough corners
        return target;
    }
}