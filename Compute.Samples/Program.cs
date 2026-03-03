using System;
using System.Collections;
using System.Collections.Generic;
using Compute;

namespace Compute.Samples;

internal static class Program
{
    private delegate SampleResult SampleRunner(Accelerator accelerator);

    private static readonly (string Name, SampleRunner Run)[] SampleSuite =
    {
        ("GEMM (Local Memory)", GemmExample.RunGemmExample),
        ("Worker Dimensions", MultiDimensionalExample.RunMultiDimensionalExamples),
        ("Type-Safe Wrapper", TypeSafeKernelExample.RunTypeSafeExamples),
        ("N-Body", NBodySimulation.RunNBodyExample),
        ("Atomics", AtomicExample.RunAtomicExample),
        ("Images", ImageExample.RunImageExample),
        ("Reductions", ReductionExample.RunReductionExample)
    };

    private static int Main()
    {
        var overallPassed = true;

        foreach (var platform in Platform.Platforms)
        {
            PrintDetails(platform);
            Console.WriteLine();

            foreach (var accelerator in platform.Accelerators)
            {
                PrintDetails(accelerator);
                Console.WriteLine();
                Console.WriteLine($"Running sample suite on {accelerator.Name}...");

                var results = new List<SampleResult>(SampleSuite.Length);
                foreach (var (_, run) in SampleSuite)
                {
                    var result = run(accelerator);
                    results.Add(result);
                }

                var suitePass = PrintSummary(results);
                overallPassed = overallPassed && suitePass;

                Console.WriteLine(new string('═', 72));
            }

            Console.WriteLine($"Done with: {platform.Name}");
            Console.WriteLine();
        }

        return overallPassed ? 0 : 1;
    }

    private static bool PrintSummary(IReadOnlyList<SampleResult> results)
    {
        Console.WriteLine("\nSample Summary");
        Console.WriteLine(new string('-', 72));

        var passedCount = 0;

        foreach (var result in results)
        {
            if (result.Passed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                passedCount++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            var correctness = result.Passed ? "PASS" : "FAIL";
            Console.Write($"[{correctness}] ");
            Console.ResetColor();

            var cpu = result.CpuMilliseconds.HasValue ? $"CPU={result.CpuMilliseconds}ms" : "CPU=n/a";
            var gpu = result.GpuMilliseconds.HasValue ? $"GPU={result.GpuMilliseconds}ms" : "GPU=n/a";
            var speedup = result.Speedup.HasValue ? $"Speedup={result.Speedup.Value:F2}x" : "Speedup=n/a";

            Console.WriteLine($"{result.Name}: {cpu}, {gpu}, {speedup}");

            if (!string.IsNullOrWhiteSpace(result.Details))
            {
                Console.WriteLine($"         {result.Details}");
            }
        }

        Console.WriteLine(new string('-', 72));
        Console.WriteLine($"Suite result: {passedCount}/{results.Count} examples passed");

        return passedCount == results.Count;
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

            if (value is IEnumerable enumerable && value is not string)
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
}
