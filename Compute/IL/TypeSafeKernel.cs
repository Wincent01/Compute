using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using Compute.Memory;

namespace Compute.IL
{
    /// <summary>
    /// A type-safe wrapper around KernelDelegate that automatically manages SharedCollections
    /// </summary>
    /// <typeparam name="TDelegate">The delegate type matching the kernel method signature</typeparam>
    public class TypeSafeKernel<TDelegate> : IDisposable where TDelegate : Delegate
    {
        private readonly KernelDelegate _kernelDelegate;
        private readonly MethodInfo _method;
        private readonly ILProgram _program;
        private readonly ConcurrentBag<IDisposable> _managedResources;

        internal TypeSafeKernel(KernelDelegate kernelDelegate, MethodInfo method, ILProgram program)
        {
            _kernelDelegate = kernelDelegate;
            _method = method;
            _program = program;
            _managedResources = new ConcurrentBag<IDisposable>();
        }

        /// <summary>
        /// Invoke the kernel with automatic array-to-SharedCollection conversion and result retrieval
        /// </summary>
        /// <param name="workers">Worker dimensions</param>
        /// <param name="args">Arguments matching the kernel method signature</param>
        public void Invoke(WorkerDimensions workers, params object[] args)
        {
            var parameters = _method.GetParameters();
            var ptrs = new UIntPtr[args.Length];
            var sharedCollections = new IDisposable[args.Length];
            var outputArrays = new (int index, Array array, IDisposable sharedCollection)[args.Length];
            var outputCount = 0;

            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    var arg = args[i];

                    if (paramType.IsArray)
                    {
                        // Convert array to SharedCollection automatically
                        var elementType = paramType.GetElementType();
                        var sharedCollection = CreateSharedCollection(elementType, arg);
                        sharedCollections[i] = sharedCollection;
                        
                        // Extract UPtr using reflection
                        var uptrProperty = sharedCollection.GetType().GetProperty("UPtr");
                        ptrs[i] = (UIntPtr)uptrProperty.GetValue(sharedCollection);

                        // Check if this is an output parameter (not marked as [Const])
                        var isConst = parameters[i].GetCustomAttribute<ConstAttribute>() != null;
                        if (!isConst && arg is Array array)
                        {
                            outputArrays[outputCount++] = (i, array, sharedCollection);
                        }
                    }
                    else if (IsValueType(paramType))
                    {
                        // Handle value types (uint, float, etc.) by converting to UIntPtr
                        ptrs[i] = ConvertValueToUIntPtr(arg, paramType);
                    }
                    else
                    {
                        throw new ArgumentException($"Unsupported parameter type: {paramType}");
                    }
                }

                // Execute the kernel
                _kernelDelegate(workers, ptrs);

                // Copy results back to output arrays
                for (int i = 0; i < outputCount; i++)
                {
                    var (index, array, sharedCollection) = outputArrays[i];
                    CopyResultsBack(array, sharedCollection);
                }
            }
            finally
            {
                // Clean up SharedCollections
                for (int i = 0; i < sharedCollections.Length; i++)
                {
                    sharedCollections[i]?.Dispose();
                }
            }
        }

        private IDisposable CreateSharedCollection(Type elementType, object array)
        {
            // Use reflection to create SharedCollection<T> from array
            var sharedCollectionType = typeof(SharedCollection<>).MakeGenericType(elementType);
            
            // Call the constructor that takes (Context, Span<T>, bool)
            // First convert Array to Span<T> using unsafe cast approach
            var createMethod = typeof(TypeSafeKernel<TDelegate>).GetMethod(nameof(CreateSharedCollectionGeneric), 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var genericMethod = createMethod.MakeGenericMethod(elementType);
            
            return (IDisposable)genericMethod.Invoke(this, new[] { array });
        }

        private SharedCollection<T> CreateSharedCollectionGeneric<T>(T[] array) where T : unmanaged
        {
            return new SharedCollection<T>(_program.Context, new Span<T>(array), true);
        }

        private void CopyResultsBack(Array originalArray, IDisposable sharedCollection)
        {
            // Use reflection to call the generic copy method
            var elementType = originalArray.GetType().GetElementType();
            var copyMethod = typeof(TypeSafeKernel<TDelegate>).GetMethod(nameof(CopyResultsBackGeneric), 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var genericMethod = copyMethod.MakeGenericMethod(elementType);
            
            genericMethod.Invoke(this, new object[] { originalArray, sharedCollection });
        }

        private void CopyResultsBackGeneric<T>(T[] originalArray, SharedCollection<T> sharedCollection) where T : unmanaged
        {
            // Read results from SharedCollection back to original array
            var results = sharedCollection.CopyToHost();
            results.CopyTo(originalArray);
        }

        private static UIntPtr ConvertValueToUIntPtr(object value, Type type)
        {
            // Convert value types to UIntPtr for kernel arguments
            return type.Name switch
            {
                nameof(UInt32) => (UIntPtr)(uint)value,
                nameof(Int32) => (UIntPtr)(uint)(int)value,
                nameof(UInt64) => (UIntPtr)(ulong)value,
                nameof(Int64) => (UIntPtr)(ulong)(long)value,
                nameof(Single) => (UIntPtr)BitConverter.ToUInt32(BitConverter.GetBytes((float)value)),
                nameof(Double) => (UIntPtr)BitConverter.ToUInt64(BitConverter.GetBytes((double)value)),
                _ => throw new ArgumentException($"Unsupported value type: {type}")
            };
        }

        private static bool IsValueType(Type type)
        {
            return type.IsPrimitive || type.IsValueType;
        }

        public void Dispose()
        {
            // Dispose all managed SharedCollections
            while (_managedResources.TryTake(out var resource))
            {
                resource?.Dispose();
            }
        }
    }
}
