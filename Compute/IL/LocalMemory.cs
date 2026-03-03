using System;

namespace Compute.IL
{
    /// <summary>
    /// Provides a compile-time marker for allocating OpenCL __local (shared) memory
    /// within GPU kernel code. The returned array is declared as <c>__local T name[size]</c>
    /// in the generated OpenCL kernel and is shared among all work items in a work group.
    /// 
    /// Usage:
    /// <code>
    /// var tile = LocalMemory.Allocate&lt;float&gt;(256);
    /// tile[i] = value;               // standard array indexing works
    /// BuiltIn.Barrier(1);            // CLK_LOCAL_MEM_FENCE
    /// float x = tile[otherIndex];    // read from another work item's write
    /// </code>
    /// 
    /// Works in both [Kernel] static methods and lambda kernels via Parallel.Run.
    /// The size must be a compile-time constant (or constant expression).
    /// </summary>
    public static class LocalMemory
    {
        /// <summary>
        /// Declares a __local (shared) memory array of the specified size.
        /// This method is a compile-time marker — it is never actually executed on the CPU.
        /// The AST compiler recognizes this call and emits a <c>__local T name[size]</c> declaration.
        /// </summary>
        /// <typeparam name="T">The element type (must be an unmanaged type)</typeparam>
        /// <param name="size">The number of elements. Must be a compile-time constant.</param>
        /// <returns>An array handle used for indexing in kernel code</returns>
        public static T[] Allocate<T>(int size) where T : unmanaged
        {
            // This method is never called at runtime — it's a marker for the GPU compiler.
            // If somehow called on the CPU, return a regular array as a fallback.
            return new T[size];
        }

        /// <summary>
        /// Declares a 2D __local (shared) memory region backed by a flat array.
        /// The generated OpenCL declaration is equivalent to <c>__local T name[width * height]</c>.
        /// </summary>
        public static T[] Allocate<T>(int width, int height) where T : unmanaged
        {
            return new T[width * height];
        }

        /// <summary>
        /// Declares a 2D __local (shared) memory region and exposes 2D indexing semantics.
        /// </summary>
        public static LocalArray2D<T> Allocate2D<T>(int width, int height) where T : unmanaged
        {
            return new LocalArray2D<T>(new T[width * height], width, height);
        }

        /// <summary>
        /// Declares a 3D __local (shared) memory region and exposes 3D indexing semantics.
        /// </summary>
        public static LocalArray3D<T> Allocate3D<T>(int width, int height, int depth) where T : unmanaged
        {
            return new LocalArray3D<T>(new T[width * height * depth], width, height, depth);
        }
    }

    public struct LocalArray2D<T> where T : unmanaged
    {
        private readonly T[] _buffer;

        public int Width { get; }

        public int Height { get; }

        internal LocalArray2D(T[] buffer, int width, int height)
        {
            _buffer = buffer;
            Width = width;
            Height = height;
        }

        public T this[int y, int x]
        {
            get => _buffer[y * Width + x];
            set => _buffer[y * Width + x] = value;
        }
    }

    public struct LocalArray3D<T> where T : unmanaged
    {
        private readonly T[] _buffer;

        public int Width { get; }

        public int Height { get; }

        public int Depth { get; }

        internal LocalArray3D(T[] buffer, int width, int height, int depth)
        {
            _buffer = buffer;
            Width = width;
            Height = height;
            Depth = depth;
        }

        public T this[int z, int y, int x]
        {
            get => _buffer[(z * Height + y) * Width + x];
            set => _buffer[(z * Height + y) * Width + x] = value;
        }
    }
}
