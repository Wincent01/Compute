using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using Compute.IL.AST.CodeGeneration;
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
        /// Compiles an action to a kernel
        /// </summary>
        public KernelDelegate? CompileAction(Action action, out string code, out string kernelName)
        {
            var target = action.Target;

            if (target == null)
                throw new InvalidOperationException("The provided action must be a closure capturing variables.");

            var closureType = target.GetType();

            var fields = closureType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            // This is not a kernel method, so we need to create a wrapper method that sets up the closure
            // and then calls the action.
            var kernelSource = CompileToAst(action.Method);

            code = GenerateCompleteSource();

            var wrapperBuilder = new StringBuilder();

            wrapperBuilder.AppendLine($"typedef struct {CodeGenerator.GenerateStructName(closureType)} {{");

            var paramterTypes = fields.Select(f => f.FieldType).ToArray();

            foreach (var field in fields)
            {
                var astType = AstType.FromClrType(field.FieldType);
                var fieldType = CodeGenerator.GenerateType(astType);
                wrapperBuilder.AppendLine($"    {CodeGenerator.GenerateTypeQualifiers(astType)} {((astType.IsPointer || astType.IsArray) ? $"__global " : "")} {fieldType} {field.Name};");
            }

            wrapperBuilder.AppendLine($"}} {CodeGenerator.GenerateStructName(closureType)};");

            wrapperBuilder.AppendLine();

            wrapperBuilder.AppendLine(code);

            kernelName = $"{CodeGenerator.GenerateFunctionName(kernelSource)}_kernel";

            wrapperBuilder.AppendLine();
            wrapperBuilder.AppendLine($"__kernel void {kernelName}(");
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var astType = AstType.FromClrType(field.FieldType);
                var fieldType = CodeGenerator.GenerateType(astType);
                wrapperBuilder.Append($"    {CodeGenerator.GenerateTypeQualifiers(astType)} {((astType.IsPointer || astType.IsArray) ? $"__global " : "")} {fieldType} {field.Name}");
                if (i < fields.Length - 1)
                    wrapperBuilder.AppendLine(",");
                else
                    wrapperBuilder.AppendLine();
            }

            wrapperBuilder.AppendLine(") {");
            wrapperBuilder.AppendLine($"    {CodeGenerator.GenerateType(closureType)} closure;");
            foreach (var field in fields)
            {
                wrapperBuilder.AppendLine($"    closure.{field.Name} = {field.Name};");
            }
            wrapperBuilder.AppendLine($"    {CodeGenerator.GenerateFunctionName(kernelSource)}(&closure);");
            wrapperBuilder.AppendLine("}");

            code = wrapperBuilder.ToString();

            try
            {
                // Write the code to a file for inspection
                System.IO.File.WriteAllText($"kernel_error.cl", code);

                var program = DeviceProgram.FromSource(Context, code);
                program.Build(Context.Accelerator, "-cl-std=CL2.0");

                var kernel = program.BuildKernel(kernelName);
                void kernelDelegate(WorkerDimensions workers, nuint[] parameters) => KernelInvoker(paramterTypes, parameters, kernel, workers);

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