using System;
using System.Numerics;
using System.Reflection;
using System.Text;
using Compute.IL.Utility;

namespace Compute.IL.Compiler
{
    public static class CLGenerator
    {
        public static string GenerateSignature(MethodBase method, ILCode code)
        {
            var builder = new StringBuilder();

            var kernel = method.GetCustomAttribute<KernelAttribute>();

            if (kernel != null)
            {
                builder.Append("__kernel ");
            }

            if (method is MethodInfo methodInfo)
            {
                builder.Append($"{GenerateType(methodInfo.ReturnType, code)} {methodInfo.Name}_method_{methodInfo.MetadataToken}({GenerateArguments(methodInfo, code)})");
            }
            else if (method is ConstructorInfo constructorInfo)
            {
                builder.Append($"void {constructorInfo.DeclaringType.Name}_ctor_{constructorInfo.MetadataToken}({GenerateArguments(constructorInfo, code)})");
            }
            else
            {
                throw new NotSupportedException($"Only {nameof(MethodInfo)} and {nameof(ConstructorInfo)} are supported, got {method.GetType().Name}!");
            }

            return builder.ToString();
        }

        public static string GenerateArguments(MethodBase method, ILCode code)
        {
            var builder = new StringBuilder();

            if ((method is MethodInfo && !method.IsStatic) || method is ConstructorInfo)
            {
                builder.Append($"{GenerateType(method.DeclaringType, code)} this, ");
            }
            
            foreach (var parameter in method.GetParameters())
            {
                builder.Append($"{GenerateArgumentPrefix(parameter)} {GenerateType(parameter.ParameterType, code, true)} {parameter.Name}, ");
            }

            if (builder.Length > 2)
            {
                builder.Length -= 2;
            }

            return builder.ToString();
        }

        public static string GenerateKernelName(MethodBase method)
        {
            if (method is MethodInfo methodInfo)
            {
                return $"{methodInfo.Name}_method_{methodInfo.MetadataToken}";
            }
            else if (method is ConstructorInfo constructorInfo)
            {
                return $"{constructorInfo.DeclaringType.Name}_ctor_{constructorInfo.MetadataToken}";
            }
            else
            {
                throw new NotSupportedException($"Only {nameof(MethodInfo)} and {nameof(ConstructorInfo)} are supported, got {method.GetType().Name}!");
            }
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
        
        public static string GenerateType(Type type, ILCode code, bool structure = false)
        {
            var builder = new StringBuilder();
            
            var array = type.IsArray;

            if (array)
            {
                var memberType = type.GetElementType();

                builder.Append($"{GenerateType(memberType, code, structure)}*");

                return builder.ToString();
            }

            if (type.IsPrimitive)
            {
                return GeneratePrimitiveType(type);
            }

            if (type == typeof(void))
            {
                return "void";
            }

            if (type == typeof(string))
            {
                return "char*";
            }

            if (type.IsPointer)
            {
                builder.Append(type);

                builder.Length--;

                var baseType = TypeHelper.Find(builder.ToString());

                return $"{GenerateType(baseType, code, structure)}*";
            }

            if (!type.IsValueType)
            {
                throw new Exception($"Type must be a {nameof(ValueType)}, got {type}!");
            }
            
            if (code != default)
            {
                code.Link(type);
                
                return $"{(structure ? "struct " : "")}{type.Name}";
            }
            
            throw new NotSupportedException($"User defined structs are not yet implemented, cannot use {type}!");
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