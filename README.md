# Compute

Compute is an experimental OpenCL acceleration library for C#.

It provides:
- OpenCL platform and device discovery.
- OpenCL Context and memory abstractions.
- Kernel execution helpers.
- C# to OpenCL kernel generation through IL/AST compilation.

## Project Status

This project is experimental and under active iteration.

- API shape may still change.
- Not all IL patterns are supported yet.
- Some valid C# methods may fail to compile as kernels.

## Requirements

- Latest .NET SDK.
- A working OpenCL runtime/driver (NVIDIA, AMD, Intel, etc).

## Quick Start

Build samples:

```bash
dotnet build Compute.Samples/Compute.Samples.csproj
```

Run the sample suite:

```bash
dotnet run --project Compute.Samples/Compute.Samples.csproj
```

The suite currently runs representative examples for:
- GEMM with local memory tiling
- 1D/2D/3D grid execution
- Type-safe kernel invocation
- N-body simulation
- Atomics
- Images
- Reductions

Each sample reports correctness and timing (`CPU`, `GPU`, and speedup when available).

## Basic Kernel Examples

### 1) Standalone `[Kernel]` method

```csharp
[Kernel]
public static void Saxpy([Global] float[] x, [Global] float[] y, [Const] uint count)
{
	var id = BuiltIn.GetGlobalId(0);
	if (id >= count) return;

	y[id] = 2.0f * x[id] + y[id];
}
```

### 2) Lambda kernel with `Parallel`

```csharp
using var context = accelerator.CreateContext();

using var kernel = Parallel.Prepare(context, () =>
{
	var id = KernelThread.Global.X;
	if (id >= length) return;

	output[id] = input[id] * input[id];
});

kernel.Run(Grid.Size((uint)length));
```

### 3) Local memory + synchronization

```csharp
var tile = LocalMemory.Allocate2D<float>(16, 16);
tile[KernelThread.Local.Y, KernelThread.Local.X] = value;
Sync.Local();
```

### 4) GEMM with tiling

```csharp
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
```

## Notes on Images and Atomics

- Image kernels and atomic operations are supported in the sample suite.
- Image readback precision/format behavior may vary by driver/runtime.
- For realistic validation, always test on your target GPU vendor stack.

## Where to Look in the Repo

- Core library: `Compute/`
- Sample suite: `Compute.Samples/`
- Entry point for samples: `Compute.Samples/Program.cs`

## Contributing

Contributions are welcome.
