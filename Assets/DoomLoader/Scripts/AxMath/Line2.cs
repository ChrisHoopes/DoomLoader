using UnityEngine;

public struct Line2
{
    public Vector2 start;
    public Vector2 end;

    public Vector2 center { get { return (start + end) * .5f; } }

    public Line2(Vector2 Start, Vector2 End)
    {
        start = Start;
        end = End;
    }
}
