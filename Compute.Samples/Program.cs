using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Compute.IL;
using Compute.IL.AST;
using Compute.IL.AST.CodeGeneration;
using Compute.IL.AST.Statements;
using Compute.Memory;

namespace Compute.Samples
{
    internal static class Program
    {
        public static Matrix4x4 Multiply([ByValue] Matrix4x4 value1, [ByValue] Matrix4x4 value2)
        {
            Matrix4x4 m;

            // First row
            m.M11 = value1.M11 * value2.M11 + value1.M12 * value2.M21 + value1.M13 * value2.M31 +
                    value1.M14 * value2.M41;
            m.M12 = value1.M11 * value2.M12 + value1.M12 * value2.M22 + value1.M13 * value2.M32 +
                    value1.M14 * value2.M42;
            m.M13 = value1.M11 * value2.M13 + value1.M12 * value2.M23 + value1.M13 * value2.M33 +
                    value1.M14 * value2.M43;
            m.M14 = value1.M11 * value2.M14 + value1.M12 * value2.M24 + value1.M13 * value2.M34 +
                    value1.M14 * value2.M44;

            // Second row
            m.M21 = value1.M21 * value2.M11 + value1.M22 * value2.M21 + value1.M23 * value2.M31 +
                    value1.M24 * value2.M41;
            m.M22 = value1.M21 * value2.M12 + value1.M22 * value2.M22 + value1.M23 * value2.M32 +
                    value1.M24 * value2.M42;
            m.M23 = value1.M21 * value2.M13 + value1.M22 * value2.M23 + value1.M23 * value2.M33 +
                    value1.M24 * value2.M43;
            m.M24 = value1.M21 * value2.M14 + value1.M22 * value2.M24 + value1.M23 * value2.M34 +
                    value1.M24 * value2.M44;

            // Third row
            m.M31 = value1.M31 * value2.M11 + value1.M32 * value2.M21 + value1.M33 * value2.M31 +
                    value1.M34 * value2.M41;
            m.M32 = value1.M31 * value2.M12 + value1.M32 * value2.M22 + value1.M33 * value2.M32 +
                    value1.M34 * value2.M42;
            m.M33 = value1.M31 * value2.M13 + value1.M32 * value2.M23 + value1.M33 * value2.M33 +
                    value1.M34 * value2.M43;
            m.M34 = value1.M31 * value2.M14 + value1.M32 * value2.M24 + value1.M33 * value2.M34 +
                    value1.M34 * value2.M44;

            // Fourth row
            m.M41 = value1.M41 * value2.M11 + value1.M42 * value2.M21 + value1.M43 * value2.M31 +
                    value1.M44 * value2.M41;
            m.M42 = value1.M41 * value2.M12 + value1.M42 * value2.M22 + value1.M43 * value2.M32 +
                    value1.M44 * value2.M42;
            m.M43 = value1.M41 * value2.M13 + value1.M42 * value2.M23 + value1.M43 * value2.M33 +
                    value1.M44 * value2.M43;
            m.M44 = value1.M41 * value2.M14 + value1.M42 * value2.M24 + value1.M43 * value2.M34 +
                    value1.M44 * value2.M44;

            return m;
        }

        [Kernel]
        public static void ExampleKernel([Global] Matrix4x4[] input, [Global] Matrix4x4[] output, [Const] uint count)
        {
            Float4 value = new Float4();
            value.X = 1.0f;
            value.Y = 2.0f;
            value.Z = 3.0f;
            value.W = 4.0f;

            value.X += value.Y;

            var val2 = value.WZYX;
            
            value *= val2;

            var id = BuiltIn.GetGlobalId(0);

            if (id >= count) return;

            var matrix = input[id];

            for (var i = 0; i < 100; i++)
            {
                matrix = Multiply(matrix, matrix);
            }

            output[id] = matrix;
        }

        // Simple kernel for AST demonstration
        [Kernel]
        public static void SimpleAstKernel([Global] float[] input, [Global] float[] output, [Const] uint count)
        {
            var id = BuiltIn.GetGlobalId(0);
            
            if (id >= count) return;
            
            var value = input[id];
            output[id] = value * 2.0f + 1.0f;
        }

        private static void PrintDetails<T>(T instance)
        {
            foreach (var property in typeof(T).GetProperties())
            {
                object value;

                try
                {
                    value = property.GetValue(instance);
                }
                catch
                {
                    Console.WriteLine($"{property.Name} = Error!");
                    
                    continue;
                }
                
                if (value is IEnumerable enumerable && !(value is string))
                {
                    var objects = new List<object>();
                    
                    foreach (var obj in enumerable)
                    {
                        objects.Add(obj);
                    }

                    value = string.Join(", ", objects);
                }
                
                Console.WriteLine($"{property.Name} = {value}");
            }
        }

        private static void Main()
        {
            foreach (var platform in Platform.Platforms)
            {
                PrintDetails(platform);

                Console.WriteLine();

                foreach (var entry in platform.Accelerators)
                {
                    PrintDetails(entry);
                    
                    // Demonstrate AST-based compilation alongside traditional approach
                    //DemonstrateAstVsTraditional(entry);

                    // Demonstrate multi-dimensional worker support
                    //MultiDimensionalExample.RunMultiDimensionalExamples(entry);

                    // Demonstrate type-safe kernel wrapper
                    //TypeSafeKernelExample.RunTypeSafeExamples(entry);

                    // Demonstrate N-body simulation
                    NBodySimulation.RunNBodyExample(entry);

                    RunAccelerator(entry);
                }

                Console.WriteLine($"Done with: {platform.Name}");
            }
        }
        public static void RunAccelerator(Accelerator accelerator)
        {
            using var context = accelerator.CreateContext();

            var watch = new Stopwatch();
            watch.Start();

            var astProgram = new AstProgram(context, new OpenClCodeGenerator());

            var astKernel = astProgram.Compile(ExampleKernel, out string source);

            File.WriteAllText("kernel.cl", source); // Save source for debugging

            if (astKernel == null)
            {
                Console.WriteLine("AST compilation failed - aborting test");
                return;
            }

            Console.WriteLine($"Compile kernel: {watch.ElapsedMilliseconds}ms");

            const int size = 1024 * 1000;
            const int rounds = 25;

            var random = new Random();

            var totalFail = 0L;
            var totalGpu = 0L;
            var totalCpu = 0L;

            var data = new Matrix4x4[size];

            var results = new Matrix4x4[size];

            using var input = new SharedCollection<Matrix4x4>(context, size);

            using var output = new SharedCollection<Matrix4x4>(context, size);

            for (var j = 0; j < rounds; j++)
            {
                for (var i = 0; i < size; i++)
                {
                    data[i].M11 = random.Next(1, 1000);
                    data[i].M12 = random.Next(1, 1000);
                    data[i].M13 = random.Next(1, 1000);
                    data[i].M14 = random.Next(1, 1000);
                    data[i].M21 = random.Next(1, 1000);
                    data[i].M22 = random.Next(1, 1000);
                    data[i].M23 = random.Next(1, 1000);
                    data[i].M24 = random.Next(1, 1000);
                    data[i].M31 = random.Next(1, 1000);
                    data[i].M32 = random.Next(1, 1000);
                    data[i].M33 = random.Next(1, 1000);
                    data[i].M34 = random.Next(1, 1000);
                    data[i].M41 = random.Next(1, 1000);
                    data[i].M42 = random.Next(1, 1000);
                    data[i].M43 = random.Next(1, 1000);
                    data[i].M44 = random.Next(1, 1000);
                }

                watch.Restart();

                input.CopyToDevice(data);
                
                astKernel(size, input, output, size);

                output.CopyToHostNonAlloc(results);

                var gpu = watch.ElapsedMilliseconds;

                watch.Restart();
                
                for (var i = 0; i < size; i++)
                {
                    var value = data[i];

                    for (var k = 0; k < 100; k++)
                    {
                        value = Multiply(value, value);
                    }

                    data[i] = value;
                }

                var cpu = watch.ElapsedMilliseconds;

                watch.Stop();

                var failed = 0;

                for (var i = 0; i < size; i++)
                {
                    if (!results[i].Equals(data[i]))
                    {
                        failed++;
                    }
                }

                if (failed == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                Console.WriteLine(
                    $"[{j + 1:0000}/{rounds:0000}] Kernel succeeded with {Percent(size, failed)} operations at {Percent(cpu, gpu, true)} IL speed!"
                );

                totalFail += failed;
                totalCpu += cpu;
                totalGpu += gpu;

                Console.ResetColor();
            }

            Console.WriteLine(
                $"Ran {rounds} rounds in sets of {size}, total operations: {rounds * size}\n" +
                $"Total success: {Percent(size * rounds, totalFail)} with {totalFail} errors\n" +
                $"Total speed: {Percent(totalCpu, totalGpu, true)} IL speed with {totalCpu}ms CPU-time and {totalGpu}ms GPU-time"
            );

            accelerator.Dispose();
        }

        public static string Percent(double a, double b, bool overflow = false)
        {
            if (b.Equals(0))
            {
                if (overflow)
                {
                    b = 1;
                }
                else
                {
                    return "100%";
                }
            }

            var percent = a / b * 100;

            percent = Math.Round(percent, 2);

            return $"{percent}%";
        }
    }
}