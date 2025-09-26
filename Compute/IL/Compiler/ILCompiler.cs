using System;
using System.Reflection;
using System.Text;
using Mono.Cecil;

namespace Compute.IL.Compiler
{
    internal static class ILCompiler
    {
        public static string Compile(MethodBase method, ILSource source)
        {
            var builder = new StringBuilder();

            var atomic = method.GetCustomAttribute<AtomicAttribute>();

            if (atomic != null)
            {
                builder.AppendLine("#pragma OPENCL EXTENSION cl_khr_int64_base_atomics : enable");
                builder.AppendLine("#pragma OPENCL EXTENSION cl_khr_int64_extended_atomics : enable");
            }

            builder.Append($"{CLGenerator.GenerateSignature(method, source)}");

            builder.Append("\n{\n");

            builder.Append($"{GenerateBody(method, source)}"); 
            
            builder.Append("\n}\n");

            return builder.ToString();
        }

        public static string GenerateBody(MethodBase method, ILSource source)
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

                    return CLBodyGenerator.GenerateBodyContent(info, source);
                }
            }
            
            return "";
        }
    }
}