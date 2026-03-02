using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using Compute.IL.AST.CodeGeneration;
using Compute.IL.AST.Transforms;
using Compute.IL.Utility;
using Mono.Cecil.Cil;

namespace Compute.IL.AST
{
    /// <summary>
    /// AST-based alternative to ILProgram that works exclusively with the new AST system
    /// Provides type-safe kernel compilation without string-based code generation
    /// </summary>
    public class AstProgram : IDisposable
    {
        public List<AstMethodSource> MethodSources { get; }
        public Context Context { get; }
        public Dictionary<MethodInfo, KernelDelegate> CompiledKernels { get; }
        public HashSet<Type> RequiredTypes { get; }
        public ICodeGenerator CodeGenerator { get; }
        public AstCompiler MethodCompiler { get; }

        public AstProgram(Context context, ICodeGenerator codeGenerator)
        {
            Context = context;
            CodeGenerator = codeGenerator;
            MethodSources = [];
            RequiredTypes = [];
            CompiledKernels = [];
            MethodCompiler = new AstCompiler();
        }

        public AstProgram(Accelerator accelerator, ICodeGenerator codeGenerator)
            : this(accelerator.CreateContext(), codeGenerator)
        {
        }

        /// <summary>
        /// Compiles a kernel method using the AST system
        /// </summary>
        public KernelDelegate? Compile<T>(T @delegate) where T : Delegate
        {
            return CompileDelegate(@delegate.Method, out _);
        }

        /// <summary>
        /// Compiles a kernel method using the AST system with source code output
        /// </summary>
        public KernelDelegate? Compile<T>(T @delegate, out string code) where T : Delegate
        {
            return CompileDelegate(@delegate.Method, out code);
        }

        /// <summary>
        /// Core compilation method that converts .NET methods to AST and then to target code
        /// </summary>
        public KernelDelegate? CompileDelegate(MethodInfo method, out string code)
        {
            code = "";

            if (CompiledKernels.TryGetValue(method, out var existingKernel))
            {
                return existingKernel;
            }

            if (!method.IsStatic)
            {
                throw new InvalidOperationException("AST kernels must be static methods!");
            }

            var attribute = method.GetCustomAttribute<KernelAttribute>();
            if (attribute == null)
            {
                throw new InvalidOperationException("AST kernels must be marked with the Kernel attribute!");
            }

            // Compile the method using AST
            var kernelSource = CompileToAst(method);
            code = GenerateCompleteSource();

            try
            {
                var program = DeviceProgram.FromSource(Context, code);
                program.Build(Context.Accelerator);

                var kernel = program.BuildKernel(method);
                void kernelDelegate(WorkerDimensions workers, nuint[] parameters) => KernelInvoker(kernelSource, parameters, kernel, workers);

                CompiledKernels[method] = kernelDelegate;
                return kernelDelegate;
            }
            catch (Exception e)
            {
                Console.WriteLine($"AST compilation failed for {method.Name}: {e}");
                return null;
            }
        }

        /// <summary>
        /// Compiles an action (closure/lambda) to a kernel.
        /// Uses the AST transform pipeline to inline the closure, producing a single
        /// flat __kernel function where each captured variable is a direct parameter.
        /// This avoids putting types like image2d_t inside structs (which OpenCL forbids).
        /// </summary>
        public KernelDelegate? CompileAction(Action action, out string code, out string kernelName)
        {
            var target = action.Target;

            if (target == null)
                throw new InvalidOperationException("The provided action must be a closure capturing variables.");

            var closureType = target.GetType();

            var fields = closureType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var parameterTypes = fields.Select(f => f.FieldType).ToArray();

            // 1. Compile the lambda body to AST (this produces this->field style accesses)
            var kernelSource = CompileToAst(action.Method);

            // 2. Run the transform pipeline to inline the closure
            var transformContext = new AstTransformContext
            {
                Method = action.Method,
                ClosureFields = fields,
                ClosureType = closureType,
                CodeGenerator = CodeGenerator
            };

            var pipeline = AstTransformPipeline.CreateDefault();
            kernelSource.Body = pipeline.Run(kernelSource.Body, transformContext);

            // 3. Generate the code — dependency structs and helper functions first
            var builder = new StringBuilder();

            // Add any required struct definitions (for types used in the kernel, NOT the closure struct)
            foreach (var structDef in RequiredTypes)
            {
                var structAst = new StructAstType(structDef);
                builder.AppendLine(CodeGenerator.GenerateTypeDefinition(structAst));
            }

            // Add helper function declarations and definitions (if the lambda calls other methods)
            foreach (var source in MethodSources)
            {
                if (source == kernelSource) continue; // Skip the kernel itself

                builder.AppendLine();
                builder.AppendLine($"{CodeGenerator.GenerateFunctionSignature(source)};");
            }

            foreach (var source in MethodSources)
            {
                if (source == kernelSource) continue;

                builder.AppendLine();
                builder.AppendLine(CodeGenerator.GenerateFunctionSignature(source));
                builder.AppendLine("{");
                builder.AppendLine(CodeGenerator.GenerateBody(source.Body));
                builder.AppendLine("}");
            }

            // 4. Generate the single flat __kernel function
            kernelName = $"{CodeGenerator.GenerateFunctionName(kernelSource)}_kernel";
            var kernelSignature = CodeGenerator.GenerateClosureKernelSignature(kernelName, fields);

            builder.AppendLine();
            builder.AppendLine(kernelSignature);
            builder.AppendLine("{");
            builder.AppendLine(CodeGenerator.GenerateBody(kernelSource.Body));
            builder.AppendLine("}");

            code = builder.ToString();

            try
            {
                // Write the code to a file for inspection
                System.IO.File.WriteAllText($"kernel_error.cl", code);

                var program = DeviceProgram.FromSource(Context, code);
                program.Build(Context.Accelerator, "-cl-std=CL2.0");

                var kernel = program.BuildKernel(kernelName);
                void kernelDelegate(WorkerDimensions workers, nuint[] parameters) => KernelInvoker(parameterTypes, parameters, kernel, workers);

                return kernelDelegate;
            }
            catch (Exception e)
            {
                Console.WriteLine($"AST action compilation failed for {action.Method.Name}: {e}");
                return null;
            }
        }

        /// <summary>
        /// Compiles a method to AST representation
        /// </summary>
        public AstMethodSource CompileToAst(MethodBase method)
        {
            var existingSource = MethodSources.FirstOrDefault(s => s.Method.Equals(method));
            if (existingSource != null)
            {
                return existingSource;
            }

            var definition = TypeHelper.FindMethodDefinition(method);

            if (definition == null)
            {
                throw new InvalidOperationException($"Method {method.Name} not found in type {method.DeclaringType?.FullName}");
            }

            var block = MethodCompiler.CompileMethodBody(definition, out var typeDependencies, out var methodDependencies);

            if (block == null)
            {
                throw new InvalidOperationException($"Failed to compile method body for {method.Name}");
            }

            var kernelSource = new AstMethodSource(method, block);
            MethodSources.Add(kernelSource);

            // Add type dependencies (e.g., structs)
            foreach (var type in typeDependencies)
            {
                var attributes = type.GetCustomAttributes(typeof(AliasAttribute), false);

                if (attributes.Length > 0)
                {
                    continue; // Skip alias types
                }

                if (type.IsValueType && !type.IsPrimitive && !type.IsEnum)
                {
                    RequiredTypes.Add(type);
                }
            }

            // Add method dependencies
            foreach (var dep in methodDependencies)
            {
                var methodBase = TypeHelper.FindMethod(dep);

                if (methodBase == null)
                {
                    throw new InvalidOperationException($"Method dependency {dep.Name} not found in type {dep.DeclaringType?.FullName}");
                }

                var attributes = methodBase.GetCustomAttributes(typeof(AliasAttribute), false);

                if (attributes.Length > 0)
                {
                    continue; // Skip alias methods
                }

                CompileToAst(methodBase);
            }

            return kernelSource;
        }

        /// <summary>
        /// Generates complete OpenCL source code from AST
        /// </summary>
        public string GenerateCompleteSource()
        {
            var builder = new StringBuilder();
            
            // Add any required struct definitions
            foreach (var structDef in RequiredTypes)
            {
                var structAst = new StructAstType(structDef);

                builder.AppendLine(CodeGenerator.GenerateTypeDefinition(structAst));
            }

            // Add function declarations and definitions
            foreach (var kernelSource in MethodSources)
            {
                builder.AppendLine();
                builder.AppendLine($"{CodeGenerator.GenerateFunctionSignature(kernelSource)};");
                builder.AppendLine();
            }

            foreach (var kernelSource in MethodSources)
            {
                builder.AppendLine();
                builder.AppendLine(CodeGenerator.GenerateFunctionSignature(kernelSource));
                builder.AppendLine("{");
                builder.AppendLine(CodeGenerator.GenerateBody(kernelSource.Body));
                builder.AppendLine("}");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Kernel invocation logic compatible with existing system
        /// </summary>
        private unsafe void KernelInvoker(AstMethodSource source, UIntPtr[] parameters, Kernel kernel, WorkerDimensions workers)
        {
            Span<KernelArgument> values = stackalloc KernelArgument[parameters.Length];

            var method = source.Method;
            var parameterTypes = method.GetParameters();

            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];

                Type parameterType;

                if (method.DeclaringType != null && !method.IsStatic)
                {
                    if (index == 0)
                    {
                        parameterType = method.DeclaringType;
                    }
                    else
                    {
                        parameterType = parameterTypes[index - 1].ParameterType;
                    }
                }
                else
                {
                    parameterType = parameterTypes[index].ParameterType;
                }

                if (parameterType.IsArray)
                {
                    values[index] = new KernelArgument
                    {
                        Size = 8, // Pointer size
                        Value = parameter
                    };
                }
                else if (parameterType.IsClass)
                {
                    values[index] = new KernelArgument
                    {
                        Size = 8, // Pointer size for class references
                        Value = parameter
                    };
                }
                else
                {
                    values[index] = new KernelArgument
                    {
                        Size = (uint)Marshal.SizeOf(parameterType),
                        Value = parameter
                    };
                }
            }

            lock (kernel)
            {
                kernel.InvokeAuto(workers, values);
            }
        }

        /// <summary>
        /// Kernel invocation logic compatible with existing system
        /// </summary>
        private unsafe void KernelInvoker(Type[] parameterTypes, UIntPtr[] parameters, Kernel kernel, WorkerDimensions workers)
        {
            Span<KernelArgument> values = stackalloc KernelArgument[parameters.Length];

            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                var parameterType = parameterTypes[index];

                if (parameterType.IsArray)
                {
                    values[index] = new KernelArgument
                    {
                        Size = 8, // Pointer size
                        Value = parameter
                    };
                }
                else if (parameterType.IsClass)
                {
                    values[index] = new KernelArgument
                    {
                        Size = 8, // Pointer size for class references
                        Value = parameter
                    };
                }
                else
                {
                    values[index] = new KernelArgument
                    {
                        Size = (uint)Marshal.SizeOf(parameterType),
                        Value = parameter
                    };
                }
            }

            lock (kernel)
            {
                kernel.InvokeAuto(workers, values);
            }
        }

        /// <summary>
        /// Gets all compiled kernels
        /// </summary>
        public IEnumerable<AstMethodSource> GetKernels() => MethodSources.Where(s => s.IsKernel);

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Context?.Dispose();
            }
        }
    }
}