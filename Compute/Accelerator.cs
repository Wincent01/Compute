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

        public string Name => QueryString(DeviceInfo.Name);

        public bool Available => QueryBoolean(DeviceInfo.Available);

        public bool CompilerAvailable => QueryBoolean(DeviceInfo.CompilerAvailable);

        public string[] Extensions => QueryString(DeviceInfo.Extensions).Split(' ');

        public ulong GlobalMemory => QueryULong(DeviceInfo.GlobalMemSize);
        
        public ulong LocalMemory => QueryULong(DeviceInfo.LocalMemSize);

        public uint Units => QueryUInt(DeviceInfo.MaxComputeUnits);

        public uint ClockFrequency => QueryUInt(DeviceInfo.MaxClockFrequency);

        public string Vendor => QueryString(DeviceInfo.Vendor);

        public string Version => QueryString(DeviceInfo.Version);

        public string DriverVersion => QueryString(DeviceInfo.DriverVersion);

        public uint AddressBits => QueryUInt(DeviceInfo.AddressBits);

        public bool LittleEndian => QueryBoolean(DeviceInfo.EndianLittle);

        public bool ErrorCorrectionSupport => QueryBoolean(DeviceInfo.ErrorCorrectionSupport);

        public ulong MaxAllocSize => QueryULong(DeviceInfo.MaxMemAllocSize);

        public uint MaxWorkDimensions => QueryUInt(DeviceInfo.MaxWorkItemDimensions);

        public uint MaxWorkGroupSize => QueryUInt(DeviceInfo.MaxWorkGroupSize);

        public IEnumerable<ulong> MaxWorkSizes
        {
            get
            {
                var results = QueryInfo(DeviceInfo.MaxWorkItemSizes);
                
                for (var i = 0; i < results.Length; i += 8)
                {
                    yield return BitConverter.ToUInt64(results, i);
                }
            }
        }

        public string Profile => QueryString(DeviceInfo.Profile);

        public string BoardNameAmd => QueryString( (DeviceInfo) 0x4038);

        public ulong FreeMemoryAmd => QueryULong((DeviceInfo) 0x4039) * 1000;

        public uint MultiplierPerUnitAmd => QueryUInt((DeviceInfo) 0x4040);
        
        public uint InstructionsPerUnitAmd => QueryUInt((DeviceInfo) 0x4042);

        public uint LocalMemoryPerUnitAmd => QueryUInt((DeviceInfo) 0x4047);
        
        private byte[] QueryInfo(DeviceInfo type)
        {
            var result = new byte[1024];

            var size = new UIntPtr[1];

            var error = (ErrorCodes) Bindings.OpenCl.GetDeviceInfo(
                Handle,
                type,
                (UIntPtr) result.Length,
                new Span<byte>(result),
                new Span<UIntPtr>(size)
            );

            if (error != ErrorCodes.Success)
            {
                throw new Exception($"Failed to get device info \"{type}\"!");
            }
                
            Array.Resize(ref result, (int) size[default]);

            return result;
        }

        private string QueryString(DeviceInfo type)
        {
            var result = QueryInfo(type);

            return new string(result.Select(b => (char) b).ToArray()).Replace("\0", "");
        }

        private uint QueryUInt(DeviceInfo type)
        {
            var result = QueryInfo(type);

            return BitConverter.ToUInt32(result);
        }
        
        private ulong QueryULong(DeviceInfo type)
        {
            var result = QueryInfo(type);

            return BitConverter.ToUInt64(result);
        }

        private bool QueryBoolean(DeviceInfo type)
        {
            var result = (Bool) QueryUInt(type);

            return result == Bool.True;
        }

        public Context CreateContext()
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

            var queue = Bindings.OpenCl.CreateCommandQueue(result, Handle, CommandQueueProperties.None, new Span<int>(error));

            if (queue == IntPtr.Zero)
            {
                throw new Exception("Failed to create command queue!");
            }
            
            var retain = (ErrorCodes) Bindings.OpenCl.RetainCommandQueue(queue);

            if (retain != ErrorCodes.Success)
            {
                throw new Exception("Failed to retain command queue!");
            }
            
            var context = new Context(this, result, queue);

            OpenContexts.Add(context);

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
                AcceleratorType.Cpu => DeviceType.Cpu,
                AcceleratorType.Gpu => DeviceType.Gpu,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            var result = new IntPtr[1];
            var count = new uint[1];

            var span = new Span<IntPtr>(result);

            var error = (ErrorCodes) Bindings.OpenCl.GetDeviceIDs(
                IntPtr.Zero,
                deviceType,
                1,
                span,
                new Span<uint>(count)
            );

            if (error != ErrorCodes.Success)
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
            foreach (var context in OpenContexts.ToArray())
            {
                context.Dispose();
            }

            Bindings.OpenCl.ReleaseDevice(Handle);
            
            OpenContexts.Clear();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
    }
}