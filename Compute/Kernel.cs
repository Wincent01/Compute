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

        private static void CalculateLocalWorkSizes(UIntPtr[] globalWork, UIntPtr[] localWork, uint maxGroupSize, uint dimensions)
        {
            switch (dimensions)
            {
                case 1:
                    // For 1D, find the largest divisor of global size that doesn't exceed max group size
                    localWork[0] = (UIntPtr)CalculateOptimal1D((uint)globalWork[0], maxGroupSize);
                    break;
                    
                case 2:
                    // For 2D, distribute the work group size across both dimensions
                    CalculateOptimal2D((uint)globalWork[0], (uint)globalWork[1], maxGroupSize, out var localX, out var localY);
                    localWork[0] = (UIntPtr)localX;
                    localWork[1] = (UIntPtr)localY;
                    break;
                    
                case 3:
                    // For 3D, distribute across all three dimensions
                    CalculateOptimal3D((uint)globalWork[0], (uint)globalWork[1], (uint)globalWork[2], maxGroupSize, 
                        out var localX3D, out var localY3D, out var localZ3D);
                    localWork[0] = (UIntPtr)localX3D;
                    localWork[1] = (UIntPtr)localY3D;
                    localWork[2] = (UIntPtr)localZ3D;
                    break;
                    
                default:
                    throw new ArgumentException($"Unsupported number of dimensions: {dimensions}");
            }
        }

        private static uint CalculateOptimal1D(uint globalSize, uint maxGroupSize)
        {
            // Find the largest power of 2 that divides globalSize and doesn't exceed maxGroupSize
            var optimal = 1u;
            for (var size = 2u; size <= maxGroupSize && size <= globalSize; size *= 2)
            {
                if (globalSize % size == 0)
                    optimal = size;
            }
            
            // If no power of 2 works well, try other divisors
            if (optimal == 1)
            {
                for (var size = Math.Min(maxGroupSize, globalSize); size >= 1; size--)
                {
                    if (globalSize % size == 0)
                        return size;
                }
            }
            
            return optimal;
        }

        private static void CalculateOptimal2D(uint globalX, uint globalY, uint maxGroupSize, out uint localX, out uint localY)
        {
            // Start with a square-ish distribution
            var sqrtMax = (uint)Math.Sqrt(maxGroupSize);
            
            localX = Math.Min(sqrtMax, globalX);
            localY = Math.Min(maxGroupSize / localX, globalY);
            
            // Ensure we don't exceed the max group size
            while (localX * localY > maxGroupSize)
            {
                if (localX > localY)
                    localX--;
                else
                    localY--;
            }
            
            // Try to make them divisors of the global sizes
            localX = FindBestDivisor(globalX, localX);
            localY = FindBestDivisor(globalY, localY);
            
            // Final check to ensure we don't exceed max group size
            while (localX * localY > maxGroupSize)
            {
                if (localX > localY)
                    localX = FindBestDivisor(globalX, localX - 1);
                else
                    localY = FindBestDivisor(globalY, localY - 1);
            }
        }

        private static void CalculateOptimal3D(uint globalX, uint globalY, uint globalZ, uint maxGroupSize, 
            out uint localX, out uint localY, out uint localZ)
        {
            // Start with a cube-ish distribution
            var cubeRoot = (uint)Math.Pow(maxGroupSize, 1.0 / 3.0);
            
            localX = Math.Min(cubeRoot, globalX);
            localY = Math.Min(cubeRoot, globalY);
            localZ = Math.Min(maxGroupSize / (localX * localY), globalZ);
            
            // Ensure we don't exceed the max group size
            while (localX * localY * localZ > maxGroupSize)
            {
                if (localX >= localY && localX >= localZ)
                    localX--;
                else if (localY >= localZ)
                    localY--;
                else
                    localZ--;
            }
            
            // Try to make them divisors of the global sizes
            localX = FindBestDivisor(globalX, localX);
            localY = FindBestDivisor(globalY, localY);
            localZ = FindBestDivisor(globalZ, localZ);
            
            // Final check
            while (localX * localY * localZ > maxGroupSize)
            {
                if (localX >= localY && localX >= localZ)
                    localX = FindBestDivisor(globalX, localX - 1);
                else if (localY >= localZ)
                    localY = FindBestDivisor(globalY, localY - 1);
                else
                    localZ = FindBestDivisor(globalZ, localZ - 1);
            }
        }

        private static uint FindBestDivisor(uint number, uint maxValue)
        {
            if (maxValue == 0) return 1;
            
            for (var i = maxValue; i >= 1; i--)
            {
                if (number % i == 0)
                    return i;
            }
            return 1;
        }

        public unsafe void Invoke(WorkerDimensions workers, Span<KernelArgument> parameters)
        {
            ErrorCodes error;

            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];

                var arg = parameter.Value;

                error = (ErrorCodes) Bindings.OpenCl.SetKernelArg(
                    Handle,
                    (uint) index,
                    parameter.Size,
                    &arg
                );

                if (error != ErrorCodes.Success)
                {
                    throw new Exception("Failed to set kernel argument!");
                }
            }

            var groupSize = GetGroupSize();
            var dimensions = workers.DimensionCount;
            var globalWork = workers.ToArray();
            var localWork = new UIntPtr[dimensions];

            // Calculate optimal local work group sizes for each dimension
            CalculateLocalWorkSizes(globalWork, localWork, groupSize, dimensions);

            fixed (UIntPtr* globalPtr = globalWork)
            fixed (UIntPtr* localPtr = localWork)
            {
                error = (ErrorCodes) Bindings.OpenCl.EnqueueNdrangeKernel(
                    Program.Context.Queue,
                    Handle,
                    dimensions,
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
                throw new Exception("Failed to invoke kernel, error: " + error);
            }
            
            error = (ErrorCodes) Bindings.OpenCl.Finish(Program.Context.Queue);

            if (error != ErrorCodes.Success)
            {
                throw new Exception($"Failed to finish kernel call, error: {error}");
            }
        }

        /// <summary>
        /// Invokes the kernel with automatic local work group size determination.
        /// This is often safer for complex scenarios where manual calculation might fail.
        /// </summary>
        public unsafe void InvokeAuto(WorkerDimensions workers, Span<KernelArgument> parameters)
        {
            ErrorCodes error;

            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];

                var arg = parameter.Value;

                error = (ErrorCodes) Bindings.OpenCl.SetKernelArg(
                    Handle,
                    (uint) index,
                    parameter.Size,
                    &arg
                );

                if (error != ErrorCodes.Success)
                {
                    throw new Exception("Failed to set kernel argument!");
                }
            }

            var dimensions = workers.DimensionCount;
            var globalWork = workers.ToArray();

            fixed (UIntPtr* globalPtr = globalWork)
            {
                error = (ErrorCodes) Bindings.OpenCl.EnqueueNdrangeKernel(
                    Program.Context.Queue,
                    Handle,
                    dimensions,
                    null,
                    globalPtr,
                    null, // Let OpenCL determine optimal local work group sizes
                    0,
                    null,
                    null
                );
            }

            if (error != ErrorCodes.Success)
            {
                throw new Exception("Failed to invoke kernel with auto local work size, error: " + error);
            }
            
            error = (ErrorCodes) Bindings.OpenCl.Finish(Program.Context.Queue);

            if (error != ErrorCodes.Success)
            {
                throw new Exception($"Failed to finish kernel call, error: {error}");
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