using System;
using System.Diagnostics;
using Compute.IL;
using Compute.IL.AST.Lambda;

namespace Compute.Samples;

internal static class AtomicExample
{
    public static SampleResult RunAtomicExample(Accelerator accelerator)
    {
        Console.WriteLine($"\n=== Atomic Operations on {accelerator.Name} ===");

        using var context = accelerator.CreateContext();

        const int count = 1 << 20;
        var previousValues = new int[count];
        var counter = new int[1];

        var gpuWatch = Stopwatch.StartNew();
        using (var parallel = Parallel.Prepare(context, () =>
               {
                   var id = KernelThread.Global.X;
                   if (id >= count) return;

                   previousValues[id] = Atomic.Add(ref counter[0], 1);
               }))
        {
            parallel.Run(Grid.Size((uint)count));
        }
        gpuWatch.Stop();

        var expectedFinal = count;
        var expectedSum = ((long)count * (count - 1)) / 2;
        var expectedXor = XorRange(count - 1);

        long actualSum = 0;
        var actualXor = 0;
        var min = int.MaxValue;
        var max = int.MinValue;

        for (var i = 0; i < previousValues.Length; i++)
        {
            var value = previousValues[i];
            actualSum += value;
            actualXor ^= value;
            if (value < min) min = value;
            if (value > max) max = value;
        }

        var passed = counter[0] == expectedFinal
                     && actualSum == expectedSum
                     && actualXor == expectedXor
                     && min == 0
                     && max == count - 1;

        var cpuWatch = Stopwatch.StartNew();
        var cpuCounter = 0;
        var cpuPrevious = new int[count];
        for (var i = 0; i < count; i++)
        {
            cpuPrevious[i] = cpuCounter;
            cpuCounter += 1;
        }
        cpuWatch.Stop();

        Console.WriteLine($"GPU atomic increments: {counter[0]} in {gpuWatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"CPU reference increments: {cpuCounter} in {cpuWatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Correctness: {(passed ? "PASS" : "FAIL")}");

        return new SampleResult
        {
            Name = "Atomics",
            Passed = passed,
            CpuMilliseconds = cpuWatch.ElapsedMilliseconds,
            GpuMilliseconds = gpuWatch.ElapsedMilliseconds,
            Details = $"count={count}, final={counter[0]}, min={min}, max={max}"
        };
    }

    private static int XorRange(int n)
    {
        return n & 3 switch
        {
            0 => n,
            1 => 1,
            2 => n + 1,
            _ => 0
        };
    }
}