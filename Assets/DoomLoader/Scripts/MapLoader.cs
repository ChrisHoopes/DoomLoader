using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public static class MapLoader 
{
    public static string CurrentMap;

    public static bool IsSkyTexture(string textureName)
    {
        if (textureName == "F_SKY1")
            return true;

        return false;
    }

    public const int sizeDividor = 32;
    public const int flatUVdividor = 64 / sizeDividor; //all Doom flats are 64x64
    public const float _4units = 4f / sizeDividor;
    public const float _8units = 8f / sizeDividor;
    public const float _16units = 16f / sizeDividor;
    public const float _24units = 24f / sizeDividor;
    public const float _32units = 32f / sizeDividor;
    public const float _64units = 64f / sizeDividor;
    public const float _96units = 96f / sizeDividor;
    public const float _128units = 128f / sizeDividor;

    public static List<Vertex> vertices;
    public static List<Sector> sectors;
    public static List<Linedef> linedefs;
    public static List<Sidedef> sidedefs;
    public static List<Thing> things;

    public static Lump things_lump;
    public static Lump linedefs_lump;
    public static Lump sidedefs_lump;
    public static Lump vertexes_lump;
    public static Lump segs_lump;
    public static Lump ssectors_lump;
    public static Lump nodes_lump;
    public static Lump sectors_lump;
    public static Lump reject_lump;
    public static Lump blockmap_lump;

    public static int minX = int.MaxValue;
    public static int maxX = int.MinValue;
    public static int minY = int.MaxValue;
    public static int maxY = int.MinValue;
    public static int minZ = int.MaxValue;
    public static int maxZ = int.MinValue;

    public static int sizeX = 0;
    public static int sizeY = 0;
    public static int sizeZ = 0;

    public static void Unload()
    {
        if (string.IsNullOrEmpty(CurrentMap))
            return;

        foreach (Vertex v in vertices)
        {
            v.Linedefs.Clear();
        }

        Sector.TaggedSectors = new Dictionary<int, List<Sector>>();
        foreach (Sector s in sectors)
        {
            s.Sidedefs.Clear();
            s.triangles.Clear();
        }

        foreach (Linedef l in linedefs)
        {
            l.start = null;
            l.end = null;
            l.Front = null;
            l.Back = null;
        }

        foreach (Sidedef s in sidedefs)
        {
            s.Line = null;
            s.Sector = null;
        }

        AI.heatmap = new Vector3[0, 0];

        for (int y = 0; y < TheGrid.sizeY; y++)
            for (int x = 0; x < TheGrid.sizeX; x++)
            {
                foreach (Triangle t in TheGrid.triangles[x, y]) t.sector = null;
                TheGrid.triangles[x, y].Clear();
                TheGrid.sectors[x, y].Clear();
                TheGrid.linedefs[x, y].Clear();
                TheGrid.decorThings[x, y].Clear();
                TheGrid.neutralThings[x, y].Clear();
                TheGrid.monsterThings[x, y].Clear();
                TheGrid.itemThings[x, y].Clear();
            }

        TheGrid.triangles = new List<Triangle>[0, 0];
        TheGrid.sizeX = 0;
        TheGrid.sizeY = 0;

        things_lump = null;
        linedefs_lump = null;
        sidedefs_lump = null;
        vertexes_lump = null;
        segs_lump = null;
        ssectors_lump = null;
        nodes_lump = null;
        sectors_lump = null;
        reject_lump = null;
        blockmap_lump = null;

        vertices.Clear();
        sectors.Clear();
        linedefs.Clear();
        sidedefs.Clear();
        things.Clear();

        for (int c = 0; c < GameManager.Instance.transform.childCount; c++)
            GameObject.Destroy(GameManager.Instance.transform.GetChild(c).gameObject);

        for (int c = 0; c < GameManager.Instance.TemporaryObjectsHolder.childCount; c++)
            GameObject.Destroy(GameManager.Instance.TemporaryObjectsHolder.GetChild(c).gameObject);

        GameManager.Instance.Player[0].LastSector = null;
        GameManager.Instance.Player[0].currentSector = null;

        PlayerInfo.Instance.unfoundSecrets = new List<Sector>();
        PlayerInfo.Instance.foundSecrets = new List<Sector>();

        CurrentMap = "";
    }

    public static bool Load(string mapName)
    {
        if (WadLoader.lumps.Count == 0)
        {
            Debug.LogError("MapLoader: Load: WadLoader.lumps == 0");
            return false;
        }

        //lumps
        {
            int i = 0;
            foreach (Lump l in WadLoader.lumps)
            {
                if (l.lumpName.Equals(mapName))
                    goto found;

                i++;
            }

            Debug.LogError("MapLoader: Load: Could not find map \"" + mapName + "\"");
            return false;

        found:
            things_lump = WadLoader.lumps[++i];
            linedefs_lump = WadLoader.lumps[++i];
            sidedefs_lump = WadLoader.lumps[++i];
            vertexes_lump = WadLoader.lumps[++i];
            segs_lump = WadLoader.lumps[++i];
            ssectors_lump = WadLoader.lumps[++i];
            nodes_lump = WadLoader.lumps[++i];
            sectors_lump = WadLoader.lumps[++i];
            reject_lump = WadLoader.lumps[++i];
            blockmap_lump = WadLoader.lumps[++i];
        }

        #region Hotfixes
        //fixes a small mishap by original level developer, sector 7 is not closed
        if (mapName == "E1M3")
        {
            //linedef 933 second vertex will be changed to vertex index 764
            linedefs_lump.data[13064] = 252;
            linedefs_lump.data[13065] = 2;
        }

        //the original method to handle the tracking of boss deaths was hardcoded, we gonna
        //add a linedef to one of the walls of the pentagram. That way we can easily add
        //the wanted behavior in similar manner to all others.
        if (mapName == "E1M8")
        {
            //linedef 104 type will be changed to 666
            linedefs_lump.data[1462] = 154;
            linedefs_lump.data[1463] = 2;
        }
        #endregion

        //things
        {
            int num = things_lump.data.Length / 10;
            things = new List<Thing>(num);

            for (int i = 0, p = 0; i < num; i++)
            {
                short x = ByteReader.ReadShort(things_lump.data, ref p);
                short y = ByteReader.ReadShort(things_lump.data, ref p);
                int facing = ByteReader.ReadInt16(things_lump.data, ref p);
                int thingtype = ByteReader.ReadInt16(things_lump.data, ref p);
                int flags = ByteReader.ReadInt16(things_lump.data, ref p);

                things.Add(new Thing(x, y, facing, thingtype, flags));
            }
        }

        //vertices
        {
            int num = vertexes_lump.data.Length / 4;
            vertices = new List<Vertex>(num);

            for (int i = 0, p = 0; i < num; i++)
            {
                short x = ByteReader.ReadShort(vertexes_lump.data, ref p);
                short y = ByteReader.ReadShort(vertexes_lump.data, ref p);

                vertices.Add(new Vertex(x, y));

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        //sectors
        {
            int num = sectors_lump.data.Length / 26;
            sectors = new List<Sector>(num);

            for (int i = 0, p = 0; i < num; i++)
            {
                short hfloor = ByteReader.ReadShort(sectors_lump.data, ref p);
                short hceil = ByteReader.ReadShort(sectors_lump.data, ref p);

                string tfloor = ByteReader.ReadName8(sectors_lump.data, ref p);
                string tceil = ByteReader.ReadName8(sectors_lump.data, ref p);

                int bright = ByteReader.ReadInt16(sectors_lump.data, ref p);
                int special = ByteReader.ReadInt16(sectors_lump.data, ref p);
                int tag = ByteReader.ReadInt16(sectors_lump.data, ref p);

                sectors.Add(new Sector(hfloor, hceil, tfloor, tceil, special, tag, bright));

                if (hfloor < minZ) minZ = hfloor;
                if (hceil > maxZ) maxZ = hceil;
            }
        }

        //sidedefs
        {
            int num = sidedefs_lump.data.Length / 30;
            sidedefs = new List<Sidedef>(num);

            for (int i = 0, p = 0; i < num; i++)
            {
                short offsetx = ByteReader.ReadShort(sidedefs_lump.data, ref p);
                short offsety = ByteReader.ReadShort(sidedefs_lump.data, ref p);

                string thigh = ByteReader.ReadName8(sidedefs_lump.data, ref p);
                string tlow = ByteReader.ReadName8(sidedefs_lump.data, ref p);
                string tmid = ByteReader.ReadName8(sidedefs_lump.data, ref p);

                int sector = ByteReader.ReadInt16(sidedefs_lump.data, ref p);

                sidedefs.Add(new Sidedef(sectors[sector], offsetx, offsety, thigh, tlow, tmid, i));
            }
        }

        //linedefs
        {
            int num = linedefs_lump.data.Length / 14;
            linedefs = new List<Linedef>(num);

            for (int i = 0, p = 0; i < num; i++)
            {
                int v1 = ByteReader.ReadInt16(linedefs_lump.data, ref p);
                int v2 = ByteReader.ReadInt16(linedefs_lump.data, ref p);
                int flags = ByteReader.ReadInt16(linedefs_lump.data, ref p);
                int action = ByteReader.ReadInt16(linedefs_lump.data, ref p);
                int tag = ByteReader.ReadInt16(linedefs_lump.data, ref p);
                int s1 = ByteReader.ReadInt16(linedefs_lump.data, ref p);
                int s2 = ByteReader.ReadInt16(linedefs_lump.data, ref p);

                Linedef line = new Linedef(vertices[v1], vertices[v2], flags, action, tag);
                linedefs.Add(line);

                if (s1 != ushort.MaxValue)
                    sidedefs[s1].SetLine(line, true);

                if (s2 != ushort.MaxValue)
                    sidedefs[s2].SetLine(line, false);
            }
        }

        //SKY FIX
        {
            foreach (Linedef l in linedefs)
            {
                if (l.Back == null)
                    continue;

                if (IsSkyTexture(l.Front.Sector.ceilingTexture))
                    if (IsSkyTexture(l.Back.Sector.ceilingTexture))
                    {
                        l.Front.tHigh = "F_SKY1";
                        l.Back.tHigh = "F_SKY1";
                    }

                if (IsSkyTexture(l.Front.Sector.floorTexture))
                    if (IsSkyTexture(l.Back.Sector.floorTexture))
                    {
                        l.Front.tLow = "F_SKY1";
                        l.Back.tLow = "F_SKY1";
                    }
            }
        }

        //modify geometry to accomodate expected changes
        foreach (Linedef l in linedefs)
        {
            if (l.lineType == 0)
                continue;

            switch (l.lineType)
            {
                default:
                    break;

                //common doors
                case 1:
                case 26:
                case 27:
                case 28:
                case 31:
                case 32:
                case 33:
                case 34:
                case 46:
                case 86:
                    {
                        if (l.Back != null)
                            if (l.Back.Sector.maximumCeilingHeight == l.Back.Sector.ceilingHeight
                                || l.Front.Sector.ceilingHeight - _4units < l.Back.Sector.maximumCeilingHeight)
                                l.Back.Sector.maximumCeilingHeight = l.Front.Sector.ceilingHeight - _4units;
                    }
                    break;

                //doors that start open
                case 16:
                case 76:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                            sector.minimumCeilingHeight = sector.floorHeight;
                    }
                    break;

                //remote doors
                case 2:
                case 63:
                case 90:
                case 103:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                            foreach (Sidedef s in sector.Sidedefs)
                            {
                                if (s.Line.Front.Sector == sector)
                                    continue;

                                if (sector.maximumCeilingHeight == sector.ceilingHeight ||
                                    s.Line.Front.Sector.ceilingHeight - _4units < sector.maximumCeilingHeight)
                                        sector.maximumCeilingHeight = s.Line.Front.Sector.ceilingHeight - _4units;
                            }
                    }
                    break;

                //raise floor to lowest neighbor ceiling
                case 5:
                case 91:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            float targetHeight = float.MaxValue;

                            foreach (Sidedef s in sector.Sidedefs)
                            {
                                if (s.Line.Front.Sector.ceilingHeight < targetHeight)
                                    targetHeight = s.Line.Front.Sector.ceilingHeight;

                                if (s.Line.Back != null)
                                    if (s.Line.Back.Sector.ceilingHeight < targetHeight)
                                        targetHeight = s.Line.Back.Sector.ceilingHeight;
                            }

                            if (targetHeight < float.MaxValue)
                                sector.maximumFloorHeight = targetHeight;
                        }
                    }
                    break;

                //stairbuilder, 8units
                case 7:
                case 8:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            List<Sector> stairs = new List<Sector>();
                            Sector targetSector = sector;

                            int count = 0;
                            bool failed = false;
                            while (!failed)
                            {
                                count++;
                                stairs.Add(targetSector);
                                targetSector.maximumFloorHeight = sector.floorHeight + _8units * count;

                                failed = true;
                                foreach (Sidedef s in targetSector.Sidedefs)
                                {
                                    if (s.Line.Back == null)
                                        continue;

                                    if (s.Line.Back.Sector == targetSector)
                                        continue;

                                    if (s.Line.Back.Sector.floorTexture != targetSector.floorTexture)
                                        continue;

                                    if (stairs.Contains(s.Line.Back.Sector))
                                        continue;

                                    targetSector = s.Line.Back.Sector;
                                    failed = false;
                                }
                            }
                        }
                    }
                    break;

                //floor raises to next higher
                case 18:
                case 20:
                case 22:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            float targetHeight = float.MaxValue;
                            foreach (Sidedef s in sector.Sidedefs)
                            {
                                if (s.Line.Back == null)
                                    continue;

                                if (s.Line.Back.Sector == s.Line.Front.Sector)
                                    continue;

                                if (s.Line.Front.Sector == sector)
                                    if (s.Line.Back.Sector.floorHeight > sector.floorHeight && s.Line.Back.Sector.floorHeight < targetHeight)
                                        targetHeight = s.Line.Back.Sector.floorHeight;

                                if (s.Line.Back.Sector == sector)
                                    if (s.Line.Front.Sector.floorHeight > sector.floorHeight && s.Line.Front.Sector.floorHeight < targetHeight)
                                        targetHeight = s.Line.Front.Sector.floorHeight;
                            }

                            if (targetHeight < float.MaxValue)
                                sector.maximumFloorHeight = targetHeight;
                        }
                    }
                    break;

                //lower floor to lowest neighbor floor
                case 23:
                case 38:
                case 60:
                case 82:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                            foreach (Sidedef s in sector.Sidedefs)
                            {
                                if (s.Line.Front.Sector.floorHeight < sector.minimumFloorHeight)
                                    sector.minimumFloorHeight = s.Line.Front.Sector.floorHeight;

                                if (s.Line.Back != null)
                                    if (s.Line.Back.Sector.floorHeight < sector.minimumFloorHeight)
                                        sector.minimumFloorHeight = s.Line.Back.Sector.floorHeight;
                            }
                    }
                    break;

                //lower floor to highest neighbor floor + 8
                case 36:
                case 70:
                case 71:
                case 98:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                            foreach (Sidedef s in sector.Sidedefs)
                            {
                                float highest = float.MinValue;

                                if (s.Line.Front.Sector.floorHeight + _8units < sector.floorHeight && s.Line.Front.Sector.floorHeight > highest)
                                    highest = s.Line.Front.Sector.floorHeight + _8units;

                                if (s.Line.Back != null)
                                    if (s.Line.Back.Sector.floorHeight + _8units < sector.minimumFloorHeight)
                                        highest = s.Line.Back.Sector.floorHeight + _8units;

                                if (highest > float.MinValue)
                                    sector.minimumFloorHeight = highest;
                            }
                    }
                    break;

                //common lifts
                case 62:
                case 88:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                            foreach (Sidedef s in sector.Sidedefs)
                            {
                                if (s.Line.Front.Sector.floorHeight < sector.minimumFloorHeight)
                                    sector.minimumFloorHeight = s.Line.Front.Sector.floorHeight;

                                if (s.Line.Back != null)
                                    if (s.Line.Back.Sector.floorHeight < sector.minimumFloorHeight)
                                        sector.minimumFloorHeight = s.Line.Back.Sector.floorHeight;
                            }
                    }
                    break;

                //nearby sector with tag 666 floor lowers to lowest neigbor floor (after bosses are dead)
                case 666:
                    {
                        Sector sector = null;

                        if (l.Front.Sector.tag == 666)
                            sector = l.Front.Sector;
                        if (l.Back != null)
                            if (l.Back.Sector.tag == 666)
                                sector = l.Back.Sector;

                        if (sector == null)
                        {
                            Debug.LogError("Linedef type 666 could not find nearby sector with tag 666!");
                            continue;
                        }

                        foreach (Sidedef s in sector.Sidedefs)
                        {
                            if (s.Line.Front.Sector.floorHeight < sector.minimumFloorHeight)
                                sector.minimumFloorHeight = s.Line.Front.Sector.floorHeight;

                            if (s.Line.Back != null)
                                if (s.Line.Back.Sector.floorHeight < sector.minimumFloorHeight)
                                    sector.minimumFloorHeight = s.Line.Back.Sector.floorHeight;
                        }
                    }
                    break;
            }
        }

        sizeX = maxX - minX;
        sizeY = maxY - minY;
        sizeZ = maxZ - minZ;

        CurrentMap = mapName;
        Debug.Log("Loaded map \"" + mapName + "\"");
        return true;
    }

    //this must be called after level geometry has been created
    public static void ApplyLinedefBehavior()
    {
        Transform holder = new GameObject("DynamicMeshes").transform;
        holder.transform.SetParent(GameManager.Instance.transform);

        int index = -1;
        foreach (Linedef l in linedefs)
        {
            index++;

            if (l.lineType == 0)
                continue;

            switch (l.lineType)
            {
                default:
                    Debug.Log("Linedef " + index + " has unknown type (" + l.lineType + ")");
                    break;

                //common door
                case 1:
                case 26:
                case 27:
                case 28:
                    {
                        if (l.TopFrontObject == null)
                            break;

                        if (l.Back == null)
                            break;

                        l.Back.Sector.ceilingObject.transform.SetParent(holder);

                        Door1Controller lc = l.TopFrontObject.AddComponent<Door1Controller>();

                        if (l.lineType == 26) lc.requiresKeycard = 0;
                        if (l.lineType == 27) lc.requiresKeycard = 1;
                        if (l.lineType == 28) lc.requiresKeycard = 2;

                        SlowRepeatableDoorController sc = l.Back.Sector.ceilingObject.GetComponent<SlowRepeatableDoorController>();
                        if (sc == null)
                            sc = l.Back.Sector.ceilingObject.AddComponent<SlowRepeatableDoorController>();

                        sc.Init(l.Back.Sector);
                        lc.sectorController = sc;

                        l.Back.Sector.Dynamic = true;
                    }
                    break;

                //single use door, walk trigger
                case 2:
                case 86: //repeatable trigger (no effect atm...)
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<SlowOneshotDoorController> linked = new List<SlowOneshotDoorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.ceilingObject.transform.SetParent(holder);

                            SlowOneshotDoorController sc = sector.ceilingObject.GetComponent<SlowOneshotDoorController>();
                            if (sc == null)
                            {
                                sc = sector.ceilingObject.gameObject.AddComponent<SlowOneshotDoorController>();
                                sc.Init(sector);
                            }
                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        CreateWalkTrigger(l, index, holder, (c) =>
                        {
                            PlayerThing player = c.GetComponent<PlayerThing>();

                            if (player == null)
                                return;

                            foreach (SlowOneshotDoorController lc in linked)
                            {
                                if (lc.CurrentState == SlowOneshotDoorController.State.Closed)
                                    lc.CurrentState = SlowOneshotDoorController.State.Opening;

                                //E1M4 has double door behaviors on the center column around blue keycard
                                SlowRepeatableDoorController sd = lc.targetSector.ceilingObject.GetComponent<SlowRepeatableDoorController>();
                                if (sd != null)
                                {
                                    sd.CurrentState = SlowRepeatableDoorController.State.Open;
                                    sd.enabled = false;
                                }
                            }
                        });
                    }
                    break;

                //raise floor to lowest neighbor ceiling, single use, walktrigger
                case 5:
                case 91: //repeatable trigger (no effect atm...)
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<Floor5Controller> linked = new List<Floor5Controller>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            Floor5Controller sc = sector.floorObject.GetComponent<Floor5Controller>();
                            if (sc == null)
                            {
                                sc = sector.floorObject.gameObject.AddComponent<Floor5Controller>();
                                sc.Init(sector);
                            }
                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        CreateWalkTrigger(l, index, holder, (c) =>
                        {
                            PlayerThing player = c.GetComponent<PlayerThing>();

                            if (player == null)
                                return;

                            foreach (Floor5Controller lc in linked)
                                if (lc.CurrentState == Floor5Controller.State.AtBottom)
                                    lc.CurrentState = Floor5Controller.State.Rising;
                        });
                    }
                    break;

                //stairbuilder, switch
                case 7:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<StairbuilderSlow> linked = new List<StairbuilderSlow>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            List<Sector> stairSectors = new List<Sector>();
                            Sector targetSector = sector;

                            int count = 0;
                            bool failed = false;
                            while (!failed)
                            {
                                count++;
                                stairSectors.Add(targetSector);
                                targetSector.Dynamic = true;
                                targetSector.floorObject.transform.SetParent(holder);

                                failed = true;
                                foreach (Sidedef s in targetSector.Sidedefs)
                                {
                                    if (s.Line.Back == null)
                                        continue;

                                    if (s.Line.Back.Sector == targetSector)
                                        continue;

                                    if (s.Line.Back.Sector.floorTexture != targetSector.floorTexture)
                                        continue;

                                    if (stairSectors.Contains(s.Line.Back.Sector))
                                        continue;

                                    targetSector = s.Line.Back.Sector;
                                    failed = false;
                                }
                            }

                            StairbuilderSlow sc = sector.floorObject.GetComponent<StairbuilderSlow>();
                            if (sc == null)
                            {
                                sc = sector.floorObject.gameObject.AddComponent<StairbuilderSlow>();
                                sc.Init(stairSectors);
                            }
                            linked.Add(sc);
                        }

                        CreateSwitch(l, () =>
                        {
                            foreach (StairbuilderSlow sc in linked)
                                if (sc.CurrentState == StairbuilderSlow.State.Waiting)
                                    sc.CurrentState = StairbuilderSlow.State.Active;
                        });
                    }
                    break;

                //stairbuilder, walktrigger
                case 8:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<StairbuilderSlow> linked = new List<StairbuilderSlow>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            List<Sector> stairSectors = new List<Sector>();
                            Sector targetSector = sector;

                            int count = 0;
                            bool failed = false;
                            while (!failed)
                            {
                                count++;
                                stairSectors.Add(targetSector);
                                targetSector.Dynamic = true;
                                targetSector.floorObject.transform.SetParent(holder);

                                failed = true;
                                foreach (Sidedef s in targetSector.Sidedefs)
                                {
                                    if (s.Line.Back == null)
                                        continue;

                                    if (s.Line.Back.Sector == targetSector)
                                        continue;

                                    if (s.Line.Back.Sector.floorTexture != targetSector.floorTexture)
                                        continue;

                                    if (stairSectors.Contains(s.Line.Back.Sector))
                                        continue;

                                    targetSector = s.Line.Back.Sector;
                                    failed = false;
                                }
                            }

                            StairbuilderSlow sc = sector.floorObject.GetComponent<StairbuilderSlow>();
                            if (sc == null)
                            {
                                sc = sector.floorObject.gameObject.AddComponent<StairbuilderSlow>();
                                sc.Init(stairSectors);
                            }
                            linked.Add(sc);
                        }

                        CreateWalkTrigger(l, index, holder, (c) =>
                        {
                            PlayerThing player = c.GetComponent<PlayerThing>();

                            if (player == null)
                                return;

                            foreach (StairbuilderSlow lc in linked)
                                if (lc.CurrentState == StairbuilderSlow.State.Waiting)
                                    lc.CurrentState = StairbuilderSlow.State.Active;
                        });
                    }
                    break;

                //donut, switch
                case 9:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<Sector> sectors = Sector.TaggedSectors[l.lineTag];
                        if (sectors.Count == 0)
                            break;

                        sectors[0].floorObject.transform.SetParent(holder);

                        Sector ringSector = null;
                        foreach (Sidedef s in sectors[0].Sidedefs)
                        {
                            if (s.Line.Back == null)
                                continue;

                            if (s.Line.Front.Sector == sectors[0])
                            {
                                ringSector = s.Line.Back.Sector;
                                ringSector.floorObject.transform.SetParent(holder);
                                break;
                            }

                            if (s.Line.Back.Sector == sectors[0])
                            {
                                ringSector = s.Line.Front.Sector;
                                ringSector.floorObject.transform.SetParent(holder);
                                break;
                            }
                        }

                        if (ringSector == null)
                            Debug.LogError("MapLoader: Donut9Controller: No ring sector found!");

                        Donut9SectorController sc = sectors[0].floorObject.gameObject.AddComponent<Donut9SectorController>();

                        CreateSwitch(l, () =>
                        {
                            if (sc.CurrentState == Donut9SectorController.State.Waiting)
                                sc.CurrentState = Donut9SectorController.State.Active;
                        });

                        sc.Init(sectors[0], ringSector);

                        sectors[0].Dynamic = true;
                        ringSector.Dynamic = true;
                    }
                    break;

                //level end switch
                case 11:
                    {
                        CreateSwitch(l, () =>
                        {
                            if (MapLoader.CurrentMap == "E1M9")
                            {
                                GameManager.Instance.ChangeMap = "E1M4";
                                return;
                            }

                            string currentMap = MapLoader.CurrentMap;
                            int mapNumber = int.Parse(currentMap.Substring(3, 1));
                            mapNumber++;

                            GameManager.Instance.ChangeMap = currentMap.Substring(0, 3) + mapNumber;
                        });
                    }
                    break;

                //door with 30s wait, starts open, walk trigger (this is known as a "delay trap")
                case 16:
                case 76:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<SlowOneshotDelayTrapDoorController> linked = new List<SlowOneshotDelayTrapDoorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.ceilingObject.transform.SetParent(holder);

                            SlowOneshotDelayTrapDoorController sc = sector.ceilingObject.GetComponent<SlowOneshotDelayTrapDoorController>();
                            if (sc == null)
                            {
                                sc = sector.ceilingObject.gameObject.AddComponent<SlowOneshotDelayTrapDoorController>();
                                sc.Init(sector);
                            }
                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        CreateWalkTrigger(l, index, holder, (c) =>
                        {
                            PlayerThing player = c.GetComponent<PlayerThing>();

                            if (player == null)
                                return;

                            foreach (SlowOneshotDelayTrapDoorController sc in linked)
                                if (sc.CurrentState == SlowOneshotDelayTrapDoorController.State.Open)
                                {
                                    sc.CurrentState = SlowOneshotDelayTrapDoorController.State.Closing;
                                    sc.waitTime = 30;
                                }
                        });
                    }
                    break;

                //raise floor to next, one use, switch
                case 18:
                    {
                        List<Floor20SectorController> linked = new List<Floor20SectorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            Floor20SectorController sc = sector.floorObject.GetComponent<Floor20SectorController>();
                            if (sc == null)
                            {
                                sc = sector.floorObject.gameObject.AddComponent<Floor20SectorController>();
                                sc.Init(sector);
                            }

                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        CreateSwitch(l, () =>
                        {
                            foreach (Floor20SectorController sectorController in linked)
                                if (sectorController.CurrentState == Floor20SectorController.State.AtBottom)
                                    sectorController.CurrentState = Floor20SectorController.State.Rising;
                        });

                    }
                    break;

                //raise floor to next, one use, switch
                case 20:
                    {
                        List<Floor20SectorController> linked = new List<Floor20SectorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            Floor20SectorController sc = sector.floorObject.GetComponent<Floor20SectorController>();
                            if (sc == null)
                            {
                                sc = sector.floorObject.gameObject.AddComponent<Floor20SectorController>();
                                sc.Init(sector);
                            }

                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        CreateSwitch(l, () => 
                        { 
                            foreach (Floor20SectorController sectorController in linked)
                                if (sectorController.CurrentState == Floor20SectorController.State.AtBottom)
                                    sectorController.CurrentState = Floor20SectorController.State.Rising;
                        });
                    }
                    break;

                //raise floor to next, one use, walk trigger
                case 22:
                    {
                        List<Floor20SectorController> linked = new List<Floor20SectorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            Floor20SectorController sc = sector.floorObject.GetComponent<Floor20SectorController>();
                            if (sc == null)
                            {
                                sc = sector.floorObject.gameObject.AddComponent<Floor20SectorController>();
                                sc.Init(sector);
                            }

                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        CreateWalkTrigger(l, index, holder, (c) =>
                        {
                            if (c.GetComponent<PlayerThing>() == null)
                                return;

                            foreach (Floor20SectorController sc in linked)
                            {
                                if (sc.CurrentState == Floor20SectorController.State.AtBottom)
                                    sc.CurrentState = Floor20SectorController.State.Rising;
                            }
                        });
                    }
                    break;

                //lower floor to lowest neighbor floor, switch
                case 23:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<Floor23SectorController> linked = new List<Floor23SectorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            Floor23SectorController sc = sector.floorObject.GetComponent<Floor23SectorController>();
                            if (sc == null)
                            {
                                sc = sector.floorObject.gameObject.AddComponent<Floor23SectorController>();
                                sc.Init(sector);
                            }
                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        CreateSwitch(l, () =>
                        {
                            foreach (Floor23SectorController sectorController in linked)
                                if (sectorController.CurrentState == Floor23SectorController.State.AtTop)
                                    sectorController.CurrentState = Floor23SectorController.State.Lowering;
                        });
                    }
                    break;

                //single use door, pokeable
                case 31:
                case 32:
                case 33:
                case 34:
                    {
                        if (l.TopFrontObject == null)
                            break;

                        if (l.Back == null)
                            break;

                        l.Back.Sector.ceilingObject.transform.SetParent(holder);

                        Door31Controller lc = l.TopFrontObject.AddComponent<Door31Controller>();
                        SlowOneshotDoorController sc = l.Back.Sector.ceilingObject.GetComponent<SlowOneshotDoorController>();

                        if (sc == null)
                            sc = l.Back.Sector.ceilingObject.AddComponent<SlowOneshotDoorController>();

                        if (l.lineType == 32) lc.requiresKeycard = 0;
                        if (l.lineType == 33) lc.requiresKeycard = 2;
                        if (l.lineType == 34) lc.requiresKeycard = 1;

                        sc.Init(l.Back.Sector);
                        lc.sectorController = sc;

                        l.Back.Sector.Dynamic = true;
                    }
                    break;

                //make sectors dark, walktrigger
                case 35:
                    {

                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        CreateWalkTrigger(l, index, holder, (c) =>
                        {
                            if (c.GetComponent<PlayerThing>() == null)
                                return;

                            foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                            {
                                sector.brightness = (float)35 / 255f;
                                sector.floorObject.ChangeBrightness(sector.brightness);
                            }
                        });
                    }
                    break;

                //lower floor to highest neighbor floor + 8, single use, walktrigger
                case 36:
                case 98:  //repeatable trigger (no effect atm...)
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<Floor36SectorController> linked = new List<Floor36SectorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            Floor36SectorController sc = sector.floorObject.GetComponent<Floor36SectorController>();
                            if (sc == null)
                            {
                                sc = sector.floorObject.gameObject.AddComponent<Floor36SectorController>();
                                sc.Init(sector);
                            }
                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        CreateWalkTrigger(l,index,holder, (c) =>
                        {
                            PlayerThing player = c.GetComponent<PlayerThing>();

                            if (player == null)
                                return;

                            foreach (Floor36SectorController lc in linked)
                                if (lc.CurrentState == Floor36SectorController.State.AtTop)
                                    lc.CurrentState = Floor36SectorController.State.Lowering;
                        });
                    }
                    break;

                //single use door, shootable
                case 46:
                    {
                        if (l.TopFrontObject == null)
                            break;

                        if (l.Back == null)
                            break;

                        l.Back.Sector.ceilingObject.transform.SetParent(GameManager.Instance.transform);

                        Door46Controller lc = l.TopFrontObject.AddComponent<Door46Controller>();
                        SlowOneshotDoorController sc = l.Back.Sector.ceilingObject.GetComponent<SlowOneshotDoorController>();
                        if (sc == null)
                            sc = l.Back.Sector.ceilingObject.AddComponent<SlowOneshotDoorController>();

                        sc.Init(l.Back.Sector);
                        lc.sectorController = sc;

                        l.Back.Sector.Dynamic = true;
                    }
                    break;

                //scroll animation, left
                case 48:
                    {
                        foreach (GameObject g in l.GameObjects)
                            if (g != null)
                                g.AddComponent<ScrollLeftAnimation>();
                    }
                    break;

                //secret level end switch
                case 51:
                    {
                        CreateSwitch(l, () =>
                        {
                            if (MapLoader.CurrentMap == "E1M3")
                                GameManager.Instance.ChangeMap = "E1M9";
                        });
                    }
                    break;

                //common lift, switch
                case 62:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<Slow3sLiftController> linked = new List<Slow3sLiftController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            Slow3sLiftController script = sector.floorObject.GetComponent<Slow3sLiftController>();
                            if (script == null)
                            {
                                script = sector.floorObject.gameObject.AddComponent<Slow3sLiftController>();
                                script.Init(sector);
                            }

                            linked.Add(script);
                            sector.Dynamic = true;
                        }

                        SwitchLinedefController sw = CreateSwitch(l, () => 
                        {
                            foreach (Slow3sLiftController liftController in linked)
                                if (liftController.CurrentState == Slow3sLiftController.State.AtTop)
                                    liftController.CurrentState = Slow3sLiftController.State.Lowering;
                        });

                        if (sw != null)
                        {
                            sw.Repeatable = true;

                            //all linked lifts must be at top for the switch to be activateable
                            sw.Prereq = new System.Func<bool>(() => 
                            {
                                foreach (Slow3sLiftController liftController in linked)
                                    if (liftController.CurrentState != Slow3sLiftController.State.AtTop)
                                        return false;

                                return true;
                            });

                            sw.AutoReturn = true;
                            sw.AutoReturnTime = 1f;
                        }
                    }
                    break;

                //repeatable door, switch
                case 63:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<SlowRepeatableDoorController> linked = new List<SlowRepeatableDoorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.ceilingObject.transform.SetParent(holder);

                            SlowRepeatableDoorController sc = sector.ceilingObject.GetComponent<SlowRepeatableDoorController>();
                            if (sc == null)
                            {
                                sc = sector.ceilingObject.AddComponent<SlowRepeatableDoorController>();
                                sc.Init(sector);
                            }

                            linked.Add(sc);
                            sector.Dynamic = true;
                        }

                        SwitchLinedefController sw = CreateSwitch(l, () =>
                        {
                            foreach (SlowRepeatableDoorController sectorController in linked)
                                if (sectorController.CurrentState == SlowRepeatableDoorController.State.Closed)
                                    sectorController.CurrentState = SlowRepeatableDoorController.State.Opening;
                        });

                        if (sw != null)
                        {
                            sw.Repeatable = true;

                            //all linked doors must be closed for the switch to be activateable
                            sw.Prereq = new System.Func<bool>(() =>
                            {
                                foreach (SlowRepeatableDoorController sectorController in linked)
                                    if (sectorController.CurrentState != SlowRepeatableDoorController.State.Closed)
                                        return false;

                                return true;
                            });

                            sw.AutoReturn = true;
                            sw.AutoReturnTime = 1f;
                        }
                    }
                    break;

                //lower floor to highest neighbor floor + 8, single use, switch
                case 70:
                case 71:  
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<Floor36SectorController> linked = new List<Floor36SectorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            Floor36SectorController sc = sector.floorObject.GetComponent<Floor36SectorController>();
                            if (sc == null)
                            {
                                sc = sector.floorObject.gameObject.AddComponent<Floor36SectorController>();
                                sc.Init(sector);
                            }
                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        SwitchLinedefController sw = CreateSwitch(l, () =>
                        {
                            foreach (Floor36SectorController sectorController in linked)
                            {
                                //E1M7 has a column with both floor 36 and floor 5
                                Floor5Controller fc = sectorController.targetSector.floorObject.GetComponent<Floor5Controller>();
                                if (fc != null)
                                    fc.CurrentState = Floor5Controller.State.AtTop;

                                if (sectorController.CurrentState == Floor36SectorController.State.AtTop)
                                    sectorController.CurrentState = Floor36SectorController.State.Lowering;
                            }
                        });

                        if (l.lineType == 70) //repeatable trigger
                            if (sw != null)
                            {
                                sw.Repeatable = true;

                                //all linked lifts must be at top for the switch to be activateable
                                sw.Prereq = new System.Func<bool>(() =>
                                {
                                    foreach (Floor36SectorController sectorController in linked)
                                    {
                                        //E1M7 has a column with two different floor 36 controller activators... '-_-
                                        if (sectorController.targetSector.floorHeight == sectorController.originalHeight)
                                            sectorController.CurrentState = Floor36SectorController.State.AtTop;

                                        if (sectorController.CurrentState != Floor36SectorController.State.AtTop)
                                            return false;
                                    }

                                    return true;
                                });

                                sw.AutoReturn = true;
                                sw.AutoReturnTime = 1f;
                            }

                    }
                    break;

                //lower floor to lowest neighbor floor, walk trigger
                case 82:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<Floor23SectorController> linked = new List<Floor23SectorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            Floor23SectorController sc = sector.floorObject.GetComponent<Floor23SectorController>();
                            if (sc == null)
                            {
                                sc = sector.floorObject.gameObject.AddComponent<Floor23SectorController>();
                                sc.Init(sector);
                            }
                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        CreateWalkTrigger(l, index, holder, (c) =>
                        {
                            foreach (Floor23SectorController lc in linked)
                                if (lc.CurrentState == Floor23SectorController.State.AtTop)
                                    lc.CurrentState = Floor23SectorController.State.Lowering;
                        });
                    }
                    break;

                //common lift, walktrigger
                case 88:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<Slow3sLiftController> linked = new List<Slow3sLiftController>(); 
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            Slow3sLiftController sc = sector.floorObject.GetComponent<Slow3sLiftController>();
                            if (sc == null)
                            {
                                sc = sector.floorObject.gameObject.AddComponent<Slow3sLiftController>();
                                sc.Init(sector);
                            }

                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        CreateWalkTrigger(l, index, holder, (c) => 
                        {
                            foreach (Slow3sLiftController lc in linked)
                                if (lc.CurrentState == Slow3sLiftController.State.AtTop)
                                    lc.CurrentState = Slow3sLiftController.State.Lowering;
                        });
                    }
                    break;

                //repeatable door, walktrigger
                case 90:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<SlowRepeatableDoorController> linked = new List<SlowRepeatableDoorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.ceilingObject.transform.SetParent(holder);

                            SlowRepeatableDoorController sc = sector.ceilingObject.GetComponent<SlowRepeatableDoorController>();
                            if (sc == null)
                            {
                                sc = sector.ceilingObject.gameObject.AddComponent<SlowRepeatableDoorController>();
                                sc.Init(sector);
                            }
                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        CreateWalkTrigger(l, index, holder, (c) =>
                        {
                            PlayerThing player = c.GetComponent<PlayerThing>();

                            if (player == null)
                                return;

                            foreach (SlowRepeatableDoorController lc in linked)
                                if (lc.CurrentState == SlowRepeatableDoorController.State.Closed)
                                {
                                    lc.waitTime = 4;
                                    lc.CurrentState = SlowRepeatableDoorController.State.Opening;
                                }
                        });
                    }
                    break;

                //teleport player or monster to tagged sector 
                case 97:
                    {
                        List<Sector> targetSectors = new List<Sector>();
                        if (Sector.TaggedSectors.ContainsKey(l.lineTag))
                            targetSectors.AddRange(Sector.TaggedSectors[l.lineTag]);

                        if (targetSectors.Count == 0)
                            Debug.LogError("Linedef " + index + " teleport could not find target sectors with tag "+ l.lineTag);

                        CreateWalkTrigger(l,index,holder, (c) =>
                        {
                            if (targetSectors.Count == 0)
                                return;

                            Sector targetSector = targetSectors[UnityEngine.Random.Range(0, targetSectors.Count)];
                            Vector2 targetPos = targetSector.RandomPoint;

                            GameObject effect1 = GameObject.Instantiate(GameManager.Instance.TeleportEffect);
                            effect1.transform.position = c.transform.position;

                            c.transform.position = new Vector3(targetPos.x, targetSector.floorHeight, targetPos.y);

                            GameObject effect2 = GameObject.Instantiate(GameManager.Instance.TeleportEffect);
                            effect2.transform.position = c.transform.position;
                        });
                    }
                    break;

                //single use door, switch
                case 103:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<SlowOneshotDoorController> linked = new List<SlowOneshotDoorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.ceilingObject.transform.SetParent(holder);

                            SlowOneshotDoorController sc = sector.ceilingObject.GetComponent<SlowOneshotDoorController>();
                            if (sc == null)
                            {
                                sc = sector.ceilingObject.AddComponent<SlowOneshotDoorController>();
                                sc.Init(sector);
                            }

                            linked.Add(sc);
                            sector.Dynamic = true;
                        }

                        CreateSwitch(l, () => 
                        {
                            foreach (SlowOneshotDoorController sectorController in linked)
                                if (sectorController.CurrentState == SlowOneshotDoorController.State.Closed)
                                    sectorController.CurrentState = SlowOneshotDoorController.State.Opening;
                        });
                    }
                    break;

                //connected sector with tag 666 will lower to lowest neighbor floor after bosses are dead
                case 666:
                    {
                        Sector sector = null;
                        if (l.Front.Sector.tag == 666)
                            sector = l.Front.Sector;
                        if (l.Back != null)
                            if (l.Back.Sector.tag == 666)
                                sector = l.Back.Sector;

                        if (sector == null)
                        {
                            Debug.LogError("Linedef type 666 could not find nearby sector with tag 666!");
                            continue;
                        }

                        sector.floorObject.transform.SetParent(holder);

                        Floor23SectorController sc = sector.floorObject.gameObject.AddComponent<Floor23SectorController>();
                        sc.Init(sector);

                        WhenMonstersDead mc = sector.floorObject.gameObject.AddComponent<WhenMonstersDead>();
                        mc.MonsterID = 3003; //Baron of Hell

                        mc.OnLastMonsterDie.AddListener(() =>
                        {
                            if (sc.CurrentState == Floor23SectorController.State.AtTop)
                                sc.CurrentState = Floor23SectorController.State.Lowering;
                        });

                        sector.Dynamic = true;
                    }
                    break;
            }
        }
    }

    private static SwitchLinedefController CreateSwitch(Linedef l, UnityAction OnActivate)
    {
        SwitchLinedefController script = null;
        string tex = "";

        if (l.BotFrontObject != null)
        {
            script = l.BotFrontObject.AddComponent<SwitchLinedefController>();
            tex = l.Front.tLow;
        }
        else if (l.MidFrontObject != null)
        {
            script = l.MidFrontObject.AddComponent<SwitchLinedefController>();
            tex = l.Front.tMid;
        }
        else if (l.TopFrontObject != null)
        {
            script = l.TopFrontObject.AddComponent<SwitchLinedefController>();
            tex = l.Front.tHigh;
        }

        if (script != null)
        {
            script.OnActivate.AddListener(OnActivate);
            script.CurrentTexture = tex;
        }

        return script;
    }

    private static void CreateWalkTrigger(Linedef l, int index, Transform holder, System.Action<Collider> OnActivate)
    {
        BoxCollider mc = Mesher.CreateLineTriggerCollider(
            l,
            Mathf.Min(l.Front.Sector.minimumFloorHeight, l.Back.Sector.minimumFloorHeight),
            Mathf.Max(l.Front.Sector.maximumCeilingHeight, l.Back.Sector.maximumCeilingHeight),
            "Tag_" + l.lineTag + "_teleport",
            holder
            );

        if (mc == null)
        {
            Debug.LogError("Linedef " + index + " could not create trigger. Type(" + l.lineType + ")");
            return;
        }

        mc.isTrigger = true;

        LineTrigger lt = mc.gameObject.AddComponent<LineTrigger>();
        lt.TriggerAction = OnActivate;
    }
}
