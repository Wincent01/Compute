using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Compute.Memory
{
    public class SharedCollection<T> : IEnumerable<T> where T : struct
    {
        public SharedMemoryStream Stream { get; }

        public UIntPtr UPtr => Stream.UPtr;

        public int Length => (int) (Stream.Length / Marshal.SizeOf<T>());
        
        public SharedCollection(Context context, IEnumerable<T> enumerable)
        {
            var array = enumerable.ToArray();

            var size = (uint) (Marshal.SizeOf<T>() * array.Length);
            
            using var stream = new MemoryStream();

            foreach (var element in array)
            {
                stream.Write(element);
            }
            
            Stream = new SharedMemoryStream(context, size);

            Stream.Write(stream.ToArray());
        }

        public SharedCollection(Context context, int length)
        {
            var size = (uint) (Marshal.SizeOf<T>() * length);

            Stream = new SharedMemoryStream(context, size);
        }

        public IEnumerable<T> ReadCollection()
        {
            Stream.Position = 0;

            var content = new byte[Stream.Length];

            Stream.Read(new Span<byte>(content));

            using var stream = new MemoryStream(content);

            for (var i = 0; i < Length; i++)
            {
                yield return stream.Read<T>();
            }
        }

        public void WriteCollection(IEnumerable<T> enumerable)
        {
            using var stream = new MemoryStream();

            foreach (var element in enumerable)
            {
                stream.Write(element);
            }

            Stream.Position = default;

            Stream.Write(stream.ToArray());
        }

        public IEnumerator<T> GetEnumerator() => GetEnumerator(false);
        
        public IEnumerator<T> GetEnumerator(bool active)
        {
            if (active)
            {
                foreach (var element in ReadCollection())
                {
                    yield return element;
                }
                
                yield break;
            }
            
            Stream.Position = default;
            
            for (var i = 0; i < Length; i++)
            {
                yield return Stream.Read<T>();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator(false);
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
    }
}