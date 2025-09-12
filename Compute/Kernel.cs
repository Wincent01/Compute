using System;
using System.Runtime.InteropServices;
using Compute.Memory;
using Silk.NET.OpenCL;

namespace Compute
{
    public class Kernel : IDisposable
    {
        public DeviceProgram Program { get; }

        public IntPtr Handle { get; }

        public string Name { get; }

        public Kernel(DeviceProgram program, IntPtr handle, string name)
        {
            Program = program;

            Handle = handle;

            Name = name;
        }

        public uint GetGroupSize()
        {
            var size = new UIntPtr[1];

            var error = (ErrorCodes) Bindings.OpenCl.GetKernelWorkGroupInfo(
                Handle,
                Program.Context.Accelerator.Handle,
                KernelWorkGroupInfo.WorkGroupSize,
                (UIntPtr) UIntPtr.Size,
                new Span<UIntPtr>(size),
                Span<UIntPtr>.Empty
            );

            if (error != ErrorCodes.Success)
            {
                throw new Exception("Failed to get max kernel group size!");
            }

            return (uint) size[default];
        }

        public unsafe void Invoke(uint workers, params KernelArgument[] parameters)
        {
            ErrorCodes error;

            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];

                var arg = parameter.Value;

                error = (ErrorCodes) Bindings.OpenCl.SetKernelArg(
                    Handle,
                    (uint) index,
                    (UIntPtr) parameter.Size,
                    &arg
                );

                if (error != ErrorCodes.Success)
                {
                    throw new Exception("Failed to set kernel argument!");
                }
            }

            var local = new[]
            {
                (UIntPtr) GetGroupSize()
            };

            var global = new[]
            {
                (UIntPtr) workers
            };

            fixed (UIntPtr* globalPtr = global)
            fixed (UIntPtr* localPtr = local)
            {
                error = (ErrorCodes) Bindings.OpenCl.EnqueueNdrangeKernel(
                    Program.Context.Queue,
                    Handle,
                    1,
                    null,
                    globalPtr,
                    localPtr,
                    0,
                    null,
                    null
                );
            }

            if (error != ErrorCodes.Success)
            {
                throw new Exception("Failed to invoke kernel!");
            }
            
            error = (ErrorCodes) Bindings.OpenCl.Finish(Program.Context.Queue);

            if (error != ErrorCodes.Success)
            {
                throw new Exception($"Failed to finish kernel call!");
            }
        }

        ~Kernel()
        {
            ReleaseUnmanagedResources();
        }

        private void ReleaseUnmanagedResources()
        {
            Program.Kernels.Remove(this);

            Bindings.OpenCl.ReleaseKernel(Handle);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
    }
}