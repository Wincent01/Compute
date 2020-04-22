using System;
using System.IO;
using Silk.NET.OpenCL;

namespace Compute.Memory
{
    public class SharedMemoryStream : Stream
    {
        public Context Context { get; }

        public IntPtr Handle { get; }

        public UIntPtr UPtr => (UIntPtr) Handle.ToInt64();

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length { get; }
        public override long Position { get; set; }
        
        public SharedMemoryStream(Context context, uint size)
        {
            Context = context;
            
            Handle = Context.CreateBuffer(size, CLEnum.MemReadWrite);

            Length = size;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var result = Context.ReadBuffer(Handle, (uint) count, (uint) Position);

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

        public override void Write(byte[] buffer, int offset, int count)
        {
            Array.Resize(ref buffer, count);
            
            Context.WriteBuffer(Handle, buffer, (uint) Position);

            Position += count;
        }

        public override void Close()
        {
            Context.ReleaseBuffer(Handle);
        }
    }
}