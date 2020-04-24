using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reflection;
using Compute.IL;
using Compute.Memory;

namespace Compute.Samples
{
    internal static class Program
    {
        public static Matrix4x4 Multiply(Matrix4x4 value1, Matrix4x4 value2)
        {
            Matrix4x4 m;
            
            // First row
            m.M11 = value1.M11 * value2.M11 + value1.M12 * value2.M21 + value1.M13 * value2.M31 + value1.M14 * value2.M41;
            m.M12 = value1.M11 * value2.M12 + value1.M12 * value2.M22 + value1.M13 * value2.M32 + value1.M14 * value2.M42;
            m.M13 = value1.M11 * value2.M13 + value1.M12 * value2.M23 + value1.M13 * value2.M33 + value1.M14 * value2.M43;
            m.M14 = value1.M11 * value2.M14 + value1.M12 * value2.M24 + value1.M13 * value2.M34 + value1.M14 * value2.M44;

            // Second row
            m.M21 = value1.M21 * value2.M11 + value1.M22 * value2.M21 + value1.M23 * value2.M31 + value1.M24 * value2.M41;
            m.M22 = value1.M21 * value2.M12 + value1.M22 * value2.M22 + value1.M23 * value2.M32 + value1.M24 * value2.M42;
            m.M23 = value1.M21 * value2.M13 + value1.M22 * value2.M23 + value1.M23 * value2.M33 + value1.M24 * value2.M43;
            m.M24 = value1.M21 * value2.M14 + value1.M22 * value2.M24 + value1.M23 * value2.M34 + value1.M24 * value2.M44;

            // Third row
            m.M31 = value1.M31 * value2.M11 + value1.M32 * value2.M21 + value1.M33 * value2.M31 + value1.M34 * value2.M41;
            m.M32 = value1.M31 * value2.M12 + value1.M32 * value2.M22 + value1.M33 * value2.M32 + value1.M34 * value2.M42;
            m.M33 = value1.M31 * value2.M13 + value1.M32 * value2.M23 + value1.M33 * value2.M33 + value1.M34 * value2.M43;
            m.M34 = value1.M31 * value2.M14 + value1.M32 * value2.M24 + value1.M33 * value2.M34 + value1.M34 * value2.M44;

            // Fourth row
            m.M41 = value1.M41 * value2.M11 + value1.M42 * value2.M21 + value1.M43 * value2.M31 + value1.M44 * value2.M41;
            m.M42 = value1.M41 * value2.M12 + value1.M42 * value2.M22 + value1.M43 * value2.M32 + value1.M44 * value2.M42;
            m.M43 = value1.M41 * value2.M13 + value1.M42 * value2.M23 + value1.M43 * value2.M33 + value1.M44 * value2.M43;
            m.M44 = value1.M41 * value2.M14 + value1.M42 * value2.M24 + value1.M43 * value2.M34 + value1.M44 * value2.M44;

            return m;
        }
        
        [Kernel]
        public static void ExampleKernel([Global] Matrix4x4[] input, [Global] Matrix4x4[] output, [Const] uint count)
        {
            var id = CLFunctions.GetGlobalId(0);

            if (id >= count) return;

            var matrix = input[id];
            
            output[id] = Multiply(matrix, matrix);
        }

        private static void PrintAcceleratorDetails(Accelerator accelerator)
        {
            Console.WriteLine($"{nameof(accelerator.Name)} = {accelerator.Name}");
            Console.WriteLine($"{nameof(accelerator.Vendor)} = {accelerator.Vendor}");
            Console.WriteLine($"{nameof(accelerator.Version)} = {accelerator.Version}");
            Console.WriteLine($"{nameof(accelerator.DriverVersion)} = {accelerator.DriverVersion}");
            Console.WriteLine($"{nameof(accelerator.Available)} = {accelerator.Available}");
            Console.WriteLine($"{nameof(accelerator.CompilerAvailable)} = {accelerator.CompilerAvailable}");
            Console.WriteLine($"{nameof(accelerator.Memory)} = {accelerator.Memory / Math.Pow(10, 9)} GB");
            Console.WriteLine($"{nameof(accelerator.Units)} = {accelerator.Units}");
            Console.WriteLine($"{nameof(accelerator.ClockFrequency)} = {accelerator.ClockFrequency} Mz");
        }

        private static void Main()
        {
            var accelerator = Accelerator.FindAccelerator(AcceleratorType.Gpu);

            PrintAcceleratorDetails(accelerator);

            var context = accelerator.CreateContext();

            var method = typeof(Program).GetMethod(nameof(ExampleKernel), BindingFlags.Static | BindingFlags.Public);

            Console.WriteLine("Running...");

            var watch = new Stopwatch();
            watch.Start();

            var ilProgram = new ILProgram(Assembly.GetExecutingAssembly());

            var ilKernel = ilProgram.Compile(method);

            Console.WriteLine($"Compile kernel: {watch.ElapsedMilliseconds}ms");

            var source = ilProgram.CompleteSource(ilKernel);

            File.WriteAllText("kernel.cl", source); // Save source for debugging

            watch.Restart();

            var program = DeviceProgram.FromSource(context, source);

            program.Build();

            Console.WriteLine($"Build program: {watch.ElapsedMilliseconds}ms");

            watch.Restart();

            var kernel = program.BuildKernel(nameof(ExampleKernel));

            Console.WriteLine($"Build kernel: {watch.ElapsedMilliseconds}ms");

            watch.Stop();

            const int size = 1024 * 1000;
            const int rounds = 100;

            var random = new Random();

            var totalFail = 0L;
            var totalGpu = 0L;
            var totalCpu = 0L;
            
            for (var j = 0; j < rounds; j++)
            {
                var data = new Matrix4x4[size];

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

                using var input = new SharedCollection<Matrix4x4>(context, data);

                using var output = new SharedCollection<Matrix4x4>(context, size);

                using var sizeInput = new SharedValue<uint>(context, size);

                watch.Restart();

                kernel.Invoke(size, new KernelArgument
                {
                    Value = input.UPtr,
                    Size = 8
                }, new KernelArgument
                {
                    Value = output.UPtr,
                    Size = 8
                }, new KernelArgument
                {
                    Value = sizeInput.UPtr,
                    Size = 4
                });

                var gpu = watch.ElapsedMilliseconds;

                watch.Restart();

                for (var i = 0; i < size; i++)
                {
                    var value = data[i];

                    data[i] = value * value; // Simplified operation
                }

                var cpu = watch.ElapsedMilliseconds;

                watch.Stop();

                var results = output.ReadCollection();

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
                    $"[{j:000}/{rounds:000}] Kernel succeeded with {Percent(size, failed)} operations at {Percent(cpu, gpu, true)} IL speed!"
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