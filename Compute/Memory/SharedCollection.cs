using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Compute.Memory
{
    public class SharedCollection<T> : IEnumerable<T>, IDisposable where T : unmanaged
    {
        public SharedMemoryStream Stream { get; }

        public UIntPtr UPtr => Stream.UPtr;

        public int Length => (int) (Stream.Length / Marshal.SizeOf<T>());
        
        public SharedCollection(Context context, Span<T> span, bool host = false)
        {
            if (host)
            {
                Stream = SharedMemoryStream.FromHost(context, span);
            }
            else
            {
                var size = (uint) (Marshal.SizeOf<T>() * span.Length);

                Stream = new SharedMemoryStream(context, size);

                Stream.Write(span);
            }
        }

        public SharedCollection(Context context, int length, bool host = false)
        {
            var size = (uint) (Marshal.SizeOf<T>() * length);

            if (host)
            {
                Stream = SharedMemoryStream.AllocHost(context, size);
            }
            else
            {
                Stream = new SharedMemoryStream(context, size);
            }
        }

        public Span<T> ReadCollection()
        {
            Stream.Position = 0;

            return Stream.Read<T>(Length);
        }

        public void WriteCollection(IEnumerable<T> enumerable)
        {
            Stream.Position = default;

            var array = enumerable.ToArray();

            Stream.Write(new Span<T>(array));
        }

        public IEnumerator<T> GetEnumerator()
        {
            Stream.Position = default;
            
            for (var i = 0; i < Length; i++)
            {
                yield return Stream.Read<T>();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T this[int index]
        {
            get
            {
                Stream.Position = Marshal.SizeOf<T>() * index;

                return Stream.Read<T>();
            }
            set
            {
                Stream.Position = Marshal.SizeOf<T>() * index;

                Stream.Write(value);
            }
        }

        public void Dispose()
        {
            Stream?.Dispose();
        }
    }
}