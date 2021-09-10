using System.IO;
using UnityEngine;

namespace DDSSequencer.Runtime
{

internal static class Utility
{
    public static (Vector2Int size, int fps, TextureFormat format, bool mips) ReadHeaderInfo(string headerPath)
    {
        byte[] header = File.ReadAllBytes(headerPath);

        var size = new Vector2Int(header[3] * 256 + header[2],
                                    header[1] * 256 + header[0]);
        int fps = header[6];
        TextureFormat format = (int)header[5] switch
        {
            23 => TextureFormat.BC7,
            18 => TextureFormat.DXT5,
            15 => TextureFormat.DXT1,
            21 => TextureFormat.BC5,
            _ => TextureFormat.BC7
        };

        bool mips = header[4] > 0;

        return (size, fps, format, mips);
    }
}

}