using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class TextureLoader : MonoBehaviour 
{
    public FilterMode DefaultFilterMode = FilterMode.Point;

    public static TextureLoader Instance;
    public Texture illegal;

    void Awake()
    {
        Instance = this;
        
        for (int i = 0; i < OverrideParatemers.Length; i++)
            _overrideParameters.Add(OverrideParatemers[i].textureName, OverrideParatemers[i]);

        foreach (SpriteOverride so in _OverrideSprites)
            OverrideSprites.Add(so.spriteName, so.sprite);
    }

    [System.Serializable]
    public struct TextureParameters
    {
        public string textureName;
        public TextureWrapMode horizontalWrapMode;
        public TextureWrapMode verticalWrapMode;
        public FilterMode filterMode;
    }

    [System.Serializable]
    public struct SpriteOverride
    {
        public string spriteName;
        public Texture sprite;
    }

    public static Color[] Palette;
    public static List<string> PatchNames = new List<string>();
    public static Dictionary<string, int[,]> Patches = new Dictionary<string, int[,]>();
    public static List<MapTexture> MapTextures = new List<MapTexture>();
    public static Dictionary<string, Texture> WallTextures = new Dictionary<string, Texture>();
    public static Dictionary<string, Texture> FlatTextures = new Dictionary<string, Texture>();
    public static Dictionary<string, Texture> SpriteTextures = new Dictionary<string, Texture>();
    public static Dictionary<string, bool> NeedsAlphacut = new Dictionary<string, bool>();
    public Dictionary<string, TextureParameters> _overrideParameters = new Dictionary<string, TextureParameters>();
    public TextureParameters[] OverrideParatemers = new TextureParameters[0];
    public List<SpriteOverride> _OverrideSprites = new List<SpriteOverride>();
    public Dictionary<string, Texture> OverrideSprites = new Dictionary<string, Texture>();

    public class MapTexture
    {
        public string textureName;
        public int masked;
        public int width;
        public int height;
        public int columnDirectory;
        public MapPatch[] patches;
    }

    public class MapPatch
    {
        public short originx;
        public short originy;
        public int number;
        public int stepdir;
        public int colormap;
    }

    public Texture GetWallTexture(string textureName)
    {
        if (WallTextures.ContainsKey(textureName))
            return (WallTextures[textureName]);

        Debug.Log("TextureLoader: No wall texture \"" + textureName +"\"");
        return illegal;
    }

    public Texture GetFlatTexture(string textureName)
    {
        if (FlatTextures.ContainsKey(textureName))
            return (FlatTextures[textureName]);

        Debug.Log("TextureLoader: No flat texture \"" + textureName + "\"");
        return illegal;
    }

    public Texture GetSpriteTexture(string textureName)
    {
        if (OverrideSprites.ContainsKey(textureName))
            return OverrideSprites[textureName];

        if (SpriteTextures.ContainsKey(textureName))
            return (SpriteTextures[textureName]);

        Debug.Log("TextureLoader: No sprite texture \"" + textureName + "\"");
        return illegal;
    }

    public void LoadAndBuildAll()
    {
        if (LoadPalette())
            if (LoadPatchNames())
            {
                foreach (string p in PatchNames)
                    LoadPatch(p);

                if (LoadMapTextures())
                    BuildWallTextures();

                LoadFlats();
                LoadSprites();
            }
    }

    public void LoadSprites()
    {
        bool begin = false;
        foreach (Lump l in WadLoader.lumps)
        {
            if (!begin)
            {
                if (l.lumpName == "S_START")
                    begin = true;

                continue;
            }

            if (l.lumpName == "S_END")
                break;

            int[,] pixelindices = ReadPatchData(l.data);
            int width = pixelindices.GetLength(0);
            int height = pixelindices.GetLength(1);
            Color[] pixels = new Color[height * width];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = Palette[pixelindices[x, y]];

            Texture2D tex = new Texture2D(width, height) { name = l.lumpName };
            tex.SetPixels(pixels);

            if (_overrideParameters.ContainsKey(l.lumpName))
            {
                TextureParameters p = _overrideParameters[l.lumpName];
                tex.wrapModeU = p.horizontalWrapMode;
                tex.wrapModeV = p.verticalWrapMode;
                tex.filterMode = p.filterMode;
            }
            else
            {
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = DefaultFilterMode;
            }

            tex.Apply();
            SpriteTextures.Add(l.lumpName, tex);
        }
    }

    public void BuildWallTextures()
    {
        foreach (MapTexture t in MapTextures)
        {
            Color[] pixels = new Color[t.width * t.height];

            //set the base pixels clear
            for (int y = 0; y < t.height; y++)
                for (int x = 0; x < t.width; x++)
                    pixels[y * t.width + x] = Palette[256];

            foreach (MapPatch p in t.patches)
            {
                if (p.number >= PatchNames.Count)
                {
                    Debug.LogError("TextureLoader: BuildTextures: Patch number out of range, in texture \"" + t.textureName + "\"");
                    continue;
                }

                string patchName = PatchNames[p.number];
                if (!Patches.ContainsKey(patchName))
                {
                    Debug.LogError("TextureLoader: BuildTextures: Could not find patch \"" + patchName + "\"");
                    continue;
                }

                int[,] patch = Patches[patchName];

                int pheight = patch.GetLength(1);
                for (int y = 0; y < pheight; y++)
                    for (int x = 0; x < patch.GetLength(0); x++)
                    {
                        int py = pheight - y - 1;

                        if (patch[x, py] == 256)
                            continue;

                        int oy = p.originy + y;
                        int ox = p.originx + x;

                        if (ox >= 0 && ox < t.width && oy >= 0 && oy < t.height)
                            pixels[(t.height - oy - 1) * t.width + ox] = Palette[patch[x, py]];
                    }
            }

            bool alphaCut = false;
            for (int y = 0; y < t.height; y++)
                for (int x = 0; x < t.width; x++)
                    if (pixels[y * t.width + x].a < 1f)
                        alphaCut = true;

            Texture2D tex = new Texture2D(t.width, t.height) { name = t.textureName };
            tex.SetPixels(pixels);

            if (_overrideParameters.ContainsKey(t.textureName))
            {
                TextureParameters p = _overrideParameters[t.textureName];
                tex.wrapModeU = p.horizontalWrapMode;
                tex.wrapModeV = p.verticalWrapMode;
                tex.filterMode = p.filterMode;
            }
            else
            {
                tex.wrapMode = TextureWrapMode.Repeat;
                tex.filterMode = DefaultFilterMode;
            }

            tex.Apply();
            WallTextures.Add(t.textureName, tex);

            if (alphaCut)
                NeedsAlphacut.Add(t.textureName, true);
        }
    }

    public bool LoadMapTextures()
    {
        foreach (Lump l in WadLoader.lumps)
            if (l.lumpName == "TEXTURE1")
            {
                int p = 0;
                int num = ByteReader.ReadInt32(l.data, ref p);

                int[] offsets = new int[num];
                for (int i = 0; i < num; i++)
                    offsets[i] = ByteReader.ReadInt32(l.data, ref p);

                MapTextures.Clear();
                for (int i = 0; i < num; i++)
                {
                    p = offsets[i];

                    MapTexture t = new MapTexture
                    {
                        textureName = ByteReader.ReadName8(l.data, ref p),

                        masked = ByteReader.ReadInt32(l.data, ref p),
                        width = ByteReader.ReadInt16(l.data, ref p),
                        height = ByteReader.ReadInt16(l.data, ref p),
                        columnDirectory = ByteReader.ReadInt32(l.data, ref p),
                        patches = new MapPatch[ByteReader.ReadInt16(l.data, ref p)]
                    };

                    for (int j = 0; j < t.patches.Length; j++)
                        t.patches[j] = new MapPatch
                        {
                            originx = ByteReader.ReadShort(l.data, ref p),
                            originy = ByteReader.ReadShort(l.data, ref p),
                            number = ByteReader.ReadInt16(l.data, ref p),
                            stepdir = ByteReader.ReadInt16(l.data, ref p),
                            colormap = ByteReader.ReadInt16(l.data, ref p)
                        };

                    MapTextures.Add(t);
                }

                return true;
            }

        return false;
    }

    public void LoadFlats()
    {
        bool begin = false;
        foreach (Lump l in WadLoader.lumps)
        {
            if (!begin)
            {
                if (l.lumpName == "F1_START")
                    begin = true;

                continue;
            }

            if (l.lumpName == "F1_END")
                break;

            Color[] pixels = new Color[64 * 64];

            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    pixels[y * 64 + x] = Palette[l.data[y * 64 + x]];

            Texture2D tex = new Texture2D(64, 64) { name = l.lumpName };
            tex.SetPixels(pixels);
            tex.filterMode = DefaultFilterMode;
            tex.Apply();

            FlatTextures.Add(l.lumpName, tex);
        }
    }

    public bool LoadPalette()
    {
        foreach (Lump l in WadLoader.lumps)
            if (l.lumpName == "PLAYPAL")
            {
                Palette = new Color[257];
                for (int i = 0; i < 256; i++)
                    Palette[i] = new Color((float)l.data[i * 3] / 255f, (float)l.data[i * 3 + 1] / 255f, (float)l.data[i * 3 + 2] / 255f, 1f);

                Palette[256] = new Color(0, 0, 0, 0);

                return true;
            }

        return false;
    }

    public bool LoadPatchNames()
    {
        foreach (Lump l in WadLoader.lumps)
            if (l.lumpName == "PNAMES")
            {
                PatchNames.Clear();

                int p = 0;
                int num = (int)(l.data[p++] | (int)l.data[p++] << 8 | (int)l.data[p++] << 16 | (int)l.data[p++] << 24);

                for (int i = 0; i < num; i++)
                    PatchNames.Add(ByteReader.ReadName8(l.data, ref p));

                return true;
            }

        return false;
    }

    public int[,] LoadPatch(string patchName)
    {
        if (Patches.ContainsKey(patchName))
            return Patches[patchName];

        foreach (Lump l in WadLoader.lumps)
            if (l.lumpName == patchName)
            {
                int[,] pixels = ReadPatchData(l.data);
                Patches.Add(patchName, pixels);
                return pixels;
            }

        //suppressed this as it seems shareware Doom WAD contains PNAMES for full version patchpool or something
        //Debug.LogError("TextureLoader: LoadPatch: Could not find patch \"" + patchName + "\"");
        return null;
    }

    public int[,] ReadPatchData(byte[] data)
    {
        int p = 0;
        int width = ByteReader.ReadInt16(data, ref p);
        int height = ByteReader.ReadInt16(data, ref p);
        int left = ByteReader.ReadInt16(data, ref p);
        int top = ByteReader.ReadInt16(data, ref p);

        if (left > 0) { }
        if (top > 0) { }

        int[,] pixels = new int[width, height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[x, y] = 256;

        int[] columns = new int[width];
        for (int x = 0; x < width; x++)
            columns[x] = ByteReader.ReadInt32(data, ref p);

        for (int x = 0; x < width; x++)
        {
            p = columns[x];

            while (true)
            {
                int offset = data[p++];
                if (offset == byte.MaxValue)
                    break;

                int length = data[p++];
                p++; //dummy byte
                for (int y = 0; y < length; y++)
                    pixels[x, height - (offset + y + 1)] = data[p++];
                p++; //dummy byte
            }
        }

        return pixels;
    }

    //all switches follow a common naming convention, the start of the texture name is either SW1 or SW2
    public void SetSwitchTexture(MeshRenderer mr, bool state)
    {
        if (mr == null)
            return;
        
        MaterialPropertyBlock materialProperties = new MaterialPropertyBlock();
        mr.GetPropertyBlock(materialProperties);

        string current = materialProperties.GetTexture("_MainTex").name;

        //some switches are "invisible" in the sense that they are placed in a non-switch texture wall
        if (current.Substring(0, 2) != "SW")
            return;

        current = (state ? "SW2" : "SW1") + current.Substring(3);

        materialProperties.SetTexture("_MainTex", GetWallTexture(current));
        mr.SetPropertyBlock(materialProperties);
    }

    public void SwapSwitchTexture(MeshRenderer mr)
    {
        if (mr == null)
            return;

        MaterialPropertyBlock materialProperties = new MaterialPropertyBlock();
        mr.GetPropertyBlock(materialProperties);

        string current = materialProperties.GetTexture("_MainTex").name;

        //some switches are "invisible" in the sense that they are placed in a non-switch texture wall
        if (current.Substring(0, 2) != "SW")
            return;

        if (current[2] == '2')
            current = "SW1" + current.Substring(3);
        else
            current = "SW2" + current.Substring(3);

        materialProperties.SetTexture("_MainTex", GetWallTexture(current));
        mr.SetPropertyBlock(materialProperties);
    }
}
