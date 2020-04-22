using System;
using System.Collections.Generic;
using Silk.NET.OpenCL;

namespace Compute
{
    public class Context : IDisposable
    {
        private List<IntPtr> OpenBuffers { get; }

        internal List<DeviceProgram> DevicePrograms { get; }
        
        public Accelerator Accelerator { get; }
        
        public IntPtr Handle { get; }
        
        public IntPtr Queue { get; }

        public Context(Accelerator accelerator, IntPtr handle, IntPtr queue)
        {
            OpenBuffers = new List<IntPtr>();
            
            DevicePrograms = new List<DeviceProgram>();
            
            Accelerator = accelerator;

            Handle = handle;

            Queue = queue;
        }

        public IntPtr CreateBuffer(uint size, CLEnum flags)
        {
            var result = Bindings.OpenCl.CreateBuffer(
                Handle,
                flags,
                (UIntPtr) size,
                Span<byte>.Empty,
                Span<int>.Empty
            );

            if (result == IntPtr.Zero)
            {
                throw new Exception("Failed to allocate device memory!");
            }

            OpenBuffers.Add(result);

            return result;
        }

        public void ReleaseBuffer(IntPtr buffer)
        {
            Bindings.OpenCl.ReleaseMemObject(buffer);
            
            OpenBuffers.Remove(buffer);
        }

        public void WriteBuffer(IntPtr buffer, byte[] data, uint offset)
        {
            var error = (CLEnum) Bindings.OpenCl.EnqueueWriteBuffer(Queue,
                buffer,
                true,
                (UIntPtr) offset,
                (UIntPtr) data.Length,
                new Span<byte>(data),
                0,
                Span<IntPtr>.Empty,
                Span<IntPtr>.Empty
            );

            if (error != CLEnum.Success)
            {
                throw new Exception("Failed to write device memory!");
            }
        }

        public byte[] ReadBuffer(IntPtr buffer, uint size, uint offset)
        {
            var result = new byte[size];

            var error = (CLEnum) Bindings.OpenCl.EnqueueReadBuffer(Queue, buffer,
                true,
                (UIntPtr) offset,
                (UIntPtr) size,
                new Span<byte>(result),
                0,
                Span<IntPtr>.Empty,
                Span<IntPtr>.Empty
            );

            if (error != CLEnum.Success)
            {
                throw new Exception("Failed to read device memory!");
            }

            return result;
        }

        ~Context()
        {
            ReleaseUnmanagedResources();
        }

        private void ReleaseUnmanagedResources()
        {
            Accelerator.OpenContexts.Remove(this);

            foreach (var buffer in OpenBuffers)
            {
                Bindings.OpenCl.ReleaseMemObject(buffer);
            }
            
            OpenBuffers.Clear();

            foreach (var program in DevicePrograms)
            {
                program.Dispose();
            }
            
            DevicePrograms.Clear();

            Bindings.OpenCl.ReleaseCommandQueue(Queue);
            
            Bindings.OpenCl.ReleaseContext(Handle);
        }
        
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
    }
}