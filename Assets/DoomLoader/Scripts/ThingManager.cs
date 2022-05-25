using UnityEngine;
using System.Collections.Generic;

public class ThingManager : MonoBehaviour 
{
    public static ThingManager Instance;

    public List<ThingController> _ThingPrefabs = new List<ThingController>();

    private Dictionary<int, GameObject> thingPrefabs = new Dictionary<int, GameObject>();

    void Awake()
    {
        Instance = this;

        foreach (ThingController tc in _ThingPrefabs)
            thingPrefabs.Add(tc.thingID, tc.gameObject);
    }

    public void CreateThings(bool deathmatch)
    {
        GameObject holder = new GameObject("MapThings");
        holder.transform.SetParent(transform);

        foreach(Thing t in MapLoader.things)
        {
            if (!deathmatch)
                if ((t.flags & (1 << 4)) != 0)
                    continue;

            if (!thingPrefabs.ContainsKey(t.thingType))
            {
                Debug.Log("Unknown thing type (" + t.thingType + ")");
                continue;
            }

            GameObject thingObject = Instantiate(thingPrefabs[t.thingType]);
            thingObject.transform.SetParent(holder.transform);

            ThingController tc = thingObject.GetComponent<ThingController>();
            if (tc != null)
            {
                tc.transform.position = new Vector3(t.posX, 0, t.posY); //height will be set by tc.Init()
                tc.transform.rotation = Quaternion.Euler(0, t.facing, 0);
                tc.Init();

                if ((t.flags & (1 << 3)) != 0)
                    if (tc.CurrentBehavior != null)
                        tc.CurrentBehavior.deaf = true;
            }
        }
    }
}
