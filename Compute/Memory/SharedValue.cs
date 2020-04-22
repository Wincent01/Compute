using System;
using System.Runtime.InteropServices;

namespace Compute.Memory
{
    public class SharedValue<T> where T : struct
    {
        public SharedMemoryStream Stream { get; }

        public UIntPtr UPtr => Stream.UPtr;
        
        public SharedValue(Context context, T value)
        {
            var size = Marshal.SizeOf<T>();

            Stream = new SharedMemoryStream(context, (uint) size);

            Stream.Write(value);
        }

        public SharedValue(Context context)
        {
            var size = Marshal.SizeOf<T>();

            Stream = new SharedMemoryStream(context, (uint) size);
        }

        public T Value
        {
            get
            {
                Stream.Position = default;

                return Stream.Read<T>();
            }
            set
            {
                Stream.Position = default;

                Stream.Write(value);
            }
        }
    }
}