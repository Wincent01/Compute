using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Compute.ILKernel.Instructions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace Compute.ILKernel
{
    internal static class IlKernelCompiler
    {
        public static MethodInfo FindInfo(MethodDefinition method)
        {
            var type = Type.GetType(method.DeclaringType.FullName);

            if (type == default)
            {
                foreach (var assembly in ILKernel.Assemblies)
                {
                    type = assembly.GetType(method.DeclaringType.FullName);
                }

                if (type == default)
                {
                    throw new Exception($"Failed to find type {method.DeclaringType.FullName}!");
                }
            }
            
            var info = type.GetMethod(method.Name);

            if (info == default)
            {
                throw new Exception($"Failed to find method {method.Name}");
            }

            return info;
        }
        
        public static string CompileKernel(MethodInfo method)
        {
            var context = new ILProgramContext();
            
            var builder = new StringBuilder();

            builder.Append($"{GenerateKernelSignature(method)}");

            builder.Append("\n{\n");

            builder.Append($"{GenerateBody(method, context)}");
            
            builder.Append("\n}\n");
            
            context.CompileHelpers();

            return $"{context.HelperSource()}\n{builder}";
        }

        public static string Compile(MethodInfo method, ILProgramContext context)
        {
            var builder = new StringBuilder();

            builder.Append($"{GenerateSignature(method)}");

            builder.Append("\n{\n");

            builder.Append($"{GenerateBody(method, context)}");
            
            builder.Append("\n}\n");

            return builder.ToString();
        }

        public static string GenerateKernelSignature(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
            {
                throw new Exception($"Kernels must have return type Void, got {method.ReturnType}!");
            }

            var builder = new StringBuilder();

            builder.Append($"__kernel void {method.Name}({GenerateArguments(method)})");

            return builder.ToString();
        }

        public static string GenerateSignature(MethodInfo method)
        {
            var builder = new StringBuilder();

            builder.Append($"{GenerateType(method.ReturnType)} {method.Name}({GenerateArguments(method)})");

            return builder.ToString();
        }

        public static string GenerateArguments(MethodInfo method)
        {
            var builder = new StringBuilder();
            
            foreach (var parameter in method.GetParameters())
            {
                builder.Append($"{GenerateArgumentPrefix(parameter)} {GenerateType(parameter.ParameterType)} {parameter.Name}, ");
            }

            if (builder.Length > 2)
            {
                builder.Length -= 2;
            }

            return builder.ToString();
        }

        public static string GenerateArgumentPrefix(ParameterInfo parameter)
        {
            var global = parameter.GetCustomAttribute<GlobalAttribute>();

            if (global != null)
            {
                return "__global";
            }
            
            var constant = parameter.GetCustomAttribute<ConstAttribute>();

            if (constant != null)
            {
                return "const";
            }

            return "";
        }

        public static string GenerateBody(MethodInfo method, ILProgramContext context)
        {
            var declaring = method.DeclaringType;

            if (declaring == default)
            {
                throw new Exception($"Failed to find declaring type for {method.Name}!");
            }
            
            var assembly = AssemblyDefinition.ReadAssembly(declaring.Assembly.Location);

            foreach (var type in assembly.MainModule.Types)
            {
                if (type.FullName != declaring.FullName) continue;

                foreach (var info in type.Methods)
                {
                    if (info.Name != method.Name) continue;

                    return GenerateBodyContent(info, context);
                }
            }
            
            return "";
        }

        public static string GenerateBodyContent(MethodDefinition definition, ILProgramContext context)
        {
            var body = definition.Body;

            var instructions = typeof(ILKernel).Assembly.GetTypes().Where(
                i => i.BaseType == typeof(InstructionBase)
            ).ToArray();
            
            var codes = new Dictionary<Code, Type>();

            foreach (var instruction in instructions)
            {
                var attribute = instruction.GetCustomAttribute<InstructionAttribute>();

                if (attribute == null) continue;

                codes[attribute.Code] = instruction;
            }

            var stack = new Stack<object>();
            
            var variables = new Dictionary<int, object>();
            
            var builder = new StringBuilder();

            builder.AppendLine(GenerateVariables(body));

            foreach (var instruction in body.Instructions)
            {
                builder.AppendLine($"IL{instruction.Offset}:");
                
                var isBasic = CompileBasicInstruction(instruction, stack, variables, body, out var cl);

                if (!isBasic)
                {
                    if (!codes.TryGetValue(instruction.OpCode.Code, out var type))
                    {
                        throw new NotSupportedException($"The {instruction.OpCode.Code} OpCode is not supported!");
                    }

                    var instance = (InstructionBase) Activator.CreateInstance(type);

                    instance.Instruction = instruction;
                    instance.Definition = definition;
                    instance.Body = body;
                    instance.Stack = stack;
                    instance.Variables = variables;
                    instance.Context = context;

                    cl = instance.Compile();
                }

                if (string.IsNullOrWhiteSpace(cl)) continue;

                builder.AppendLine($"\t{cl};");
            }

            return builder.ToString();
        }

        public static string GenerateVariables(MethodBody body)
        {
            var builder = new StringBuilder();

            for (var index = 0; index < body.Variables.Count; index++)
            {
                var variable = body.Variables[index];
                var type = Type.GetType(variable.VariableType.FullName);

                var init = body.InitLocals;

                builder.AppendLine($"\t{GenerateType(type)} local{index}{(init ? " = 0" : "")};");
            }

            return builder.ToString();
        }

        public static string SetVariable(MethodBody body, int index, object variable)
        {
            var type = Type.GetType(body.Variables[index].VariableType.FullName);

            return $"local{index} = ({GenerateType(type)}) ({variable})";
        }

        public static string GetVariable(int index)
        {
            return $"local{index}";
        }

        public static string SetTemporaryVariable(Type type, object variable, out string name)
        {
            var guid = Guid.NewGuid().ToString().Replace('-', '_');

            name = $"temp{guid}";
            
            return $"{GenerateType(type)} {name} = ({GenerateType(type)}) ({variable})";
        }

        public static string GetArgument(MethodBody body, int index)
        {
            return body.Method.Parameters[index].Name;
        }

        public static bool CompileBasicInstruction(Instruction instruction, Stack<object> stack, Dictionary<int, object> variables, MethodBody body, out string cl)
        {
            cl = string.Empty;
            
            var code = instruction.OpCode.Code;

            var op = instruction.Operand;

            // TODO: Move a majority of these to instruction classes
            
            int index;
            object obj;
            switch (code)
            {
                case Code.Nop:
                    break;
                case Code.Ldc_I4:
                    stack.Push((int) op);
                    break;
                case Code.Ldc_I4_0:
                    stack.Push(0);
                    break;
                case Code.Ldc_I4_1:
                    stack.Push(1);
                    break;
                case Code.Ldc_I4_2:
                    stack.Push(2);
                    break;
                case Code.Ldc_I4_3:
                    stack.Push(3);
                    break;
                case Code.Ldc_I4_4:
                    stack.Push(4);
                    break;
                case Code.Ldc_I4_5:
                    stack.Push(5);
                    break;
                case Code.Ldc_I4_6:
                    stack.Push(6);
                    break;
                case Code.Ldc_I4_7:
                    stack.Push(7);
                    break;
                case Code.Ldc_I4_8:
                    stack.Push(8);
                    break;
                case Code.Ldc_I4_M1:
                    stack.Push(-1);
                    break;
                case Code.Stloc_0:
                    variables[0] = stack.Pop();
                    cl = SetVariable(body, 0, variables[0]);
                    break;
                case Code.Stloc_1:
                    variables[1] = stack.Pop();
                    cl = SetVariable(body, 1, variables[1]);
                    break;
                case Code.Stloc_2:
                    variables[2] = stack.Pop();
                    cl = SetVariable(body, 2, variables[2]);
                    break;
                case Code.Stloc_3:
                    variables[3] = stack.Pop();
                    cl = SetVariable(body, 3, variables[3]);
                    break;
                case Code.Stloc:
                    index = (int) op;
                    variables[index] = stack.Pop();
                    cl = SetVariable(body, index, variables[index]);
                    break;
                case Code.Stloc_S:
                    if (op is VariableDefinition definition)
                    {
                        index = definition.Index;
                    }
                    else
                    {
                        index = (int) op;
                    }

                    variables[index] = stack.Pop();
                    cl = SetVariable(body, index, variables[index]);
                    break;
                case Code.Ldloc_0:
                    stack.Push(GetVariable(0));
                    break;
                case Code.Ldloc_1:
                    stack.Push(GetVariable(1));
                    break;
                case Code.Ldloc_2:
                    stack.Push(GetVariable(2));
                    break;
                case Code.Ldloc_3:
                    stack.Push(GetVariable(3));
                    break;
                case Code.Ldloc:
                    index = (int) op;
                    stack.Push(GetVariable(index));
                    break;
                case Code.Ldloc_S:
                    if (op is VariableDefinition variable)
                    {
                        index = variable.Index;
                    }
                    else
                    {
                        index = (int) op;
                    }
                    
                    stack.Push(GetVariable(index));
                    break;
                case Code.Ldarg_0:
                    stack.Push(GetArgument(body, 0));
                    break;
                case Code.Ldarg_1:
                    stack.Push(GetArgument(body, 1));
                    break;
                case Code.Ldarg_2:
                    stack.Push(GetArgument(body, 2));
                    break;
                case Code.Ldarg_3:
                    stack.Push(GetArgument(body, 3));
                    break;
                case Code.Ldarg:
                    index = (int) op;
                    stack.Push(GetArgument(body, index));
                    break;
                case Code.Ldarga:
                    index = (int) op;
                    stack.Push($"&{GetArgument(body, index)}");
                    break;
                case Code.Ldarga_S:
                    index = ((ParameterDefinition) op).Index;
                    stack.Push($"&{GetArgument(body, index)}");
                    break;
                case Code.Conv_U:
                    stack.Push($"({GenerateType(typeof(int))}) ({stack.Pop()})");
                    break;
                case Code.Conv_I8:
                    stack.Push($"({GenerateType(typeof(long))}) ({stack.Pop()})");
                    break;
                case Code.Conv_U8:
                    stack.Push($"({GenerateType(typeof(ulong))}) ({stack.Pop()})");
                    break;
                case Code.Conv_R4:
                    stack.Push($"({GenerateType(typeof(float))}) ({stack.Pop()})");
                    break;
                case Code.Ldelem_R4:
                    obj = stack.Pop();
                    stack.Push($"(({GenerateType(typeof(float))}) {stack.Pop()}[{obj}])");
                    break;
                case Code.Ldind_R4:
                    obj = stack.Pop();
                    stack.Push($"(({GenerateType(typeof(float))}) *({obj}))");
                    break;
                case Code.Ldind_I4:
                    obj = stack.Pop();
                    var type = GenerateType(typeof(int));
                    stack.Push($"(({type}) *(({type}*) ({obj})))");
                    break;
                case Code.Stelem_R4:
                    var v1 = stack.Pop();
                    var v2 = stack.Pop();
                    var v3 = stack.Pop();
                    cl = $"{v3}[{v2}] = (({GenerateType(typeof(float))}) {v1})";
                    break;
                case Code.Ldc_R8:
                    var b = op.ToString();
                    stack.Push($"(({GenerateType(typeof(double))}) {b})");
                    break;
                case Code.Ldc_R4:
                    var f = op.ToString();
                    if (!f.Contains('.'))
                        f = $"{f}.0";
                    stack.Push($"(({GenerateType(typeof(float))}) {f}f)");
                    break;
                case Code.Stind_R4:
                    var v = stack.Pop();
                    var a = stack.Pop();
                    cl = $"*{a} = ({GenerateType(typeof(float))}) {v}";
                    break;
                default:
                    return false;
            }

            return true;
        }
        
        public static string GenerateType(Type type)
        {
            var builder = new StringBuilder();
            
            var array = type.IsArray;

            if (array)
            {
                var memberType = type.GetElementType();

                builder.Append($"{GenerateType(memberType)}*");

                return builder.ToString();
            }

            if (type.IsPrimitive)
            {
                return GeneratePrimitiveType(type);
            }

            if (!type.IsValueType)
            {
                throw new Exception($"Type must be a {nameof(ValueType)}, got {type}!");
            }
            
            throw new NotSupportedException($"User defined structs are not yet implemented!");
        }

        public static string GeneratePrimitiveType(Type type)
        {
            if (type == typeof(bool))
            {
                return "int";
            }

            if (type == typeof(byte))
            {
                return "unsigned char";
            }

            if (type == typeof(sbyte))
            {
                return "char";
            }

            if (type == typeof(short))
            {
                return "short";
            }

            if (type == typeof(ushort))
            {
                return "unsigned short";
            }

            if (type == typeof(int))
            {
                return "int";
            }

            if (type == typeof(uint))
            {
                return "unsigned int";
            }

            if (type == typeof(long))
            {
                return "long long";
            }

            if (type == typeof(ulong))
            {
                return "unsigned long long";
            }

            if (type == typeof(char))
            {
                return "unsigned short";
            }

            if (type == typeof(double))
            {
                return "double";
            }

            if (type == typeof(float))
            {
                return "float";
            }

            if (type == typeof(IntPtr))
            {
                return "int*";
            }

            if (type == typeof(UIntPtr))
            {
                return "unsigned int*";
            }
            
            throw new Exception($"Got unsupported primitive type {type}!");
        }
    }
}