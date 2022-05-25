using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates visualization sprites to help debugging
/// </summary>
public class DebugObjects : MonoBehaviour
{
    public static DebugObjects Instance;

    void Awake()
    {
        Instance = this;
    }

    public Sprite[] directionSprites = new Sprite[9];

    public static void DestroySprites()
    {
        foreach (GameObject debugSprite in Instance.DebugSprites)
            Destroy(debugSprite);

        Instance.DebugSprites.Clear();
    }

    [HideInInspector]
    public List<GameObject> DebugSprites = new List<GameObject>();

    /// <summary>
    /// Creates a common arrow
    /// </summary>
    public static GameObject CreateDirectionMarker(Vec2I position, int direction, Color color) { return CreateDirectionMarker(position.x, position.y, direction, color); }
    public static GameObject CreateDirectionMarker(int x, int y, int direction, Color color)
    {
        GameObject m = new GameObject("direction_marker");
        SpriteRenderer s = m.AddComponent<SpriteRenderer>();
        s.sprite = Instance.directionSprites[direction];
        s.color = color;
        m.transform.SetParent(Instance.transform);
        m.transform.position = new Vector3(x + .5f, 12, y + .5f);
        m.transform.rotation = Quaternion.Euler(90, 0, 0);
        Instance.DebugSprites.Add(m);

        return m;
    }

    [HideInInspector]
    public GameObject GhostCursor;
    public static Vec2I GhostCursorPos { get { return new Vec2I(Mathf.FloorToInt(Instance.GhostCursor.transform.position.x), Mathf.FloorToInt(Instance.GhostCursor.transform.position.z)); } }

    /// <summary>
    /// Draws arrows to show to the source of breath
    /// </summary>
    public static void DrawBreathArea(BreathArea area)
    {
        area.BreathList.Perform((n) => 
        {
            Instance.DebugSprites.Add(CreateDirectionMarker(n.Data.position, n.Data.direction, Color.white));
        });
    }

    private static GameObject heatmapObjects;

    /// <summary>
    /// Creates colored boxes to show the AI heatmap
    /// </summary>
    public static void DrawHeatmap()
    {
        if (heatmapObjects != null)
            Destroy(heatmapObjects);

        heatmapObjects = new GameObject("Heatmap");
        heatmapObjects.transform.SetParent(Instance.transform);

        for (int y = 0; y < AI.heatmap.GetLength(1); y++)
            for (int x = 0; x < AI.heatmap.GetLength(0); x++)
            {
                Vector3 heat = AI.heatmap[x, y];

                GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cell.transform.position = new Vector3(TheGrid.origoX + x, 10, TheGrid.origoY + y) + Vector3.one * .5f;
                cell.transform.SetParent(heatmapObjects.transform);
                MeshRenderer mr = cell.GetComponent<MeshRenderer>();
                MaterialPropertyBlock materialProperties = new MaterialPropertyBlock();
                mr.GetPropertyBlock(materialProperties);
                if (heat.x < 0 && heat.y < 0)
                    materialProperties.SetColor("_Color", Color.black);
                else if (heat.z >= 1f)
                    materialProperties.SetColor("_Color", Color.magenta);
                else if (heat.x == 0f && heat.y == 0f)
                    materialProperties.SetColor("_Color", Color.green);
                else if (heat.x >= 1f)
                    materialProperties.SetColor("_Color", Color.red);
                else if (heat.y > 0f)
                    materialProperties.SetColor("_Color", Color.Lerp(Color.yellow, Color.red, heat.y * .5f));
                else
                    materialProperties.SetColor("_Color", Color.Lerp(Color.green, Color.yellow, .5f));

                 mr.SetPropertyBlock(materialProperties);
            }
    }
}
