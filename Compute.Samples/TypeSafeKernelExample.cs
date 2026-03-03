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
        public static void MatrixAdd([Global] float[] a, [Global] float[] b, [Global] float[] result, [Const] uint count)
        {
            var id = BuiltIn.GetGlobalId(0);

            if (id >= count) return;

            result[id] = a[id] + b[id];
        }

        public static SampleResult RunTypeSafeExamples(Accelerator accelerator)
        {
            using var context = accelerator.CreateContext();
            var ilProgram = new ILProgram(context);

            Console.WriteLine($"\n=== Type-Safe Kernel Examples on {accelerator.Name} ===");

            // Example 1: Traditional way (for comparison)
            var traditional = RunTraditionalExample(ilProgram);

            // Example 2: Type-safe way
            var typeSafe = RunTypeSafeExample(ilProgram);

            // Example 3: 2D Type-safe example
            var typeSafe2D = RunTypeSafeMatrixAddExample(ilProgram);

            return new SampleResult
            {
                Name = "Type-Safe Kernel Wrapper",
                Passed = traditional.passed && typeSafe.passed && typeSafe2D.passed,
                CpuMilliseconds = traditional.cpuMs + typeSafe.cpuMs + typeSafe2D.cpuMs,
                GpuMilliseconds = traditional.gpuMs + typeSafe.gpuMs + typeSafe2D.gpuMs,
                Details = $"traditional={(traditional.passed ? "PASS" : "FAIL")}, type-safe={(typeSafe.passed ? "PASS" : "FAIL")}, type-safe-2d={(typeSafe2D.passed ? "PASS" : "FAIL")}"
            };
        }

        private static (bool passed, long gpuMs, long cpuMs) RunTraditionalExample(ILProgram ilProgram)
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
            using var inputBuffer = new SharedCollection<float>(ilProgram.Context, size);
            using var outputBuffer = new SharedCollection<float>(ilProgram.Context, size);
            inputBuffer.CopyToDevice(input);
            
            var watch = Stopwatch.StartNew();
            
            // Manual UPtr extraction
            kernel.Invoke(size, inputBuffer.UPtr, outputBuffer.UPtr, (uint)size);

            watch.Stop();
            var gpuMs = watch.ElapsedMilliseconds;

            outputBuffer.CopyToHostNonAlloc(output);

            watch.Restart();
            var expected = new float[size];
            for (var i = 0; i < size; i++)
            {
                expected[i] = input[i] * 2.0f + 1.0f;
            }
            watch.Stop();
            var cpuMs = watch.ElapsedMilliseconds;

            var passed = VerifyNear(expected, output, 1e-5f);
            
            Console.WriteLine($"Traditional: {size} elements in {gpuMs}ms");
            Console.WriteLine($"Sample: input[0]={input[0]} -> output[0]={output[0]}");
            Console.WriteLine($"Correctness: {(passed ? "PASS" : "FAIL")}");

            return (passed, gpuMs, cpuMs);
        }

        private static (bool passed, long gpuMs, long cpuMs) RunTypeSafeExample(ILProgram ilProgram)
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
            var gpuMs = watch.ElapsedMilliseconds;

            watch.Restart();
            var expected = new float[size];
            for (var i = 0; i < size; i++)
            {
                expected[i] = input[i] * 2.0f + 1.0f;
            }
            watch.Stop();
            var cpuMs = watch.ElapsedMilliseconds;

            var passed = VerifyNear(expected, output, 1e-5f);
            
            Console.WriteLine($"Type-Safe: {size} elements in {gpuMs}ms");
            Console.WriteLine($"Sample: input[0]={input[0]} -> output[0]={output[0]}");
            Console.WriteLine("Note: Arrays were automatically converted to SharedCollections!");
            Console.WriteLine($"Correctness: {(passed ? "PASS" : "FAIL")}");

            return (passed, gpuMs, cpuMs);
        }

        private static (bool passed, long gpuMs, long cpuMs) RunTypeSafeMatrixAddExample(ILProgram ilProgram)
        {
            Console.WriteLine("\n--- Type-Safe Matrix Add Approach ---");
            
            // Compile to type-safe wrapper
            using var kernel = ilProgram.CompileTypeSafe(MatrixAdd);
            
            const uint count = 32 * 32;
            var size = (int)count;
            
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
            var workers = new WorkerDimensions(count);
            kernel.Invoke(workers, a, b, result, count);

            watch.Stop();
            var gpuMs = watch.ElapsedMilliseconds;

            watch.Restart();
            var expected = new float[size];
            for (var i = 0; i < size; i++)
            {
                expected[i] = a[i] + b[i];
            }
            watch.Stop();
            var cpuMs = watch.ElapsedMilliseconds;

            var passed = VerifyNear(expected, result, 1e-5f);
            
            Console.WriteLine($"Type-Safe Matrix Add: {size} elements in {gpuMs}ms");
            Console.WriteLine($"Sample: a[0] + b[0] = {a[0]} + {b[0]} = {result[0]}");
            Console.WriteLine("Note: All arrays managed automatically with proper disposal!");
            Console.WriteLine($"Correctness: {(passed ? "PASS" : "FAIL")}");

            return (passed, gpuMs, cpuMs);
        }

        private static bool VerifyNear(float[] expected, float[] actual, float tolerance)
        {
            if (expected.Length != actual.Length)
            {
                return false;
            }

            for (var i = 0; i < expected.Length; i++)
            {
                if (MathF.Abs(expected[i] - actual[i]) > tolerance)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
