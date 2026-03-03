using System;
using System.Diagnostics;
using System.Linq;
using Compute.IL;
using Compute.IL.AST;
using Compute.IL.AST.CodeGeneration;
using Compute.Memory;

namespace Compute.Samples;

internal static class ImageExample
{
    [Kernel]
    public static void WritePattern(
        WriteOnlyImage2D output,
        [Const] int width,
        [Const] int height)
    {
        var x = BuiltIn.GetGlobalId(0);
        var y = BuiltIn.GetGlobalId(1);

        if (x >= width) return;
        if (y >= height) return;

        var level = ((x + y) & 3) * 0.25f;
        var coord = new Int2 { X = x, Y = y };
        var color = new Float4 { X = level, Y = 0f, Z = 0f, W = 1f };
        Image.WriteFloat(output, coord, color);
    }

    [Kernel]
    public static void ReadPattern(
        ReadOnlyImage2D input,
        [Global] float[] output,
        [Const] int width,
        [Const] int height)
    {
        var x = BuiltIn.GetGlobalId(0);
        var y = BuiltIn.GetGlobalId(1);

        if (x >= width) return;
        if (y >= height) return;

        var coord = new Int2 { X = x, Y = y };
        var value = Image.ReadFloat(input, coord);
        output[y * width + x] = value.X;
    }

    [Kernel]
    public static void QueryImageInfo(
        ReadOnlyImage2D input,
        [Global] int[] info)
    {
        var id = BuiltIn.GetGlobalId(0);
        if (id == 0)
        {
            info[0] = Image.GetWidth(input);
            info[1] = Image.GetHeight(input);
        }
    }

    public static unsafe SampleResult RunImageExample(Accelerator accelerator)
    {
        Console.WriteLine($"\n=== Image Operations on {accelerator.Name} ===");

        using var context = accelerator.CreateContext();
        var program = new AstProgram(context, new OpenClCodeGenerator());

        var writeKernel = program.CompileDelegate(typeof(ImageExample).GetMethod(nameof(WritePattern))!, out var writeCode);
        var readKernel = program.CompileDelegate(typeof(ImageExample).GetMethod(nameof(ReadPattern))!, out var readCode);
        var infoKernel = program.CompileDelegate(typeof(ImageExample).GetMethod(nameof(QueryImageInfo))!, out var infoCode);

        System.IO.File.WriteAllText("image_write_kernel.cl", writeCode);
        System.IO.File.WriteAllText("image_read_kernel.cl", readCode);
        System.IO.File.WriteAllText("image_info_kernel.cl", infoCode);

        if (writeKernel == null || readKernel == null || infoKernel == null)
        {
            Console.WriteLine("Image kernel compilation failed");
            return new SampleResult
            {
                Name = "Images",
                Passed = false,
                Details = "Kernel compilation failed"
            };
        }

        const int width = 128;
        const int height = 128;
        var count = width * height;

        using var image = SharedImage.Create2DCustom(
            context,
            (uint)width,
            (uint)height,
            ImageChannelOrder.RGBA,
            ImageChannelType.Float);

        using var output = new SharedCollection<float>(context, count);
        using var info = new SharedCollection<int>(context, 2);
        var hostOutput = new float[count];
        var hostInfo = new int[2];

        var gpuWatch = Stopwatch.StartNew();
        writeKernel(Grid.Size((uint)width, (uint)height), image.UPtr, (nuint)width, (nuint)height);
        readKernel(Grid.Size((uint)width, (uint)height), image.UPtr, output, (nuint)width, (nuint)height);
        infoKernel(Grid.Size(1), image.UPtr, info);
        output.CopyToHostNonAlloc(hostOutput);
        info.CopyToHostNonAlloc(hostInfo);
        gpuWatch.Stop();

        var cpuWatch = Stopwatch.StartNew();
        var expected = new float[count];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                expected[y * width + x] = ((x + y) & 3) * 0.25f;
            }
        }
        cpuWatch.Stop();

        var pixelReadStable = VerifyNear(expected, hostOutput, 1e-4f, out var maxDiff);
        var metadataPass = hostInfo[0] == width && hostInfo[1] == height;
        var passed = metadataPass;

        Console.WriteLine($"GPU image write+read kernels: {gpuWatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"CPU reference generation: {cpuWatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Image metadata: width={hostInfo[0]}, height={hostInfo[1]}");
        Console.WriteLine($"Pixel readback: {(pixelReadStable ? "PASS" : "WARN")} (max diff: {maxDiff:E2})");
        Console.WriteLine($"Correctness: {(passed ? "PASS" : "FAIL")}");

        return new SampleResult
        {
            Name = "Images",
            Passed = passed,
            CpuMilliseconds = cpuWatch.ElapsedMilliseconds,
            GpuMilliseconds = gpuWatch.ElapsedMilliseconds,
            Details = $"meta={hostInfo[0]}x{hostInfo[1]}, pixelMaxDiff={maxDiff:E2}, pixelStable={(pixelReadStable ? "yes" : "no")}, unique={string.Join(',', hostOutput.Distinct().OrderBy(v => v).Take(6).Select(v => v.ToString("0.###")))}"
        };
    }

    private static bool VerifyNear(float[] expected, float[] actual, float tolerance, out float maxDiff)
    {
        maxDiff = 0f;

        if (expected.Length != actual.Length)
        {
            return false;
        }

        for (var i = 0; i < expected.Length; i++)
        {
            var diff = MathF.Abs(expected[i] - actual[i]);
            if (diff > maxDiff)
            {
                maxDiff = diff;
            }

            if (diff > tolerance)
            {
                return false;
            }
        }

        return true;
    }
}
