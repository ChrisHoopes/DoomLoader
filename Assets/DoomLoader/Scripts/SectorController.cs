using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// SectorController will always be in the sector's floor object
/// </summary>
public class SectorController : MonoBehaviour
{
    public Sector sector;

    //keeps track of things inside the sectors (used by lighting changes and elevators, etc)
    public List<ThingController> StaticThings = new List<ThingController>();
    public LinkedList<ThingController> DynamicThings = new LinkedList<ThingController>();

    MeshRenderer mr;

    public void Init()
    {
        mr = GetComponent<MeshRenderer>();

        ChangeBrightness(sector.brightness);

        if (sector.specialType == 0 && sector.tag == 0)
        {
            //disable static sectors update
            enabled = false;
            return;
        }

        if (sector.specialType == 0)
            return;

        switch (sector.specialType)
        {
            default:
                Debug.Log("Unknown sector type: " + sector.specialType);
                break;

            case 1: //blink random
                {
                    float random = UnityEngine.Random.Range(.2f, .8f);
                    float time = 0;
                    bool flip = false;
                    float original = sector.brightness;
                    float neighbor = original;
                    foreach (Sidedef s in sector.Sidedefs)
                        if (s.Other != null)
                            if (s.Other.Sector.brightness < neighbor)
                                neighbor = s.Other.Sector.brightness;

                    parameters = new object[5] { time, flip, original, neighbor, random };
                }
                UpdateActions.Add(new Action(() =>
                {
                    float time = (float)parameters[0];
                    time += Time.deltaTime;

                    if (time >= (float)parameters[4])
                    {
                        bool flip = (bool)parameters[1];
                        time = 0;
                        flip = !flip;

                        ChangeBrightness(flip ? (float)parameters[2] : (float)parameters[3]);
                        parameters[1] = flip;

                        parameters[4] = UnityEngine.Random.Range(.2f, .8f);
                    }
                    parameters[0] = time;
                }));
                break;
            case 2: //fast strobe
            case 4:
                {
                    float time = UnityEngine.Random.Range(0, .5f);
                    bool flip = false;
                    float original = sector.brightness;
                    float neighbor = original;
                    foreach (Sidedef s in sector.Sidedefs)
                        if (s.Other != null)
                            if (s.Other.Sector.brightness < neighbor)
                                neighbor = s.Other.Sector.brightness;

                    parameters = new object[4] { time, flip, original, neighbor };
                }
                UpdateActions.Add(new Action(() =>
                {
                    float time = (float)parameters[0];
                    time += Time.deltaTime;

                    bool flip = (bool)parameters[1];
                    if ((flip && time >= .15f) || time > .5f)
                    {
                        time = 0;
                        flip = !flip;

                        ChangeBrightness(flip ? (float)parameters[2] : (float)parameters[3]);
                        parameters[1] = flip;
                    }
                    parameters[0] = time;
                }));
                break;

            case 3: //slow strobe
                {
                    float time = UnityEngine.Random.Range(0, 1f);
                    bool flip = false;
                    float original = sector.brightness;
                    float neighbor = original;
                    foreach (Sidedef s in sector.Sidedefs)
                        if (s.Other != null)
                            if (s.Other.Sector.brightness < neighbor)
                                neighbor = s.Other.Sector.brightness;

                    parameters = new object[4] { time, flip, original, neighbor };
                }
                UpdateActions.Add(new Action(() =>
                {
                    float time = (float)parameters[0];
                    time += Time.deltaTime;

                    bool flip = (bool)parameters[1];
                    if ((flip && time > .15f) || time >= 1f)
                    {
                        time = 0;
                        flip = !flip;

                        ChangeBrightness(flip ? (float)parameters[2] : (float)parameters[3]);
                        parameters[1] = flip;
                    }
                    parameters[0] = time;
                }));
                break;

            //damage sectors, handled by PlayerThing
            case 5:
            case 7:
            case 16:
            case 11:
                break;

            case 8: //oscillates
                UpdateActions.Add(new Action(() =>
                {
                    ChangeBrightness(Mathf.Sin(Time.time * 4) * .25f + .75f);
                }));
                break;

            case 9: //secret area
                PlayerInfo.Instance.unfoundSecrets.Add(sector);
                break;

            case 12: //fast strobe, synchronized
                {
                    bool flip = false;
                    float original = sector.brightness;
                    float neighbor = original;
                    foreach (Sidedef s in sector.Sidedefs)
                        if (s.Other != null)
                            if (s.Other.Sector.brightness < neighbor)
                                neighbor = s.Other.Sector.brightness;

                    parameters = new object[3] { original, neighbor, flip };
                }
                UpdateActions.Add(new Action(() =>
                {
                    bool flip = Time.time % .65f > .5f;
                    if (flip != (bool)parameters[2])
                    {
                        parameters[2] = flip;
                        ChangeBrightness(flip ? (float)parameters[0] : (float)parameters[1]);
                    }
                }));
                break;

            case 13: //slow strobe, synchronized
                {
                    bool flip = false;
                    float original = sector.brightness;
                    float neighbor = original;
                    foreach (Sidedef s in sector.Sidedefs)
                        if (s.Other != null)
                            if (s.Other.Sector.brightness < neighbor)
                                neighbor = s.Other.Sector.brightness;

                    parameters = new object[3] { original, neighbor, flip };
                }
                UpdateActions.Add(new Action(() =>
                {
                    bool flip = Time.time % 1.15f > 1f;
                    if (flip != (bool)parameters[2])
                    {
                        parameters[2] = flip;
                        ChangeBrightness(flip ? (float)parameters[0] : (float)parameters[1]);
                    }
                }));
                break;

            case 17: //flicker randomly
                {
                    float random = UnityEngine.Random.Range(.05f, .15f);
                    float time = 0;
                    bool flip = false;
                    float original = sector.brightness;
                    float neighbor = original;
                    foreach (Sidedef s in sector.Sidedefs)
                        if (s.Other != null)
                            if (s.Other.Sector.brightness < neighbor)
                                neighbor = s.Other.Sector.brightness;

                    parameters = new object[5] { time, flip, original, neighbor, random };
                }
                UpdateActions.Add(new Action(() =>
                {
                    float time = (float)parameters[0];
                    time += Time.deltaTime;

                    if (time >= (float)parameters[4])
                    {
                        bool flip = (bool)parameters[1];
                        time = 0;
                        flip = !flip;

                        ChangeBrightness(flip ? (float)parameters[2] : (float)parameters[3]);
                        parameters[1] = flip;

                        parameters[4] = UnityEngine.Random.Range(.05f, .15f);
                    }
                    parameters[0] = time;
                }));
                break;
        }
    }

    object[] parameters = new object[0];
    List<Action> UpdateActions = new List<Action>();

    void Update()
    {
        foreach (Action action in UpdateActions)
            action.Invoke();
    }

    public void ChangeBrightness(float value)
    {
        sector.brightness = value;

        MaterialPropertyBlock materialProperties = new MaterialPropertyBlock();
        mr.GetPropertyBlock(materialProperties);
        materialProperties.SetColor("_SectorLight", Color.white * value);
        mr.SetPropertyBlock(materialProperties);

        //change ceiling brightness
        {
            MeshRenderer mr = sector.ceilingObject.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                mr.GetPropertyBlock(properties);
                properties.SetColor("_SectorLight", Color.white * value);
                mr.SetPropertyBlock(properties);
            }
        }

        foreach (ThingController tc in StaticThings)
            tc.SetBrightness(value);

        foreach (ThingController tc in DynamicThings)
            tc.SetBrightness(value);

        foreach (Sidedef s in sector.Sidedefs)
            foreach (GameObject gameObject in s.IsFront ? s.Line.FrontObjects : s.Line.BackObjects)
                if (gameObject != null)
                {
                    MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        MaterialPropertyBlock properties = new MaterialPropertyBlock();
                        mr.GetPropertyBlock(properties);
                        properties.SetColor("_SectorLight", Color.white * value);
                        mr.SetPropertyBlock(properties);
                    }
                }
    }
}
