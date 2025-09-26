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

        public IntPtr CreateBuffer(uint size, MemFlags flags)
        {
            var error = new int[1];
            var result = Bindings.OpenCl.CreateBuffer(
                Handle,
                flags,
                (UIntPtr) size,
                Span<byte>.Empty,
                new Span<int>(error)
            );

            if (result == IntPtr.Zero)
            {
                throw new Exception("Failed to allocate device memory!");
            }

            var retainError = (ErrorCodes) Bindings.OpenCl.RetainMemObject(result);

            if (retainError != ErrorCodes.Success)
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
                MemFlags.UseHostPtr,
                (UIntPtr) (span.Length * Marshal.SizeOf<T>()),
                span,
                new Span<int>(error)
            );
            
            if (result == IntPtr.Zero)
            {
                throw new Exception("Failed to allocate device host memory!");
            }

            var retainError = (ErrorCodes) Bindings.OpenCl.RetainMemObject(result);

            if (retainError != ErrorCodes.Success)
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
                MemFlags.AllocHostPtr | MemFlags.ReadWrite,
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

        public unsafe IntPtr MapBuffer(IntPtr buffer, uint size, MapFlags flags)
        {
            var error = 0;
            var result = Bindings.OpenCl.EnqueueMapBuffer(Queue,
                buffer,
                true,
                flags,
                (UIntPtr) 0,
                (UIntPtr) size,
                0,
                null,
                null,
                &error
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
            var results = (ErrorCodes) Bindings.OpenCl.EnqueueUnmapMemObject(Queue,
                buffer,
                map.ToPointer(),
                0,
                null,
                null
            );

            if (results != ErrorCodes.Success)
            {
                throw new Exception($"Failed to unmap device memory!");
            }
        }

        public void ReleaseBuffer(IntPtr buffer)
        {
            Bindings.OpenCl.ReleaseMemObject(buffer);
            
            OpenBuffers.Remove(buffer);
        }

        public unsafe void WriteBuffer<T>(IntPtr buffer, Span<T> data, uint offset) where T : unmanaged
        {
            var watch = new Stopwatch();

            watch.Start();

            fixed (T* dataPtr = data)
            {
                var error = (ErrorCodes) Bindings.OpenCl.EnqueueWriteBuffer(
                    Queue,
                    buffer,
                    true,
                    (UIntPtr) offset,
                    (UIntPtr) (data.Length * Marshal.SizeOf<T>()),
                    dataPtr,
                    0,
                    null,
                    null
                );
                
                if (error != ErrorCodes.Success)
                {
                    throw new Exception("Failed to write device memory!");
                }
            }
        }

        public unsafe Span<T> ReadBuffer<T>(IntPtr buffer, uint size, uint offset) where T : unmanaged
        {
            var result = new T[size];

            fixed (T* resultPtr = result)
            {
                var error = (ErrorCodes) Bindings.OpenCl.EnqueueReadBuffer(
                    Queue,
                    buffer,
                    true,
                    (UIntPtr) offset,
                    (UIntPtr) (size * Marshal.SizeOf<T>()),
                    resultPtr,
                    0,
                    null,
                    null
                );

                if (error != ErrorCodes.Success)
                {
                    throw new Exception("Failed to read device memory!");
                }
            }

            return result;
        }

        public unsafe void ReadBufferNonAlloc<T>(IntPtr buffer, Span<T> data, uint size, uint offset) where T : unmanaged
        {
            fixed (T* dataPtr = data)
            {
                var error = (ErrorCodes) Bindings.OpenCl.EnqueueReadBuffer(
                    Queue,
                    buffer,
                    true,
                    (UIntPtr) offset,
                    (UIntPtr) (size * Marshal.SizeOf<T>()),
                    dataPtr,
                    0,
                    null,
                    null
                );

                if (error != ErrorCodes.Success)
                {
                    throw new Exception("Failed to read device memory!");
                }
            }
        }

        public void FinishQueue()
        {
            Bindings.OpenCl.Finish(Queue);
        }

        public void FlushQueue()
        {
            Bindings.OpenCl.Flush(Queue);
        }

        public uint QueryCommandQueue(CommandQueueInfo flag)
        {
            var bytes = new byte[1024];

            var size = new UIntPtr[1];

            var error = (ErrorCodes) Bindings.OpenCl.GetCommandQueueInfo(
                Queue,
                flag,
                (UIntPtr) bytes.Length,
                new Span<byte>(bytes),
                new Span<UIntPtr>(size)
            );

            if (error != ErrorCodes.Success)
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