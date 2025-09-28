using System;
using System.IO;
using System.Runtime.InteropServices;
using Silk.NET.OpenCL;

namespace Compute.Memory
{
    public class SharedMemoryStream : Stream
    {
        public Context Context { get; }

        public IntPtr Handle { get; }

        public UIntPtr UPtr => (UIntPtr) Handle.ToInt64();

        public bool Host { get; }
        
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length { get; }
        public override long Position { get; set; }
        
        public SharedMemoryStream(Context context, uint size)
        {
            Context = context;
            
            Handle = Context.CreateBuffer(size, MemFlags.ReadWrite);

            Length = size;
        }

        public SharedMemoryStream(Context context, uint size, IntPtr handle, bool host)
        {
            Context = context;

            Handle = handle;

            Length = size;

            Host = host;
        }

        public static SharedMemoryStream FromHost<T>(Context context, Span<T> span) where T : unmanaged
        {
            var handle = context.CreateBufferHost(span);

            return new SharedMemoryStream(context, (uint) span.Length, handle, true);
        }

        public static SharedMemoryStream AllocHost(Context context, uint size)
        {
            var handle = context.AllocBufferHost(size);

            return new SharedMemoryStream(context, size, handle, true);
        }

        public override void Flush()
        {
        }

        public Span<T> Read<T>(int count) where T : unmanaged
        {
            var result = Context.ReadBuffer<T>(Handle, (uint) count, (uint) Position);
            
            Position += Marshal.SizeOf<T>() * count;

            return result;
        }

        public void ReadNonAlloc<T>(Span<T> data, int count) where T : unmanaged
        {
            Context.ReadBufferNonAlloc(Handle, data, (uint)count, (uint)Position);

            Position += Marshal.SizeOf<T>() * count;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var result = Context.ReadBuffer<byte>(Handle, (uint)count, (uint)Position).ToArray();

            result.CopyTo(buffer, offset);

            Position += count;

            return result.Length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public void Write<T>(Span<T> buffer) where T : unmanaged
        {
            Context.WriteBuffer(Handle, buffer, (uint) Position);

            Position += Marshal.SizeOf<T>() * buffer.Length;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Array.Resize(ref buffer, count);
            
            Context.WriteBuffer(Handle, new Span<byte>(buffer), (uint) Position);

            Position += count;
        }

        /// <summary>
        /// Writes an array of any unmanaged type to the device memory using reflection.
        /// </summary>
        /// <param name="array">The array to write to device memory</param>
        /// <param name="elementType">The element type of the array</param>
        public void WriteArrayGeneric(Array array, Type elementType)
        {
            var originalPosition = Position;
            Position = 0;
            
            // Use reflection to call the generic method for any unmanaged type
            var writeMethod = typeof(SharedMemoryStream).GetMethod(nameof(WriteArrayGenericTyped), 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (writeMethod == null)
                throw new InvalidOperationException("WriteArrayGenericTyped method not found.");
                
            var genericMethod = writeMethod.MakeGenericMethod(elementType);
            genericMethod.Invoke(this, new object[] { array });
            
            Position = originalPosition;
        }
        
        /// <summary>
        /// Reads data from device memory back into an array of any unmanaged type using reflection.
        /// </summary>
        /// <param name="array">The array to read data into</param>
        /// <param name="elementType">The element type of the array</param>
        public void ReadArrayGeneric(Array array, Type elementType)
        {
            var originalPosition = Position;
            Position = 0;
            
            // Use reflection to call the generic method for any unmanaged type
            var readMethod = typeof(SharedMemoryStream).GetMethod(nameof(ReadArrayGenericTyped), 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (readMethod == null)
                throw new InvalidOperationException("ReadArrayGenericTyped method not found.");
                
            var genericMethod = readMethod.MakeGenericMethod(elementType);
            genericMethod.Invoke(this, new object[] { array });
            
            Position = originalPosition;
        }
        
        private void WriteArrayGenericTyped<T>(Array array) where T : unmanaged
        {
            var typedArray = (T[])array;
            Write(new Span<T>(typedArray));
        }
        
        private void ReadArrayGenericTyped<T>(Array array) where T : unmanaged
        {
            var typedArray = (T[])array;
            ReadNonAlloc(new Span<T>(typedArray), typedArray.Length);
        }

        public override void Close()
        {
            Context.ReleaseBuffer(Handle);
        }
    }
}