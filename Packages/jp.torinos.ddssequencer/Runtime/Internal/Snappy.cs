using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DDSSequencer.Runtime
{

public static class Snappy
{
    [DllImport("snappy", EntryPoint="encode_binary")]
    static extern IntPtr EncodeBinary(byte[] data, int size, out int dstSize);

    [DllImport("snappy", EntryPoint="decode_binary")]
    static extern IntPtr DecodeBinary(byte[] data, int size, out int dstSize);

    // pointer returned from this function must be freed
    // after use using Marshal.FreeCoTaskMem().
    public static IntPtr EncodeToPtr(byte[] src, out int size)
    {
        IntPtr ptr = EncodeBinary(src, src.Length, out size);

        if(ptr == IntPtr.Zero)
            throw new InvalidOperationException();

        return ptr;
    }

    public static byte[] Encode(byte[] src)
    {
        IntPtr ptr = EncodeToPtr(src, out var size);

        byte[] data = new byte[size];
        Marshal.Copy(ptr, data, 0, size);

        Marshal.FreeCoTaskMem(ptr);

        return data;
    }

    // pointer returned from this function must be freed
    // after use using Marshal.FreeCoTaskMem().
    public static IntPtr DecodeToPtr(byte[] src, out int size)
    {
        IntPtr ptr = DecodeBinary(src, src.Length, out size);

        if(ptr == IntPtr.Zero)
            throw new InvalidOperationException();

        return ptr;
    }

    public static byte[] Decode(byte[] src)
    {
        IntPtr ptr = DecodeToPtr(src, out var size);

        byte[] data = new byte[size];
        Marshal.Copy(ptr, data, 0, size);

        Marshal.FreeCoTaskMem(ptr);

        return data;
    }

    internal static Texture2D DecodeTexture(byte[] src, int width, int height, TextureFormat format)
    {
        IntPtr ptr = DecodeToPtr(src, out var size);

        Texture2D tex = new Texture2D(width, height, format, false);
        tex.LoadRawTextureData(ptr, size);
        tex.Apply();

        Marshal.FreeCoTaskMem(ptr);

        return tex;
    }
}

}