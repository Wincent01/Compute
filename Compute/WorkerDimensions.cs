using System;

namespace Compute
{
    /// <summary>
    /// Represents the dimensions for kernel worker configuration in 1D, 2D, or 3D space.
    /// </summary>
    public readonly struct WorkerDimensions
    {
        public uint X { get; init; }
        public uint Y { get; init; }
        public uint Z { get; init; }

        public uint LocalX { get; init; }
        public uint LocalY { get; init; }
        public uint LocalZ { get; init; }

        /// <summary>
        /// Gets the number of dimensions (1, 2, or 3) based on which values are non-zero.
        /// </summary>
        public readonly uint DimensionCount
        {
            get
            {
                if (Z > 0) return 3;
                if (Y > 0) return 2;
                return 1;
            }
        }

        public readonly uint LocalDimensionCount
        {
            get
            {
                if (LocalZ > 0) return 3;
                if (LocalY > 0) return 2;
                return 1;
            }
        }

        public readonly bool HasLocalSize => LocalX > 0 || LocalY > 0 || LocalZ > 0;

        /// <summary>
        /// Creates a 1D worker dimension.
        /// </summary>
        /// <param name="x">Number of workers in X dimension</param>
        public WorkerDimensions(uint x)
        {
            X = x;
            Y = 0;
            Z = 0;
        }

        /// <summary>
        /// Creates a 2D worker dimension.
        /// </summary>
        /// <param name="x">Number of workers in X dimension</param>
        /// <param name="y">Number of workers in Y dimension</param>
        public WorkerDimensions(uint x, uint y)
        {
            X = x;
            Y = y;
            Z = 0;
        }

        /// <summary>
        /// Creates a 3D worker dimension.
        /// </summary>
        /// <param name="x">Number of workers in X dimension</param>
        /// <param name="y">Number of workers in Y dimension</param>
        /// <param name="z">Number of workers in Z dimension</param>
        public WorkerDimensions(uint x, uint y, uint z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Gets the dimensions as an array of UIntPtr for OpenCL calls.
        /// </summary>
        /// <returns>Array containing the non-zero dimensions</returns>
        public UIntPtr[] ToArray()
        {
            return DimensionCount switch
            {
                1 => [X],
                2 => [X, Y],
                3 => [X, Y, Z],
                _ => throw new InvalidOperationException("At least X dimension must be non-zero."),
            };
        }

        /// <summary>
        /// Gets the local dimensions as an array of UIntPtr for OpenCL calls.
        /// </summary> <returns>Array containing the non-zero local dimensions</returns>
        public UIntPtr[] LocalSizeToArray()
        {
            return LocalDimensionCount switch
            {
                1 => [LocalX],
                2 => [LocalX, LocalY],
                3 => [LocalX, LocalY, LocalZ],
                _ => throw new InvalidOperationException("At least LocalX dimension must be non-zero."),
            };
        }

        /// <summary>
        /// Implicit conversion from uint to 1D WorkerDimensions for backward compatibility.
        /// </summary>
        /// <param name="workers">Number of workers</param>
        public static implicit operator WorkerDimensions(uint workers)
        {
            return new WorkerDimensions(workers);
        }

        /// <summary>
        /// Implicit conversion from uint to 1D WorkerDimensions for backward compatibility.
        /// </summary>
        /// <param name="workers">Number of workers</param>
        public static implicit operator WorkerDimensions(int workers)
        {
            return new WorkerDimensions((uint)workers);
        }

        /// <summary>
        /// Implicit conversion from uint to 1D WorkerDimensions for backward compatibility.
        /// </summary>
        /// <param name="workers">Number of workers</param>
        public static implicit operator WorkerDimensions(uint[] workers)
        {
            return workers.Length switch
            {
                1 => new WorkerDimensions(workers[0]),
                2 => new WorkerDimensions(workers[0], workers[1]),
                3 => new WorkerDimensions(workers[0], workers[1], workers[2]),
                _ => throw new ArgumentException("Invalid workers array length.")
            };
        }

        /// <summary>
        /// Implicit conversion from uint to 1D WorkerDimensions for backward compatibility.
        /// </summary>
        /// <param name="workers">Number of workers</param>
        public static implicit operator WorkerDimensions(int[] workers)
        {
            return workers.Length switch
            {
                1 => new WorkerDimensions((uint)workers[0]),
                2 => new WorkerDimensions((uint)workers[0], (uint)workers[1]),
                3 => new WorkerDimensions((uint)workers[0], (uint)workers[1], (uint)workers[2]),
                _ => throw new ArgumentException("Invalid workers array length.")
            };
        }

        /// <summary>
        /// Gets the total number of workers across all dimensions.
        /// </summary>
        public uint TotalWorkers
        {
            get
            {
                var total = X;
                if (Y > 0) total *= Y;
                if (Z > 0) total *= Z;
                return total;
            }
        }

        public override string ToString()
        {
            return DimensionCount switch
            {
                1 => $"({X})",
                2 => $"({X}, {Y})",
                3 => $"({X}, {Y}, {Z})",
                _ => "(0)",
            };
        }
    }
}
