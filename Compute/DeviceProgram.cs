using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.OpenCL;

namespace Compute
{
    public class DeviceProgram : IDisposable
    {
        internal List<Kernel> Kernels { get; }
        
        public Context Context { get; }
        
        public IntPtr Handle { get; }
        
        public DeviceProgram(Context context, IntPtr handle)
        {
            Kernels = new List<Kernel>();
            
            Context = context;
            
            Handle = handle;

            Context.DevicePrograms.Add(this);
        }

        public static DeviceProgram FromSource(Context context, string source)
        {
            var code = new[]
            {
                $"{source}\0"
            };

            var result = Bindings.OpenCl.CreateProgramWithSource(context.Handle,
                1,
                code,
                Span<UIntPtr>.Empty,
                Span<int>.Empty
            );

            if (result == IntPtr.Zero)
            {
                throw new Exception("Failed to create compute program!");
            }

            return new DeviceProgram(context, result);
        }

        public unsafe void Build()
        {
            var error = (CLEnum) Bindings.OpenCl.BuildProgram(Handle,
                0,
                Span<IntPtr>.Empty,
                Span<char>.Empty,
                null,
                Span<byte>.Empty
            );

            if (error == CLEnum.Success) return;
            
            var buffer = new byte[2048];

            var length = new UIntPtr[1];

            Bindings.OpenCl.GetProgramBuildInfo(Handle,
                Context.Accelerator.Handle,
                (uint) CLEnum.ProgramBuildLog,
                (UIntPtr) buffer.Length,
                new Span<byte>(buffer),
                new Span<UIntPtr>(length)
            );

            Array.Resize(ref buffer, (int) length[default]);

            var str = new string(buffer.Select(b => (char) b).ToArray());

            throw new Exception($"Failed to build device program!\n{str}");
        }

        public Kernel BuildKernel(string name)
        {
            var kernel = Kernels.FirstOrDefault(k => k.Name == name);

            if (kernel != default)
            {
                return kernel;
            }

            var result = Bindings.OpenCl.CreateKernel(Handle, name, Span<int>.Empty);

            if (result == IntPtr.Zero)
            {
                throw new Exception("Failed to create compute kernel!");
            }

            kernel = new Kernel(this, result, name);

            Kernels.Add(kernel);

            return kernel;
        }

        ~DeviceProgram()
        {
            ReleaseUnmanagedResources();
        }

        private void ReleaseUnmanagedResources()
        {
            foreach (var kernel in Kernels)
            {
                kernel.Dispose();
            }

            Kernels.Clear();

            Context.DevicePrograms.Remove(this);

            Bindings.OpenCl.ReleaseProgram(Handle);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
    }
}