namespace Compute.IL.AST.Lambda;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Compute.IL;
using Compute.IL.AST.CodeGeneration;
using Compute.Memory;

public class Parallel : IDisposable
{
    private readonly List<(Array originalArray, SharedMemoryStream stream)> _arrayMappings = [];
    private readonly List<(FieldInfo field, SharedMemoryStream stream, Type originalType)> _valueTypeWritebacks = [];
    
    // Buffer reuse indexed by field index to avoid aliasing issues
    private readonly Dictionary<int, SharedMemoryStream> _bufferCache = [];

    private bool _disposed = false;
    
    // Fields for compiled kernel state
    private readonly Context _context;
    private readonly KernelDelegate _compiledKernel;
    private readonly string _kernelName;
    private readonly FieldInfo[] _closureFields;
    private readonly Type _closureType;
    private readonly object _closureInstance;

    private Parallel(Context context, Action action)
    {
        var target = action.Target;

        if (target == null)
            throw new InvalidOperationException("The provided action must be a closure capturing variables.");

        _context = context;
        _closureType = target.GetType();
        _closureInstance = target;
        _closureFields = _closureType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        var program = new AstProgram(context, new OpenClCodeGenerator());

        var compiledKernel = program.CompileAction(action, out var code, out _kernelName);

        // Save the code to a file for inspection
        System.IO.File.WriteAllText($"kernel_test.cl", code);

        if (compiledKernel == null)
            throw new InvalidOperationException("Failed to compile the provided action to a kernel.");

        _compiledKernel = compiledKernel;
    }

    /// <summary>
    /// Compiles an action to a reusable kernel without executing it
    /// </summary>
    /// <param name="context">The compute context</param>
    /// <param name="action">The action to compile</param>
    /// <returns>A Parallel instance with compiled kernel ready for execution</returns>
    public static Parallel Prepare(Context context, Action action)
    {
        return new Parallel(context, action);
    }

    /// <summary>
    /// Executes the pre-compiled kernel with the specified number of workers
    /// </summary>
    /// <param name="workers">The number of workers to run</param>
    public void Run(WorkerDimensions workers)
    {
        var args = _closureFields.Select((f, index) =>
        {
            var value = f.GetValue(_closureInstance) ?? throw new InvalidOperationException($"Field '{f.Name}' in closure is null and cannot be cast to 'nuint'.");
            return ConvertToArg(_context, value, f, index);
        }).ToArray();

        _compiledKernel(workers, args);

        // Write back results from device to host arrays and primitives
        WriteBackResults();
    }

    /// <summary>
    /// Compiles and executes an action in one call (original behavior)
    /// </summary>
    /// <param name="context">The compute context</param>
    /// <param name="workers">The number of workers to run</param>
    /// <param name="action">The action to compile and execute</param>
    public static void Run(Context context, WorkerDimensions workers, Action action)
    {
        using var parallel = Prepare(context, action);
        parallel.Run(workers);
    }

    private unsafe nuint ConvertToArg(Context context, object value, FieldInfo field, int fieldIndex)
    {
        var type = value.GetType();

        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            if (elementType == null)
                throw new InvalidOperationException($"Array field '{value}' has no element type.");

            if (!IsUnmanagedType(elementType))
                throw new InvalidOperationException($"Array field '{value}' has element type '{elementType.FullName}' which is not an unmanaged type. Only unmanaged types (primitives and structs containing only unmanaged types) are supported.");

            var array = (Array)value;

            uint arraySize = (uint)(array.Length * Marshal.SizeOf(elementType));

            // Get or create reusable buffer for this field
            var arrayStream = GetOrCreateBuffer(context, fieldIndex, arraySize);

            // Reset stream position for writing
            arrayStream.Position = 0;

            // Write array data to device memory
            WriteArrayToDevice(arrayStream, array, elementType);

            _arrayMappings.Add((array, arrayStream));

            return arrayStream.UPtr;
        }

        // Handle [ValueOnDevice] structs (e.g. image handles) — pass the raw handle directly
        // These wrap a nuint handle that is already a cl_mem pointer or similar opaque type.
        if (type.IsValueType && HasValueOnDeviceField(type))
        {
            var handleField = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(f => f.GetCustomAttribute<ValueOnDeviceAttribute>() != null);

            if (handleField != null)
            {
                var handleValue = handleField.GetValue(value);
                if (handleValue is nuint ptr)
                    return ptr;
            }
        }

        // Handle primitive value types: pass the raw value bits directly in the nuint.
        // OpenCL's clSetKernelArg expects a pointer to the value in host memory for scalars,
        // NOT a cl_mem handle. Since KernelInvoker passes &arg (where arg is nuint), we pack
        // the scalar bits into the low bytes of the nuint on little-endian systems.
        if (type.IsPrimitive || type.IsEnum)
        {
            nuint raw = 0;
            unsafe
            {
                var valueSize = Marshal.SizeOf(type);
                var gcPin = GCHandle.Alloc(value, GCHandleType.Pinned);
                try
                {
                    Buffer.MemoryCopy(gcPin.AddrOfPinnedObject().ToPointer(), &raw, sizeof(nuint), valueSize);
                }
                finally
                {
                    gcPin.Free();
                }
            }

            // Track for potential writeback (value may be modified by kernel)
            // For primitives passed by value, we need a buffer to read back from
            if (field != null)
            {
                var primSize = (uint)Marshal.SizeOf(type);
                var wbStream = GetOrCreateBuffer(context, fieldIndex, primSize);
                wbStream.Position = 0;
                var pinHandle = GCHandle.Alloc(value, GCHandleType.Pinned);
                try
                {
                    wbStream.Write(pinHandle.AddrOfPinnedObject().ToPointer(), primSize);
                }
                finally
                {
                    pinHandle.Free();
                }
                _valueTypeWritebacks.Add((field, wbStream, type));
            }

            return raw;
        }

        // Handle all other value types (structs) — these still go through device memory
        var valueType = value.GetType();
        var size = (uint)Marshal.SizeOf(valueType);
        
        // Get or create reusable buffer for this field
        var stream = GetOrCreateBuffer(context, fieldIndex, size);
        
        // Reset stream position for writing
        stream.Position = 0;
        
        // Write the value directly to device memory using marshalling
        var handle = GCHandle.Alloc(value, GCHandleType.Pinned);
        try
        {
            var ptr = handle.AddrOfPinnedObject();
            stream.Write(ptr.ToPointer(), size);
        }
        finally
        {
            handle.Free();
        }

        // Track value type for potential writeback if we have field info
        if (field != null)
        {
            _valueTypeWritebacks.Add((field, stream, valueType));
        }

        return stream.UPtr;
    }

    private void WriteArrayToDevice(SharedMemoryStream stream, Array array, Type elementType)
    {
        stream.WriteArrayGeneric(array, elementType);
    }

    private SharedMemoryStream GetOrCreateBuffer(Context context, int fieldIndex, uint size)
    {
        if (_bufferCache.TryGetValue(fieldIndex, out var existingStream))
        {
            // Check if the existing buffer is large enough
            if (existingStream.Length >= size)
            {
                return existingStream;
            }
            
            // If buffer is too small, close it and create a new one
            existingStream.Close();
            _bufferCache.Remove(fieldIndex);
        }

        var stream = new SharedMemoryStream(context, size);
        _bufferCache[fieldIndex] = stream;
        return stream;
    }

    private unsafe object ReadValueTypeFromDevice(SharedMemoryStream stream, Type originalType)
    {
        // Reset stream position to beginning for reading
        stream.Position = 0;
        
        // Read the value from device memory using marshalling for all value types
        var size = Marshal.SizeOf(originalType);
        var buffer = new byte[size];
        var totalRead = 0;
        while (totalRead < size)
        {
            var bytesRead = stream.Read(buffer, totalRead, size - totalRead);
            if (bytesRead == 0)
                throw new InvalidOperationException("Unexpected end of stream while reading value type from device");
            totalRead += bytesRead;
        }
        
        // Pin the buffer and marshal back to the original type
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            var ptr = handle.AddrOfPinnedObject();
            return Marshal.PtrToStructure(ptr, originalType)!;
        }
        finally
        {
            handle.Free();
        }
    }

    private void WriteBackResults()
    {
        // Write back arrays
        foreach (var (originalArray, stream) in _arrayMappings)
        {
            var elementType = originalArray.GetType().GetElementType()!;
            stream.ReadArrayGeneric(originalArray, elementType);
        }

        // Write back value types
        foreach (var (field, stream, originalType) in _valueTypeWritebacks)
        {
            var value = ReadValueTypeFromDevice(stream, originalType);
            field.SetValue(_closureInstance, value);
        }

        _arrayMappings.Clear();
        _valueTypeWritebacks.Clear();
    }

    private static bool IsUnmanagedType(Type type)
    {
        // Check if the type can be used with the 'unmanaged' constraint
        // This includes all primitive types and structs that contain only unmanaged types

        if (type.IsPrimitive)
            return true;

        if (type.IsEnum)
            return IsUnmanagedType(Enum.GetUnderlyingType(type));

        if (!type.IsValueType)
            return false;

        // For structs, check all fields recursively
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (!IsUnmanagedType(field.FieldType))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks whether a type contains a field marked with [ValueOnDevice],
    /// indicating it wraps an opaque handle (e.g. cl_mem for image2d_t).
    /// </summary>
    private static bool HasValueOnDeviceField(Type type)
    {
        return type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Any(f => f.GetCustomAttribute<ValueOnDeviceAttribute>() != null);
    }

    ~Parallel()
    {
        Cleanup();
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    private void Cleanup()
    {
        if (_disposed)
            return;

        _disposed = true;

        _arrayMappings.Clear();
        _valueTypeWritebacks.Clear();
        
        // Clear buffer cache
        foreach (var stream in _bufferCache.Values)
        {
            stream.Close();
        }

        _bufferCache.Clear();
    }
}