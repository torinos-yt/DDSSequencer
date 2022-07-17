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

    /// <summary>
    /// pointer returned from this function must be freed
    /// after use using Marshal.FreeCoTaskMem().
    /// </summary>
    /// <param name="src">Byte array to be compressed</param>
    /// <param name="size">Size of compressed bytes</param>
    /// <returns>Pointer to the beginning of a contiguous region of compressed bytes</returns>
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

    /// <summary>
    /// pointer returned from this function must be freed
    /// after use using Marshal.FreeCoTaskMem().
    /// </summary>
    /// <param name="src">Byte array to be decompressed</param>
    /// <param name="size">Size of decompressed bytes</param>
    /// <returns>Pointer to the beginning of a contiguous region of decompressed bytes</returns>
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
}

}