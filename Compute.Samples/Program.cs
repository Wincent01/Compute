using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Compute.ILKernel;
using Compute.Memory;

namespace Compute.Samples
{
    internal static class Program
    {
        public static float HelperMethod()
        {
            var i = 0;

            for (var j = 0; j < 6; j++)
            {
                i += 1;
            }

            return i;
        }
        
        public static float AntherHelper()
        {
            return 5 + HelperMethod();
        }
        
        public static void ExampleKernel([Global] float[] input, [Global] float[] output, [Const] uint count)
        {
            var id = ILKernel.ILKernel.GetGlobalId(0);

            if (id >= count) return;
            
            output[id] = input[id] * input[id];

            for (var i = 0; i < 4; i++)
            {
                output[id] += 1;
            }

            output[id] /= 2;

            output[id] += AntherHelper() + HelperMethod();

            if (float.IsNaN(output[id]))
            {
                output[id] += 1;
            }
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
            ILKernel.ILKernel.Assemblies.Add(Assembly.GetExecutingAssembly());
            
            var accelerator = Accelerator.FindAccelerator(AcceleratorType.Gpu);

            PrintAcceleratorDetails(accelerator);

            var context = accelerator.CreateContext();

            var method = typeof(Program).GetMethod(nameof(ExampleKernel), BindingFlags.Static | BindingFlags.Public);

            Console.WriteLine("Running...");
            
            var watch = new Stopwatch();
            watch.Start();

            var ilKernel = ILKernel.ILKernel.Compile(method);
            
            Console.WriteLine($"Compile kernel: {watch.ElapsedMilliseconds}ms");

            File.WriteAllText("kernel.cl", ilKernel.Source); // Save source for debugging
            
            watch.Restart();
            
            var program = DeviceProgram.FromSource(context, ilKernel.Source);
            
            program.Build();
            
            Console.WriteLine($"Build program: {watch.ElapsedMilliseconds}ms");
            
            watch.Restart();

            var kernel = program.BuildKernel(nameof(ExampleKernel));
            
            Console.WriteLine($"Build kernel: {watch.ElapsedMilliseconds}ms");
            
            watch.Stop();
            
            const int size = 1024 * 10000;

            var random = new Random();
            
            var data = new float[size];

            for (var i = 0; i < size; i++)
            {
                var value = (float) random.Next(1, 1000);

                data[i] = value;
            }
            
            watch.Start();

            var input = new SharedCollection<float>(context, data);

            var output = new SharedCollection<float>(context, size);

            var sizeInput = new SharedValue<uint>(context, size);
            
            Console.WriteLine($"Write device memory: {watch.ElapsedMilliseconds}ms");
            
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
            
            Console.WriteLine($"Gpu operations: {watch.ElapsedMilliseconds}ms");
            
            watch.Restart();

            for (var i = 0; i < size; i++)
            {
                var value = data[i];

                data[i] = (value * value + 4) / 2 + 5 + 12; // Simplified operation
            }
            
            Console.WriteLine($"Cpu operations: {watch.ElapsedMilliseconds}ms");
            
            watch.Restart();

            var results = output.ReadCollection().ToArray();
            
            Console.WriteLine($"Read device memory: {watch.ElapsedMilliseconds}ms");
            
            watch.Stop();

            for (var i = 0; i < size; i++)
            {
                if (!results[i].Equals(data[i]))
                {
                    throw new Exception("Kernel returned false results!");
                }
            }

            Console.WriteLine("Kernel return true results!");

            accelerator.Dispose();
        }
    }
}