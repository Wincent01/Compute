using System;
using System.Diagnostics;
using Compute.IL;
using Compute.IL.AST.Lambda;

namespace Compute.Samples;

internal static class ReductionExample
{
    public static SampleResult RunReductionExample(Accelerator accelerator)
    {
        Console.WriteLine($"\n=== Reduction on {accelerator.Name} ===");

        using var context = accelerator.CreateContext();

        const int count = 1 << 20;
        var values = new int[count];
        var gpuSum = new int[1];

        var random = new Random(123);
        for (var i = 0; i < count; i++)
        {
            values[i] = random.Next(0, 4);
        }

        var gpuWatch = Stopwatch.StartNew();
        using (var parallel = Parallel.Prepare(context, () =>
               {
                   var id = KernelThread.Global.X;
                   if (id >= count) return;

                   Atomic.Add(ref gpuSum[0], values[id]);
               }))
        {
            parallel.Run(Grid.Size((uint)count));
        }
        gpuWatch.Stop();

        var cpuWatch = Stopwatch.StartNew();
        var cpuSum = 0;
        for (var i = 0; i < count; i++)
        {
            cpuSum += values[i];
        }
        cpuWatch.Stop();

        var passed = gpuSum[0] == cpuSum;

        Console.WriteLine($"GPU reduction sum: {gpuSum[0]} in {gpuWatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"CPU reduction sum: {cpuSum} in {cpuWatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Correctness: {(passed ? "PASS" : "FAIL")}");

        return new SampleResult
        {
            Name = "Reductions",
            Passed = passed,
            CpuMilliseconds = cpuWatch.ElapsedMilliseconds,
            GpuMilliseconds = gpuWatch.ElapsedMilliseconds,
            Details = $"count={count}, sum={gpuSum[0]}"
        };
    }
}