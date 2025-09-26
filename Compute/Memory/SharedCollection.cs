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

        public Span<T> CopyToHost()
        {
            Stream.Position = 0;

            return Stream.Read<T>(Length);
        }

        public void CopyToHostNonAlloc(Span<T> data)
        {
            Stream.Position = 0;

            Stream.ReadNonAlloc(data, Length);
        }

        public void CopyToDevice(Span<T> enumerable)
        {
            Stream.Position = default;

            Stream.Write(enumerable);
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

        public static implicit operator UIntPtr(SharedCollection<T> collection)
        {
            return collection.UPtr;
        }

        public void Dispose()
        {
            Stream?.Dispose();
        }
    }
}