using System.Text;

public static class ByteReader
{
    public static int ReadInt32(byte[] data, ref int pointer)
    {
        return (int)(data[pointer++] |
                (int)data[pointer++] << 8 |
                (int)data[pointer++] << 16 |
                (int)data[pointer++] << 24);
    }

    public static int ReadInt16(byte[] data, ref int pointer)
    {
        return (int)(data[pointer++] | 
                (int)data[pointer++] << 8);

    }

    public static short ReadShort(byte[] data, ref int pointer)
    {
        return (short)(data[pointer++] |
                (short)data[pointer++] << 8);

    }

    public static string ReadName8(byte[] data, ref int pointer)
    {
        return Encoding.ASCII.GetString(new byte[]
        {
            data[pointer++],
            data[pointer++],
            data[pointer++],
            data[pointer++],
            data[pointer++],
            data[pointer++],
            data[pointer++],
            data[pointer++]
        }).TrimEnd('\0').ToUpper();
    }
}
