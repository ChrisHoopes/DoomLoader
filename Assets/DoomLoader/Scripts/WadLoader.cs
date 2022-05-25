using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System;

public class WadLoader : MonoBehaviour 
{
    public static List<Lump> lumps = new List<Lump>();

    public static bool LoadWad(string file)
    {
        string path = Application.streamingAssetsPath + "/" + file;
        if (!File.Exists(path))
        {
            Debug.LogError("WadLoader: Load: File \"" + file + "\" does not exist!");
            return false;
        }

        FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        BinaryReader reader = new BinaryReader(stream, Encoding.ASCII);

        if (file.Length < 4)
        {
            reader.Close();
            stream.Close();
            Debug.LogError("WadLoader: Load: WAD length < 4!");
            return false;
        }

        try
        {
            stream.Seek(0, SeekOrigin.Begin);

            bool isiwad = (Encoding.ASCII.GetString(reader.ReadBytes(4)) == "IWAD"); //other option is "PWAD"
            if (isiwad) { }

            int numlumps = reader.ReadInt32();
            int lumpsoffset = reader.ReadInt32();

            stream.Seek(lumpsoffset, SeekOrigin.Begin);

            lumps.Clear();

            for (int i = 0; i < numlumps; i++)
            {
                int offset = reader.ReadInt32();
                int length = reader.ReadInt32();
                string name = Encoding.ASCII.GetString(reader.ReadBytes(8)).TrimEnd('\0').ToUpper();

                lumps.Add(new Lump(offset, length, name));
            }

            //load the whole wad into memory
            long bytes = 0;
            foreach(Lump l in lumps)
            {
                l.data = new byte[l.length];
                stream.Seek(l.offset, SeekOrigin.Begin);
                stream.Read(l.data, 0, l.length);
                bytes += l.length;
            }

            Debug.Log("Loaded WAD \"" + file + "\" (" + bytes + " bytes in lumps)");
        }
        catch(Exception e)
        {
            Debug.LogError("WadLoader: Load: Reader exception!");
            Debug.LogError(e);

            reader.Close();
            stream.Close();
            return false;
        }

        reader.Close();
        stream.Close();
        return true;
    }
}
