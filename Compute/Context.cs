using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

            var error = (CLEnum) Bindings.OpenCl.RetainMemObject(result);

            if (error != CLEnum.Success)
            {
                throw new Exception("Failed to retain device memory!");
            }

            OpenBuffers.Add(result);

            return result;
        }

        public IntPtr CreateBufferHost<T>(Span<T> span) where T : unmanaged
        {
            var error = new int[1];
            
            var result = Bindings.OpenCl.CreateBuffer(
                Handle,
                CLEnum.MemUseHostPtr,
                (UIntPtr) (span.Length * Marshal.SizeOf<T>()),
                span,
                new Span<int>(error)
            );
            
            if (result == IntPtr.Zero)
            {
                throw new Exception("Failed to allocate device host memory!");
            }

            var retainError = (CLEnum) Bindings.OpenCl.RetainMemObject(result);

            if (retainError != CLEnum.Success)
            {
                throw new Exception("Failed to retain device host memory!");
            }

            OpenBuffers.Add(result);

            return result;
        }

        public unsafe IntPtr AllocBufferHost(uint size)
        {
            /* TODO: Figure out why this fails */
            
            var error = 0;
            var ptr = 0;
            
            var result = Bindings.OpenCl.CreateBuffer(
                Handle,
                CLEnum.MemAllocHostPtr | CLEnum.MemReadWrite,
                (UIntPtr) size,
                &ptr,
                &error
            );
            
            if (result == IntPtr.Zero)
            {
                throw new Exception("Failed to allocate device host memory!");
            }

            OpenBuffers.Add(result);

            return result;
        }

        public unsafe IntPtr MapBuffer(IntPtr buffer, uint size, CLEnum flags)
        {
            var result = Bindings.OpenCl.EnqueueMapBuffer(Queue,
                buffer,
                true,
                flags,
                (UIntPtr) 0,
                (UIntPtr) size,
                0,
                Span<IntPtr>.Empty,
                Span<IntPtr>.Empty,
                Span<int>.Empty
            );
            
            var ptr = new IntPtr(result);

            if (ptr == IntPtr.Zero)
            {
                throw new Exception($"Failed to map device memory!");
            }

            return ptr;
        }

        public unsafe void UnmapBuffer(IntPtr buffer, IntPtr map)
        {
            var results = (CLEnum) Bindings.OpenCl.EnqueueUnmapMemObject(Queue,
                buffer,
                map.ToPointer(),
                0,
                null,
                null
            );

            if (results != CLEnum.Success)
            {
                throw new Exception($"Failed to unmap device memory!");
            }
        }

        public void ReleaseBuffer(IntPtr buffer)
        {
            Bindings.OpenCl.ReleaseMemObject(buffer);
            
            OpenBuffers.Remove(buffer);
        }

        public void WriteBuffer<T>(IntPtr buffer, Span<T> data, uint offset) where T : unmanaged
        {
            var watch = new Stopwatch();

            watch.Start();

            var error = (CLEnum) Bindings.OpenCl.EnqueueWriteBuffer(
                Queue,
                buffer,
                true,
                (UIntPtr) offset,
                (UIntPtr) (data.Length * Marshal.SizeOf<T>()),
                data,
                0,
                Span<IntPtr>.Empty,
                Span<IntPtr>.Empty
            );
            
            if (error != CLEnum.Success)
            {
                throw new Exception("Failed to write device memory!");
            }
        }

        public Span<T> ReadBuffer<T>(IntPtr buffer, uint size, uint offset) where T : unmanaged
        {
            var result = new T[size];

            var error = (CLEnum) Bindings.OpenCl.EnqueueReadBuffer(
                Queue,
                buffer,
                true,
                (UIntPtr) offset,
                (UIntPtr) (size * Marshal.SizeOf<T>()),
                new Span<T>(result),
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

        public void FinishQueue()
        {
            Bindings.OpenCl.Finish(Queue);
        }

        public void FlushQueue()
        {
            Bindings.OpenCl.Flush(Queue);
        }

        public uint QueryCommandQueue(CLEnum flag)
        {
            var bytes = new byte[1024];

            var size = new UIntPtr[1];

            var error = (CLEnum) Bindings.OpenCl.GetCommandQueueInfo(
                Queue,
                (uint) flag,
                (UIntPtr) bytes.Length,
                new Span<byte>(bytes),
                new Span<UIntPtr>(size)
            );

            if (error != CLEnum.Success)
            {
                throw new Exception("Failed to query command queue!");
            }
            
            Array.Resize(ref bytes, (int) size[0]);

            return BitConverter.ToUInt32(bytes);
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

            foreach (var program in DevicePrograms.ToArray())
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