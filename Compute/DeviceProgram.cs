using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Compute.IL.Compiler;
using Silk.NET.OpenCL;

namespace Compute
{
    public class DeviceProgram : IDisposable
    {
        internal List<Kernel> Kernels { get; }
        
        public Context Context { get; }
        
        public IntPtr Handle { get; }
        
        public DeviceProgram(Context context, IntPtr handle)
        {
            Kernels = new List<Kernel>();
            
            Context = context;
            
            Handle = handle;

            Context.DevicePrograms.Add(this);
        }

        public static unsafe DeviceProgram FromSource(Context context, string source)
        {
            var sourceBytes = System.Text.Encoding.UTF8.GetBytes(source + "\0");
            var lengths = new UIntPtr[] { (UIntPtr)sourceBytes.Length };
            var error = new int[1];

            fixed (byte* sourceBytesPtr = sourceBytes)
            fixed (UIntPtr* lengthsPtr = lengths)
            {
                var sourcePtrs = stackalloc byte*[1];
                sourcePtrs[0] = sourceBytesPtr;

                var result = Bindings.OpenCl.CreateProgramWithSource(context.Handle,
                    1,
                    sourcePtrs,
                    lengthsPtr,
                    error
                );

                if (result == IntPtr.Zero)
                {
                    throw new Exception("Failed to create compute program!");
                }

                return new DeviceProgram(context, result);
            }
        }

        public unsafe void Build(Accelerator accelerator)
        {
            Build(accelerator, options: null);
        }

        /// <summary>
        /// Builds the program with optional build options
        /// </summary>
        /// <param name="accelerator">The accelerator (device) to build the program for</param>
        /// <param name="options">OpenCL build options (e.g., "-cl-std=CL2.0", "-Werror")</param>
        public unsafe void Build(Accelerator accelerator, string? options)
        {
            try
            {
                // Check build status before attempting build
                var initialStatus = GetBuildStatus();
                if (initialStatus == BuildStatus.Success)
                    return;

                var deviceHandle = accelerator.Handle;
                var error = (ErrorCodes) Bindings.OpenCl.BuildProgram(Handle,
                    1,
                    &deviceHandle,
                    options,
                    null,
                    null
                );

                if (error == ErrorCodes.Success) return;
                
                // Get comprehensive build information for better error reporting
                var buildLog = GetBuildLog();
                var buildOptions = GetBuildOptions();
                var finalStatus = GetBuildStatus();

                var errorMessage = $"Failed to build device program!\n" +
                                 $"Error Code: [{error}]\n" +
                                 $"Build Status: [{finalStatus}]\n" +
                                 $"Build Options: [{buildOptions}]\n" +
                                 $"Device: [{Context.Accelerator.Name}]\n" +
                                 $"Platform: [{Context.Accelerator.Vendor}]\n" +
                                 $"Build Log:\n{buildLog}";

                throw new Exception(errorMessage);
            }
            catch (AccessViolationException ex)
            {
                // Handle low-level crashes (like the LLVM assertion failure you encountered)
                var deviceInfo = $"Device: {Context.Accelerator.Name} ({Context.Accelerator.Vendor})";
                var suggestion = "This appears to be a driver-level crash. Consider:\n" +
                               "1. Updating your OpenCL drivers\n" +
                               "2. Simplifying the kernel code to identify problematic constructs\n" +
                               "3. Using different build options (e.g., -cl-std=CL1.2)\n" +
                               "4. Checking for unsupported vector operations or data types";
                
                throw new Exception($"OpenCL compiler crashed during build process.\n{deviceInfo}\n{suggestion}\n\nOriginal error: {ex.Message}", ex);
            }
            catch (Exception ex) when (!(ex is Exception && ex.Message.StartsWith("Failed to build device program")))
            {
                // Handle other unexpected exceptions during build
                var deviceInfo = $"Device: {Context.Accelerator.Name} ({Context.Accelerator.Vendor})";
                throw new Exception($"Unexpected error during program build.\n{deviceInfo}\n\nOriginal error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the OpenCL build options used to compile the program
        /// </summary>
        /// <returns>Build options string</returns>
        public unsafe string GetBuildOptions()
        {
            var buffer = new byte[1024];
            var length = new UIntPtr[1];

            var error = (ErrorCodes) Bindings.OpenCl.GetProgramBuildInfo(Handle,
                Context.Accelerator.Handle,
                ProgramBuildInfo.BuildOptions,
                (UIntPtr) buffer.Length,
                new Span<byte>(buffer),
                new Span<UIntPtr>(length)
            );

            if (error != ErrorCodes.Success)
                throw new Exception($"Failed to get build options: {error}");

            Array.Resize(ref buffer, (int) length[0]);
            return new string(buffer.Select(b => (char) b).ToArray()).Replace("\0", "");
        }

        /// <summary>
        /// Gets the build status for the program
        /// </summary>
        /// <returns>Build status</returns>
        public unsafe BuildStatus GetBuildStatus()
        {
            var status = new int[1];

            var error = (ErrorCodes) Bindings.OpenCl.GetProgramBuildInfo(Handle,
                Context.Accelerator.Handle,
                ProgramBuildInfo.BuildStatus,
                (UIntPtr) sizeof(int),
                new Span<int>(status),
                Span<UIntPtr>.Empty
            );

            if (error != ErrorCodes.Success)
                throw new Exception($"Failed to get build status: {error}");

            return (BuildStatus)status[0];
        }

        /// <summary>
        /// Gets the build log for the program (useful for debugging)
        /// </summary>
        /// <returns>Build log string</returns>
        public unsafe string GetBuildLog()
        {
            var buffer = new byte[2048];
            var length = new UIntPtr[1];

            var error = (ErrorCodes) Bindings.OpenCl.GetProgramBuildInfo(Handle,
                Context.Accelerator.Handle,
                ProgramBuildInfo.BuildLog,
                (UIntPtr) buffer.Length,
                new Span<byte>(buffer),
                new Span<UIntPtr>(length)
            );

            if (error != ErrorCodes.Success)
                throw new Exception($"Failed to get build log: {error}");

            Array.Resize(ref buffer, (int) length[0]);
            return new string(buffer.Select(b => (char) b).ToArray()).Replace("\0", "");
        }

        /// <summary>
        /// Gets the program source code (for debugging purposes)
        /// </summary>
        /// <returns>Program source as string</returns>
        public unsafe string GetSource()
        {
            var buffer = new byte[8192]; // Larger buffer for source code
            var length = new UIntPtr[1];

            var error = (ErrorCodes) Bindings.OpenCl.GetProgramInfo(Handle,
                ProgramInfo.Source,
                (UIntPtr) buffer.Length,
                new Span<byte>(buffer),
                new Span<UIntPtr>(length)
            );

            if (error != ErrorCodes.Success)
                throw new Exception($"Failed to get program source: {error}");

            if (length[0] == UIntPtr.Zero)
                return "Source not available";

            Array.Resize(ref buffer, (int) length[0]);
            return new string(buffer.Select(b => (char) b).ToArray()).Replace("\0", "");
        }

        /// <summary>
        /// Validates the program source for common issues that might cause compiler crashes
        /// </summary>
        /// <returns>List of potential issues found</returns>
        public List<string> ValidateSource()
        {
            var issues = new List<string>();
            
            try
            {
                var source = GetSource();
                
                // Check for common problematic patterns
                if (source.Contains("image2d_t*"))
                    issues.Add("Found image2d_t pointer - this might cause issues with some drivers. Consider using image2d_t directly.");
                
                if (source.Contains("vector") || source.Contains("Vector"))
                    issues.Add("Found vector type references - this might be related to the LLVM vector type assertion failure.");
                
                // Count vector type usage
                var vectorTypes = new[] { "float2", "float3", "float4", "int2", "int3", "int4", "uint2", "uint3", "uint4" };
                foreach (var vectorType in vectorTypes)
                {
                    if (source.Contains(vectorType))
                        issues.Add($"Found {vectorType} usage - ensure proper vector type handling.");
                }
                
                if (source.Contains("__kernel") && source.Split("__kernel").Length > 10)
                    issues.Add("Large number of kernels detected - consider splitting into multiple programs.");
                    
            }
            catch (Exception ex)
            {
                issues.Add($"Could not validate source: {ex.Message}");
            }
            
            return issues;
        }

        /// <summary>
        /// Attempts to build with comprehensive error handling and diagnostics
        /// </summary>
        /// <param name="accelerator">The accelerator (device) to build the program for</param>
        /// <param name="options">Build options</param>
        /// <param name="validate">Whether to validate source before building</param>
        /// <returns>Build success status and any warnings</returns>
        public (bool Success, List<string> Warnings) TryBuild(Accelerator accelerator, string? options = null, bool validate = true)
        {
            var warnings = new List<string>();
            
            try
            {
                if (validate)
                {
                    var issues = ValidateSource();
                    if (issues.Count > 0)
                    {
                        warnings.Add("Potential issues detected in source:");
                        warnings.AddRange(issues);
                    }
                }
                
                // Add defensive build options to prevent some crashes
                var safeOptions = options ?? "";
                if (!safeOptions.Contains("-cl-std="))
                {
                    safeOptions += " -cl-std=CL1.2"; // Use older standard for better compatibility
                    warnings.Add("Added -cl-std=CL1.2 for better driver compatibility");
                }
                
                Build(accelerator, safeOptions.Trim());
                return (true, warnings);
            }
            catch (Exception ex)
            {
                warnings.Add($"Build failed: {ex.Message}");
                
                // Suggest fallback options
                if (ex.Message.Contains("vector") || ex.Message.Contains("Vector"))
                {
                    warnings.Add("SUGGESTION: Try building with -cl-std=CL1.1 to avoid vector type issues");
                }
                
                if (ex.Message.Contains("image"))
                {
                    warnings.Add("SUGGESTION: Check image type declarations - avoid pointers to image types");
                }
                
                return (false, warnings);
            }
        }

        public Kernel BuildKernel(MethodInfo method)
        {
            return BuildKernel(CLGenerator.GenerateKernelName(method));
        }

        public Kernel BuildKernel(string name)
        {
            var kernel = Kernels.FirstOrDefault(k => k.Name == name);

            if (kernel != default)
            {
                return kernel;
            }

            var result = Bindings.OpenCl.CreateKernel(Handle, name, out var error);

            if (result == IntPtr.Zero)
            {
                throw new Exception($"Failed to create compute kernel!\nError: [{(ErrorCodes)error}]");
            }

            kernel = new Kernel(this, result, name);

            Kernels.Add(kernel);

            return kernel;
        }

        ~DeviceProgram()
        {
            ReleaseUnmanagedResources();
        }

        private void ReleaseUnmanagedResources()
        {
            foreach (var kernel in Kernels.ToArray())
            {
                kernel.Dispose();
            }

            Kernels.Clear();

            Context.DevicePrograms.Remove(this);

            Bindings.OpenCl.ReleaseProgram(Handle);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
    }
}