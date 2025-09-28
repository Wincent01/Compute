using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Compute.IL;
using Compute.Memory;

namespace Compute.Samples
{
    /// <summary>
    /// Example demonstrating multi-dimensional worker configurations for kernels.
    /// This shows how to use 1D, 2D, and 3D worker dimensions with the new WorkerDimensions struct.
    /// </summary>
    internal static class MultiDimensionalExample
    {
        [Kernel]
        public static void MatrixMultiply2D([Global] float[] a, [Global] float[] b, [Global] float[] result, [Const] uint width, [Const] uint height)
        {
            var x = BuiltIn.GetGlobalId(0);
            var y = BuiltIn.GetGlobalId(1);
            
            if (x >= width || y >= height) return;
            
            var index = y * width + x;
            result[index] = a[index] * b[index];
        }

        [Kernel]
        public static void Process3D([Global] float[] data, [Global] float[] output, [Const] uint width, [Const] uint height, [Const] uint depth)
        {
            var x = BuiltIn.GetGlobalId(0);
            var y = BuiltIn.GetGlobalId(1);
            var z = BuiltIn.GetGlobalId(2);
            
            if (x >= width || y >= height || z >= depth) return;
            
            var index = z * width * height + y * width + x;
            output[index] = data[index] * 2.0f + 1.0f;
        }

        [Kernel]
        public static void Simple1D([Global] float[] input, [Global] float[] output, [Const] uint count)
        {
            var id = BuiltIn.GetGlobalId(0);
            
            if (id >= count) return;
            
            output[id] = input[id] * input[id];
        }

        public static void RunMultiDimensionalExamples(Accelerator accelerator)
        {
            using var context = accelerator.CreateContext();
            var ilProgram = new ILProgram(context);

            Console.WriteLine($"\n=== Multi-Dimensional Worker Examples on {accelerator.Name} ===");

            // Example 1: 1D Workers (backward compatibility)
            Run1DExample(ilProgram);

            // Example 2: 2D Workers
            Run2DExample(ilProgram);

            // Example 3: 3D Workers
            Run3DExample(ilProgram);
        }

        private static void Run1DExample(ILProgram ilProgram)
        {
            Console.WriteLine("\n--- 1D Worker Example ---");
            
            var kernel = ilProgram.Compile(Simple1D);
            
            const int size = 1024;
            var input = new float[size];
            var output = new float[size];
            
            for (var i = 0; i < size; i++)
            {
                input[i] = i + 1;
            }
            
            using var inputBuffer = new SharedCollection<float>(ilProgram.Context, input, true);
            using var outputBuffer = new SharedCollection<float>(ilProgram.Context, output, true);
            
            var watch = Stopwatch.StartNew();
            
            // Using the new WorkerDimensions struct (1D)
            kernel.Invoke(size, inputBuffer.UPtr, outputBuffer.UPtr, (UIntPtr)size);
            
            // Or using implicit conversion from uint (backward compatibility)
            // kernel.Invoke(size, inputBuffer.UPtr, outputBuffer.UPtr, (UIntPtr)size);
            
            watch.Stop();
            
            Console.WriteLine($"1D Processing: {size} elements in {watch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Workers: {new WorkerDimensions(size)}");
            Console.WriteLine($"Sample results: input[0]={input[0]} -> output[0]={output[0]}, input[10]={input[10]} -> output[10]={output[10]}");
        }

        private static void Run2DExample(ILProgram ilProgram)
        {
            Console.WriteLine("\n--- 2D Worker Example ---");
            
            var kernel = ilProgram.Compile(MatrixMultiply2D);
            
            const uint width = 32;
            const uint height = 32;
            var size = (int)(width * height);
            
            var a = new float[size];
            var b = new float[size];    
            var result = new float[size];
            
            var random = new Random();
            for (var i = 0; i < size; i++)
            {
                a[i] = random.NextSingle();
                b[i] = random.NextSingle();
            }
            
            using var aBuffer = new SharedCollection<float>(ilProgram.Context, a, true);
            using var bBuffer = new SharedCollection<float>(ilProgram.Context, b, true);
            using var resultBuffer = new SharedCollection<float>(ilProgram.Context, result, true);
            
            var watch = Stopwatch.StartNew();
            
            // Using 2D worker dimensions
            var workers2D = new WorkerDimensions(width, height);
            kernel.Invoke(workers2D, aBuffer.UPtr, bBuffer.UPtr, resultBuffer.UPtr, (UIntPtr)width, (UIntPtr)height);
            
            watch.Stop();
            
            Console.WriteLine($"2D Processing: {width}x{height} matrix in {watch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Workers: {workers2D}");
            Console.WriteLine($"Total workers: {workers2D.TotalWorkers}");
            Console.WriteLine($"Sample result: a[0]*b[0] = {a[0]} * {b[0]} = {result[0]}");
        }

        private static void Run3DExample(ILProgram ilProgram)
        {
            Console.WriteLine("\n--- 3D Worker Example ---");
            
            var kernel = ilProgram.Compile(Process3D);
            
            const uint width = 8;
            const uint height = 8;
            const uint depth = 8;
            var size = (int)(width * height * depth);
            
            var data = new float[size];
            var output = new float[size];
            
            var random = new Random();
            for (var i = 0; i < size; i++)
            {
                data[i] = random.NextSingle();
            }
            
            using var dataBuffer = new SharedCollection<float>(ilProgram.Context, data, true);
            using var outputBuffer = new SharedCollection<float>(ilProgram.Context, output, true);
            
            var watch = Stopwatch.StartNew();
            
            // Using 3D worker dimensions
            var workers3D = new WorkerDimensions(width, height, depth);
            kernel.Invoke(workers3D, dataBuffer.UPtr, outputBuffer.UPtr, (UIntPtr)width, (UIntPtr)height, (UIntPtr)depth);

            watch.Stop();
            
            Console.WriteLine($"3D Processing: {width}x{height}x{depth} volume in {watch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Workers: {workers3D}");
            Console.WriteLine($"Dimensions: {workers3D.DimensionCount}");
            Console.WriteLine($"Total workers: {workers3D.TotalWorkers}");
            Console.WriteLine($"Sample result: data[0] = {data[0]} -> output[0] = {output[0]} (should be data*2+1)");
        }
    }
}
