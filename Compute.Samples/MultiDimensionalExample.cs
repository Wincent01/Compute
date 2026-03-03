using System;
using System.Diagnostics;
using Compute.IL;
using Compute.IL.AST.Lambda;

namespace Compute.Samples
{
    internal static class MultiDimensionalExample
    {
        public static SampleResult RunMultiDimensionalExamples(Accelerator accelerator)
        {
            using var context = accelerator.CreateContext();

            Console.WriteLine($"\n=== Multi-Dimensional Worker Examples on {accelerator.Name} ===");

            var oneD = Run1DExample(context);
            var twoD = Run2DExample(context);
            var threeD = Run3DExample(context);

            return new SampleResult
            {
                Name = "Worker Dimensions (1D/2D/3D)",
                Passed = oneD.passed && twoD.passed && threeD.passed,
                CpuMilliseconds = oneD.cpuMs + twoD.cpuMs + threeD.cpuMs,
                GpuMilliseconds = oneD.gpuMs + twoD.gpuMs + threeD.gpuMs,
                Details = $"1D={(oneD.passed ? "PASS" : "FAIL")}, 2D={(twoD.passed ? "PASS" : "FAIL")}, 3D={(threeD.passed ? "PASS" : "FAIL")}"
            };
        }

        private static (bool passed, long gpuMs, long cpuMs) Run1DExample(Context context)
        {
            Console.WriteLine("\n--- 1D Worker Example ---");

            const int size = 1024;
            var input = new float[size];
            var output = new float[size];

            for (var i = 0; i < size; i++)
            {
                input[i] = i + 1;
            }

            var watch = Stopwatch.StartNew();

            var workers = Grid.Size((uint)size);
            using var parallel = Parallel.Prepare(context, () =>
            {
                var i = KernelThread.Global.X;
                if (i >= size) return;
                output[i] = input[i] * input[i];
            });

            parallel.Run(workers);
            watch.Stop();
            var gpuMs = watch.ElapsedMilliseconds;

            watch.Restart();
            var expected = new float[size];
            for (var i = 0; i < size; i++)
            {
                expected[i] = input[i] * input[i];
            }
            watch.Stop();
            var cpuMs = watch.ElapsedMilliseconds;

            var passed = VerifyNear(expected, output, 1e-5f);

            Console.WriteLine($"1D Processing: {size} elements in {gpuMs}ms");
            Console.WriteLine($"Workers: {workers}");
            Console.WriteLine($"Sample results: input[0]={input[0]} -> output[0]={output[0]}, input[10]={input[10]} -> output[10]={output[10]}");
            Console.WriteLine($"Correctness: {(passed ? "PASS" : "FAIL")}");

            return (passed, gpuMs, cpuMs);
        }

        private static (bool passed, long gpuMs, long cpuMs) Run2DExample(Context context)
        {
            Console.WriteLine("\n--- 2D Worker Example ---");

            const int width = 64;
            const int height = 64;
            var size = width * height;

            var a = new float[size];
            var b = new float[size];
            var result = new float[size];

            var random = new Random(17);
            for (var i = 0; i < size; i++)
            {
                a[i] = random.NextSingle();
                b[i] = random.NextSingle();
            }

            var watch = Stopwatch.StartNew();

            var workers = Grid.Size((uint)width, (uint)height);
            using var parallel = Parallel.Prepare(context, () =>
            {
                var x = KernelThread.Global.X;
                var y = KernelThread.Global.Y;
                if (x >= width) return;
                if (y >= height) return;

                var index = y * width + x;
                result[index] = a[index] * b[index];
            });

            parallel.Run(workers);
            watch.Stop();
            var gpuMs = watch.ElapsedMilliseconds;

            watch.Restart();
            var expected = new float[size];
            for (var i = 0; i < size; i++)
            {
                expected[i] = a[i] * b[i];
            }
            watch.Stop();
            var cpuMs = watch.ElapsedMilliseconds;

            var passed = VerifyNear(expected, result, 1e-5f);

            Console.WriteLine($"2D Processing: {width}x{height} matrix in {gpuMs}ms");
            Console.WriteLine($"Workers: {workers}");
            Console.WriteLine($"Sample result: a[0]*b[0] = {a[0]} * {b[0]} = {result[0]}");
            Console.WriteLine($"Correctness: {(passed ? "PASS" : "FAIL")}");

            return (passed, gpuMs, cpuMs);
        }

        private static (bool passed, long gpuMs, long cpuMs) Run3DExample(Context context)
        {
            Console.WriteLine("\n--- 3D Worker Example ---");

            const int width = 16;
            const int height = 8;
            const int depth = 8;
            var size = width * height * depth;

            var data = new float[size];
            var output = new float[size];

            var random = new Random(21);
            for (var i = 0; i < size; i++)
            {
                data[i] = random.NextSingle();
            }

            var watch = Stopwatch.StartNew();

            var workers = Grid.Size((uint)width, (uint)height, (uint)depth);
            using var parallel = Parallel.Prepare(context, () =>
            {
                var x = KernelThread.Global.X;
                var y = KernelThread.Global.Y;
                var z = KernelThread.Global.Z;
                if (x >= width) return;
                if (y >= height) return;
                if (z >= depth) return;

                var index = z * (width * height) + y * width + x;
                output[index] = data[index] * 2.0f + 1.0f;
            });

            parallel.Run(workers);
            watch.Stop();
            var gpuMs = watch.ElapsedMilliseconds;

            watch.Restart();
            var expected = new float[size];
            for (var i = 0; i < size; i++)
            {
                expected[i] = data[i] * 2.0f + 1.0f;
            }
            watch.Stop();
            var cpuMs = watch.ElapsedMilliseconds;

            var passed = VerifyNear(expected, output, 1e-5f);

            Console.WriteLine($"3D Processing: {width}x{height}x{depth} volume in {gpuMs}ms");
            Console.WriteLine($"Workers: {workers}");
            Console.WriteLine($"Sample result: data[0] = {data[0]} -> output[0] = {output[0]} (should be data*2+1)");
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
