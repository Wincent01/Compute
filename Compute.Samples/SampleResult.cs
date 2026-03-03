using System;

namespace Compute.Samples;

public sealed class SampleResult
{
    public required string Name { get; init; }
    public required bool Passed { get; init; }
    public long? CpuMilliseconds { get; init; }
    public long? GpuMilliseconds { get; init; }
    public string Details { get; init; } = string.Empty;

    public double? Speedup
    {
        get
        {
            if (!CpuMilliseconds.HasValue || !GpuMilliseconds.HasValue || CpuMilliseconds.Value <= 0 || GpuMilliseconds.Value <= 0)
            {
                return null;
            }

            return (double)CpuMilliseconds.Value / GpuMilliseconds.Value;
        }
    }
}