using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Compute.IL.Compiler;
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

        public static unsafe DeviceProgram FromSource(Context context, string source)
        {
            var sourceBytes = System.Text.Encoding.UTF8.GetBytes(source + "\0");
            var lengths = new UIntPtr[] { (UIntPtr)sourceBytes.Length };
            var error = new int[1];

            fixed (byte* sourceBytesPtr = sourceBytes)
            fixed (UIntPtr* lengthsPtr = lengths)
            {
                var sourcePtrs = stackalloc byte*[1];
                sourcePtrs[0] = sourceBytesPtr;

                var result = Bindings.OpenCl.CreateProgramWithSource(context.Handle,
                    1,
                    sourcePtrs,
                    lengthsPtr,
                    error
                );

                if (result == IntPtr.Zero)
                {
                    throw new Exception("Failed to create compute program!");
                }

                return new DeviceProgram(context, result);
            }
        }

        public unsafe void Build()
        {
            var error = (ErrorCodes) Bindings.OpenCl.BuildProgram(Handle,
                0,
                (IntPtr*)null,
                (string)null,
                null,
                null
            );

            if (error == ErrorCodes.Success) return;
            
            var buffer = new byte[2048];

            var length = new UIntPtr[1];

            Bindings.OpenCl.GetProgramBuildInfo(Handle,
                Context.Accelerator.Handle,
                ProgramBuildInfo.BuildLog,
                (UIntPtr) buffer.Length,
                new Span<byte>(buffer),
                new Span<UIntPtr>(length)
            );

            Array.Resize(ref buffer, (int) length[default]);

            var str = new string(buffer.Select(b => (char) b).ToArray());

            throw new Exception($"Failed to build device program!\nError: [{error}]\nMessage: {str}");
        }


        public Kernel BuildKernel(MethodInfo method)
        {
            return BuildKernel(CLGenerator.GenerateKernelName(method));
        }

        public Kernel BuildKernel(string name)
        {
            var kernel = Kernels.FirstOrDefault(k => k.Name == name);

            if (kernel != default)
            {
                return kernel;
            }

            var result = Bindings.OpenCl.CreateKernel(Handle, name, out var error);

            if (result == IntPtr.Zero)
            {
                throw new Exception($"Failed to create compute kernel!\nError: [{(ErrorCodes)error}]");
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
            foreach (var kernel in Kernels.ToArray())
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