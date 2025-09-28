namespace Compute.IL.AST.Lambda;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Compute.IL.AST.CodeGeneration;
using Compute.Memory;

public class Parallel : IDisposable
{
    private List<SharedMemoryStream> _sharedCollections = new();
    private List<(Array originalArray, SharedMemoryStream stream)> _arrayMappings = new();

    public Parallel(Context context, uint workers, Action action)
    {
        var target = action.Target;

        if (target == null)
            throw new InvalidOperationException("The provided action must be a closure capturing variables.");

        var closureType = target.GetType();

        var fields = closureType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

        var program = new AstProgram(context.Accelerator, new OpenClCodeGenerator());

        var del = program.CompileAction(action, out var code, out var kernelName);

        // Save the code to a file for inspection
        System.IO.File.WriteAllText($"kernel_test.cl", code);

        if (del == null)
            throw new InvalidOperationException("Failed to compile the provided action to a kernel.");

        var args = fields.Select(f =>
        {
            var value = f.GetValue(target);
            if (value == null)
                throw new InvalidOperationException($"Field '{f.Name}' in closure is null and cannot be cast to 'nuint'.");

            return ConvertToArg(context, value);
        }).ToArray();

        del(workers, args);

        // Write back results from device to host arrays
        WriteBackResults();
    }

    private unsafe nuint ConvertToArg(Context context, object value)
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

            uint size = (uint)(array.Length * Marshal.SizeOf(elementType));

            var stream = new SharedMemoryStream(context, size);

            // Write array data to device memory
            WriteArrayToDevice(stream, array, elementType);

            _sharedCollections.Add(stream);
            _arrayMappings.Add((array, stream));

            return stream.UPtr;
        }

        return value switch
        {
            bool b => b ? 1u : 0u,
            byte b => b,
            sbyte sb => *(byte*)&sb,
            short s => *(ushort*)&s,
            ushort us => us,
            int i => *(uint*)&i,
            uint ui => ui,
            long l => sizeof(nuint) == 8 ? (nuint)(*(ulong*)&l) : (nuint)(*(uint*)&l),
            ulong ul => sizeof(nuint) == 8 ? (nuint)ul : (nuint)ul,
            float f => *(uint*)&f,
            double d => sizeof(nuint) == 8 ? (nuint)(*(ulong*)&d) : (nuint)(*(uint*)&d),
            char c => c,
            nuint n => n,
            nint ni => (nuint)ni,
            _ => throw new ArgumentException($"Unsupported type for bit-preserving conversion: {value.GetType()}")
        };
    }

    private void WriteArrayToDevice(SharedMemoryStream stream, Array array, Type elementType)
    {
        stream.WriteArrayGeneric(array, elementType);
    }

    private void WriteBackResults()
    {
        foreach (var (originalArray, stream) in _arrayMappings)
        {
            var elementType = originalArray.GetType().GetElementType()!;
            stream.ReadArrayGeneric(originalArray, elementType);
        }
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
        var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (!IsUnmanagedType(field.FieldType))
                return false;
        }

        return true;
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
        foreach (var stream in _sharedCollections)
        {
            stream.Dispose();
        }
        _sharedCollections.Clear();
        _arrayMappings.Clear();
    }
}