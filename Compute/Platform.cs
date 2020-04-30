using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.OpenCL;

namespace Compute
{
    public class Platform
    {
        public IntPtr Handle { get; }

        public string Profile => QueryString(CLEnum.PlatformProfile);

        public string Version => QueryString(CLEnum.PlatformVersion);

        public string Name => QueryString(CLEnum.PlatformName);

        public string Vendor => QueryString(CLEnum.PlatformVendor);

        public string[] Extensions => QueryString(CLEnum.PlatformExtensions).Split(' ');
        
        public Platform(IntPtr handle)
        {
            Handle = handle;
        }

        public static IEnumerable<Platform> Platforms
        {
            get
            {
                var platforms = new IntPtr[1024];
                
                var size = new uint[1];

                var error = (CLEnum) Bindings.OpenCl.GetPlatformIDs((uint) platforms.Length, platforms, size);

                if (error != CLEnum.Success)
                {
                    throw new Exception("Failed to get platform identifiers!");
                }
                
                Array.Resize(ref platforms, (int) size[default]);

                foreach (var platform in platforms)
                {
                    yield return new Platform(platform);
                }
            }
        }

        public IEnumerable<Accelerator> Accelerators
        {
            get
            {
                var result = new IntPtr[1024];
                
                var size = new uint[1];

                var span = new Span<IntPtr>(result);

                var error = (CLEnum) Bindings.OpenCl.GetDeviceIDs(
                    Handle,
                    CLEnum.DeviceTypeAll,
                    (uint) result.Length,
                    span,
                    size
                );

                if (error != CLEnum.Success)
                {
                    throw new Exception("Failed to create device group!");
                }
                
                Array.Resize(ref result, (int) size[default]);

                foreach (var handle in result)
                {
                    yield return new Accelerator(handle);
                }
            }
        }
        
        private byte[] QueryInfo(CLEnum type)
        {
            var result = new byte[1024];

            var size = new UIntPtr[1];

            var error = (CLEnum) Bindings.OpenCl.GetPlatformInfo(
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
    }
}