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

            var error = (CLEnum) Bindings.OpenCl.GetKernelWorkGroupInfo(
                Handle,
                Program.Context.Accelerator.Handle,
                (uint) CLEnum.KernelWorkGroupSize,
                (UIntPtr) UIntPtr.Size,
                new Span<UIntPtr>(size),
                Span<UIntPtr>.Empty
            );

            if (error != CLEnum.Success)
            {
                throw new Exception("Failed to get max kernel group size!");
            }

            return (uint) size[default];
        }

        public void Invoke(uint workers, params KernelArgument[] parameters)
        {
            CLEnum error;

            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];

                var arg = new[]
                {
                    parameter.Value
                };

                error = (CLEnum) Bindings.OpenCl.SetKernelArg(
                    Handle,
                    (uint) index,
                    (UIntPtr) parameter.Size,
                    new Span<UIntPtr>(arg)
                );

                if (error != CLEnum.Success)
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

            error = (CLEnum) Bindings.OpenCl.EnqueueNdrangeKernel(
                Program.Context.Queue,
                Handle,
                1,
                Span<UIntPtr>.Empty,
                new Span<UIntPtr>(global),
                new Span<UIntPtr>(local),
                0,
                Span<IntPtr>.Empty,
                Span<IntPtr>.Empty
            );

            if (error != CLEnum.Success)
            {
                throw new Exception("Failed to invoke kernel!");
            }

            Bindings.OpenCl.Finish(Program.Context.Queue);
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