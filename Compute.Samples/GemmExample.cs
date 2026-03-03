using System;
using System.Diagnostics;
using Compute;
using Compute.IL;
using Compute.IL.AST;
using Compute.IL.AST.CodeGeneration;
using Compute.IL.AST.Lambda;

namespace Compute.Samples;

/// <summary>
/// Demonstrates tiled GEMM (General Matrix Multiply) using __local (shared) memory.
/// Shows both lambda and standalone [Kernel] approaches.
/// 
/// Algorithm: C = A * B  where A is M×K, B is K×N, C is M×N
/// Each work group handles a TILE×TILE block of C, using shared memory tiles
/// to reduce global memory accesses from O(K) to O(K/TILE) per element.
/// </summary>
public static class GemmExample
{
    private const int TILE = 16;

    // ─────────────────────────────────────────────
    //  Standalone [Kernel] GEMM
    // ─────────────────────────────────────────────
    [Kernel]
    public static void GemmTiled(
        [Global] float[] A,
        [Global] float[] B,
        [Global] float[] C,
        int M,
        int N,
        int K)
    {
        var tileA = LocalMemory.Allocate2D<float>(TILE, TILE);
        var tileB = LocalMemory.Allocate2D<float>(TILE, TILE);

        int row = KernelThread.Local.Y;
        int col = KernelThread.Local.X;
        int globalRow = KernelThread.Group.Y * TILE + row;
        int globalCol = KernelThread.Group.X * TILE + col;

        float sum = 0.0f;

        int numTiles = K / TILE;
        for (int t = 0; t < numTiles; t++)
        {
            // Load one tile of A and B into local memory
            tileA[row, col] = A[globalRow * K + t * TILE + col];
            tileB[row, col] = B[(t * TILE + row) * N + globalCol];

            Sync.Local(); // CLK_LOCAL_MEM_FENCE

            // Compute partial dot product for this tile
            for (int k = 0; k < TILE; k++)
            {
                sum += tileA[row, k] * tileB[k, col];
            }

            Sync.Local(); // CLK_LOCAL_MEM_FENCE
        }

        C[globalRow * N + globalCol] = sum;
    }

    // ─────────────────────────────────────────────
    //  Runner
    // ─────────────────────────────────────────────
    public static void RunGemmExample(Accelerator accelerator)
    {
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine("  GEMM with Local Memory (Tiled)");
        Console.WriteLine("═══════════════════════════════════════════");

        const int M = 256;
        const int N = 256;
        const int K = 256;

        // Initialize matrices
        var A = new float[M * K];
        var B = new float[K * N];
        var C_gpu = new float[M * N];
        var C_cpu = new float[M * N];

        var rng = new Random(42);
        for (int i = 0; i < A.Length; i++) A[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
        for (int i = 0; i < B.Length; i++) B[i] = (float)(rng.NextDouble() * 2.0 - 1.0);

        // ── CPU reference ──
        var sw = Stopwatch.StartNew();
        CpuGemm(A, B, C_cpu, M, N, K);
        sw.Stop();
        Console.WriteLine($"CPU reference: {sw.ElapsedMilliseconds} ms");

        // ── Lambda GEMM ──
        using var context = accelerator.CreateContext();
        RunLambdaGemm(context, A, B, C_gpu, M, N, K);
        VerifyResults("Lambda GEMM", C_cpu, C_gpu, M, N);

        // ── Standalone [Kernel] GEMM ──
        Array.Clear(C_gpu);
        RunStandaloneGemm(context, A, B, C_gpu, M, N, K);
        VerifyResults("Standalone GEMM", C_cpu, C_gpu, M, N);

        Console.WriteLine("═══════════════════════════════════════════\n");
    }

    // ─────────────────────────────────────────────
    //  Lambda GEMM  (Parallel.Run)
    // ─────────────────────────────────────────────
    private static void RunLambdaGemm(Context context, float[] A, float[] B, float[] C, int M, int N, int K)
    {
        var sw = Stopwatch.StartNew();
        int tile = TILE;

        var workers = Grid.Size(N, M).Tile((uint)tile, (uint)tile);

        using var parallel = Parallel.Prepare(context, () =>
        {
            var tileA = LocalMemory.Allocate2D<float>(TILE, TILE);
            var tileB = LocalMemory.Allocate2D<float>(TILE, TILE);

            int row = KernelThread.Local.Y;
            int col = KernelThread.Local.X;
            int globalRow = KernelThread.Group.Y * tile + row;
            int globalCol = KernelThread.Group.X * tile + col;

            float sum = 0.0f;

            int numTiles = K / tile;
            for (int t = 0; t < numTiles; t++)
            {
                tileA[row, col] = A[globalRow * K + t * tile + col];
                tileB[row, col] = B[(t * tile + row) * N + globalCol];

                Sync.Local();

                for (int k = 0; k < tile; k++)
                {
                    sum += tileA[row, k] * tileB[k, col];
                }

                Sync.Local();
            }

            C[globalRow * N + globalCol] = sum;
        });

        parallel.Run(workers);
        sw.Stop();
        Console.WriteLine($"Lambda GEMM:     {sw.ElapsedMilliseconds} ms");
    }

    // ─────────────────────────────────────────────
    //  Standalone [Kernel] GEMM  (AstProgram.CompileDelegate)
    // ─────────────────────────────────────────────
    private static void RunStandaloneGemm(Context context, float[] A, float[] B, float[] C, int M, int N, int K)
    {
        var sw = Stopwatch.StartNew();

        var program = new AstProgram(context, new OpenClCodeGenerator());
        var kernel = program.CompileDelegate(
            typeof(GemmExample).GetMethod(nameof(GemmTiled))!,
            out var code);

        System.IO.File.WriteAllText("gemm_kernel.cl", code);
        Console.WriteLine($"Generated GEMM kernel → gemm_kernel.cl");

        if (kernel == null)
        {
            Console.WriteLine("ERROR: Standalone GEMM compilation failed");
            return;
        }

        // Set up shared collections
        using var sharedA = new Memory.SharedCollection<float>(context, A.Length);
        using var sharedB = new Memory.SharedCollection<float>(context, B.Length);
        using var sharedC = new Memory.SharedCollection<float>(context, C.Length);

        sharedA.CopyToDevice(A);
        sharedB.CopyToDevice(B);

        var workers = Grid.Size(N, M).Tile((uint)TILE, (uint)TILE);

        kernel(workers, sharedA, sharedB, sharedC, (nuint)M, (nuint)N, (nuint)K);

        sharedC.CopyToHostNonAlloc(C);

        sw.Stop();
        Console.WriteLine($"Standalone GEMM: {sw.ElapsedMilliseconds} ms");
    }

    // ─────────────────────────────────────────────
    //  CPU reference
    // ─────────────────────────────────────────────
    private static void CpuGemm(float[] A, float[] B, float[] C, int M, int N, int K)
    {
        for (int i = 0; i < M; i++)
        for (int j = 0; j < N; j++)
        {
            float sum = 0;
            for (int k = 0; k < K; k++)
                sum += A[i * K + k] * B[k * N + j];
            C[i * N + j] = sum;
        }
    }

    // ─────────────────────────────────────────────
    //  Verification
    // ─────────────────────────────────────────────
    private static void VerifyResults(string label, float[] expected, float[] actual, int M, int N)
    {
        int errors = 0;
        float maxDiff = 0;
        for (int i = 0; i < expected.Length; i++)
        {
            float diff = MathF.Abs(expected[i] - actual[i]);
            if (diff > maxDiff) maxDiff = diff;
            if (diff > 0.01f) errors++;
        }

        if (errors == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  {label}: PASS  (max diff: {maxDiff:E2})");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  {label}: FAIL  ({errors}/{expected.Length} elements differ, max diff: {maxDiff:E2})");
        }
        Console.ResetColor();
    }
}
