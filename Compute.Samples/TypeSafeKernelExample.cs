using System;
using System.Diagnostics;
using Compute.IL;
using Compute.Memory;

namespace Compute.Samples
{
    /// <summary>
    /// Example demonstrating the type-safe kernel wrapper that automatically manages SharedCollections
    /// </summary>
    internal static class TypeSafeKernelExample
    {
        [Kernel]
        public static void SimpleOperation([Global] float[] input, [Global] float[] output, [Const] uint count)
        {
            var id = BuiltIn.GetGlobalId(0);
            
            if (id >= count) return;
            
            output[id] = input[id] * 2.0f + 1.0f;
        }

        [Kernel]
        public static void MatrixAdd([Global] float[] a, [Global] float[] b, [Global] float[] result, [Const] uint width, [Const] uint height)
        {
            var x = BuiltIn.GetGlobalId(0);
            var y = BuiltIn.GetGlobalId(1);
            
            if (x >= width || y >= height) return;
            
            var index = y * width + x;
            result[index] = a[index] + b[index];
        }

        public static void RunTypeSafeExamples(Accelerator accelerator)
        {
            using var context = accelerator.CreateContext();
            var ilProgram = new ILProgram(context);

            Console.WriteLine($"\n=== Type-Safe Kernel Examples on {accelerator.Name} ===");

            // Example 1: Traditional way (for comparison)
            RunTraditionalExample(ilProgram);

            // Example 2: Type-safe way
            RunTypeSafeExample(ilProgram);

            // Example 3: 2D Type-safe example
            RunTypeSafe2DExample(ilProgram);
        }

        private static void RunTraditionalExample(ILProgram ilProgram)
        {
            Console.WriteLine("\n--- Traditional Approach ---");
            
            var kernel = ilProgram.Compile(SimpleOperation);
            
            const int size = 1024;
            var input = new float[size];
            var output = new float[size];
            
            for (var i = 0; i < size; i++)
            {
                input[i] = i + 1;
            }
            
            // Manual SharedCollection management
            using var inputBuffer = new SharedCollection<float>(ilProgram.Context, input, true);
            using var outputBuffer = new SharedCollection<float>(ilProgram.Context, output, true);
            
            var watch = Stopwatch.StartNew();
            
            // Manual UPtr extraction
            kernel.Invoke(size, inputBuffer.UPtr, outputBuffer.UPtr, (UIntPtr)size);
            
            watch.Stop();
            
            Console.WriteLine($"Traditional: {size} elements in {watch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Sample: input[0]={input[0]} -> output[0]={output[0]}");
        }

        private static void RunTypeSafeExample(ILProgram ilProgram)
        {
            Console.WriteLine("\n--- Type-Safe Approach ---");
            
            // Compile to type-safe wrapper
            using var kernel = ilProgram.CompileTypeSafe(SimpleOperation);
            
            const int size = 1024;
            var input = new float[size];
            var output = new float[size];
            
            for (var i = 0; i < size; i++)
            {
                input[i] = i + 1;
            }
            
            var watch = Stopwatch.StartNew();
            
            // Direct array usage - SharedCollections created automatically!
            kernel.Invoke(size, input, output, (uint)size);
            
            watch.Stop();
            
            Console.WriteLine($"Type-Safe: {size} elements in {watch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Sample: input[0]={input[0]} -> output[0]={output[0]}");
            Console.WriteLine("Note: Arrays were automatically converted to SharedCollections!");
        }

        private static void RunTypeSafe2DExample(ILProgram ilProgram)
        {
            Console.WriteLine("\n--- Type-Safe 2D Approach ---");
            
            // Compile to type-safe wrapper
            using var kernel = ilProgram.CompileTypeSafe(MatrixAdd);
            
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
            
            var watch = Stopwatch.StartNew();
            
            // Arrays automatically converted to SharedCollections
            var workers = new WorkerDimensions(width, height);
            kernel.Invoke(workers, a, b, result, width, height);
            
            watch.Stop();
            
            Console.WriteLine($"Type-Safe 2D: {width}x{height} matrix in {watch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Sample: a[0] + b[0] = {a[0]} + {b[0]} = {result[0]}");
            Console.WriteLine("Note: All arrays managed automatically with proper disposal!");
        }
    }
}
