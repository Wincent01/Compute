using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.OpenCL;

namespace Compute
{
    public class Accelerator : IDisposable
    {
        internal List<Context> OpenContexts { get; }
        
        public IntPtr Handle { get; }

        public Accelerator(IntPtr handle)
        {
            OpenContexts = new List<Context>();
            
            Handle = handle;
        }

        public string Name => QueryString(CLEnum.DeviceName);

        public bool Available => QueryBoolean(CLEnum.DeviceAvailable);

        public bool CompilerAvailable => QueryBoolean(CLEnum.DeviceCompilerAvailable);

        public string[] Extensions => QueryString(CLEnum.DeviceExtensions).Split(' ');

        public ulong Memory => QueryULong(CLEnum.DeviceGlobalMemSize);

        public uint Units => QueryUInt(CLEnum.DeviceMaxComputeUnits);

        public uint ClockFrequency => QueryUInt(CLEnum.DeviceMaxClockFrequency);

        public string Vendor => QueryString(CLEnum.DeviceVendor);

        public string Version => QueryString(CLEnum.DeviceVersion);

        public string DriverVersion => QueryString(CLEnum.DriverVersion);
        
        private byte[] QueryInfo(CLEnum type)
        {
            var result = new byte[1024];

            var size = new UIntPtr[1];

            var error = (CLEnum) Bindings.OpenCl.GetDeviceInfo(
                Handle,
                (uint) type,
                (UIntPtr) result.Length,
                new Span<byte>(result),
                new Span<UIntPtr>(size)
            );

            if (error != CLEnum.Success)
            {
                throw new Exception($"Failed to get device info \"{type}\"!");
            }
                
            Array.Resize(ref result, (int) size[default]);

            return result;
        }

        private string QueryString(CLEnum type)
        {
            var result = QueryInfo(type);

            return new string(result.Select(b => (char) b).ToArray()).Replace("\0", "");
        }

        private uint QueryUInt(CLEnum type)
        {
            var result = QueryInfo(type);

            return BitConverter.ToUInt32(result);
        }
        
        private ulong QueryULong(CLEnum type)
        {
            var result = QueryInfo(type);

            return BitConverter.ToUInt64(result);
        }

        private bool QueryBoolean(CLEnum type)
        {
            var result = (CLEnum) QueryUInt(type);

            return result == CLEnum.True;
        }

        public unsafe Context CreateContext()
        {
            var devices = new[]
            {
                Handle
            };

            var error = new int[1];

            var result = Bindings.OpenCl.CreateContext(
                Span<IntPtr>.Empty,
                1,
                devices,
                null,
                Span<byte>.Empty,
                error
            );

            if (result == IntPtr.Zero)
            {
                throw new Exception("Failed to create a compute context!");
            }

            var queue = Bindings.OpenCl.CreateCommandQueue(result, Handle, 0, error);

            if (queue == IntPtr.Zero)
            {
                throw new Exception("Failed to create command queue!");
            }
            
            var retain = (CLEnum) Bindings.OpenCl.RetainCommandQueue(queue);

            if (retain != CLEnum.Success)
            {
                throw new Exception("Failed to retain command queue!");
            }
            
            var context = new Context(this, result, queue);

            OpenContexts.Remove(context);

            return context;
        }

        public static unsafe void Callback(char* error, void* info, UIntPtr cb, void* data)
        {
            var bytes = new List<byte>();

            for (var i = 0; i < cb.ToUInt32(); i++)
            {
                bytes.Add((byte) *(error + i));
            }
            
            Console.WriteLine($"Error: {new string(bytes.Select(b => (char) b).ToArray())} {cb}");
        }

        public static Accelerator FindAccelerator(AcceleratorType type)
        {
            var deviceType = type switch
            {
                AcceleratorType.Cpu => CLEnum.DeviceTypeCpu,
                AcceleratorType.Gpu => CLEnum.DeviceTypeGpu,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            var result = new IntPtr[1];

            var span = new Span<IntPtr>(result);

            var error = (CLEnum) Bindings.OpenCl.GetDeviceIDs(
                IntPtr.Zero,
                deviceType,
                1,
                span,
                null
            );

            if (error != CLEnum.Success)
            {
                throw new Exception("Failed to create device group!");
            }

            return new Accelerator(result[default]);
        }

        ~Accelerator()
        {
            ReleaseUnmanagedResources();
        }

        private void ReleaseUnmanagedResources()
        {
            foreach (var context in OpenContexts)
            {
                context.Dispose();
            }
            
            OpenContexts.Clear();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
    }
}