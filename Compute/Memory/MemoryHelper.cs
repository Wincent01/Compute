using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Compute.Memory
{
    public static class MemoryHelper
    {
        public static byte[] ToBytes<T>(T value) where T : struct
        {
            var size = Marshal.SizeOf<T>();
            
            var bytes = new byte[size];

            MemoryMarshal.Write(new Span<byte>(bytes), ref value);
            
            return bytes;
        }

        public static T ToValue<T>(byte[] bytes) where T : struct
        {
            return MemoryMarshal.Read<T>(bytes);
        }

        public static void Write<T>(this Stream @this, T value) where T : struct
        {
            @this.Write(ToBytes(value));
        }

        public static T Read<T>(this Stream @this) where T : struct
        {
            var size = Marshal.SizeOf<T>();
            
            var bytes = new byte[size];

            @this.Read(new Span<byte>(bytes));

            return ToValue<T>(bytes);
        }
    }
}