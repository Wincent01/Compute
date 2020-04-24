using System;
using Mono.Cecil;

namespace Compute.IL.Compiler
{
    public static class TypeExtensions
    {
        public static Type FindType(this TypeReference @this)
        {
            return TypeHelper.Find(@this.FullName);
        }
        
        public static string CLString(this Type type, ILCode code)
        {
            return CLGenerator.GenerateType(type, code);
        }
    }
}