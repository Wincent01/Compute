using System;
using System.Runtime.InteropServices;

namespace Compute.Memory
{
    public class SharedValue<T> : IDisposable where T : unmanaged
    {
        public SharedMemoryStream Stream { get; }
        
        public UIntPtr UPtr => Stream.UPtr;

        private readonly T[] _value;

        public SharedValue(Context context, T value, bool host = false)
        {
            if (host)
            {
                _value = new T[] {value};
                
                Stream = SharedMemoryStream.FromHost(context, new Span<T>(_value));
            }
            else
            {
                var size = Marshal.SizeOf<T>();

                Stream = new SharedMemoryStream(context, (uint) size);

                Stream.Write(value);
            }
        }

        public SharedValue(Context context, bool host = false)
        {
            if (host)
            {
                _value = new T[1];
                
                Stream = SharedMemoryStream.FromHost(context, new Span<T>(_value));
            }
            else
            {
                var size = Marshal.SizeOf<T>();

                Stream = new SharedMemoryStream(context, (uint) size);
            }
        }

        public T Value
        {
            get
            {
                if (Stream.Host) return _value[0];
                
                Stream.Position = default;

                return Stream.Read<T>();
            }
            set
            {
                if (Stream.Host)
                {
                    _value[0] = value;
                    
                    return;
                }
                
                Stream.Position = default;

                Stream.Write(value);
            }
        }

        public void Dispose()
        {
            Stream?.Dispose();
        }
    }
}