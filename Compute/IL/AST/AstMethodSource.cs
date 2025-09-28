using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Compute.IL.AST.CodeGeneration;
using Compute.IL.AST.Statements;

namespace Compute.IL.AST
{
    /// <summary>
    /// AST-based equivalent of ILSource for representing compiled kernel methods
    /// Contains the AST representation and can generate target code
    /// </summary>
    public class AstMethodSource
    {
        public MethodBase Method { get; set; }
        public BlockStatement Body { get; set; }
        public bool IsKernel { get; set; }

        public AstMethodSource(MethodBase method, BlockStatement body)
        {
            Method = method;
            Body = body;
            
            // Check if this is a kernel method
            IsKernel = method.GetCustomAttribute<KernelAttribute>() != null;
        }

        public override string ToString()
        {
            return $"{Method.Name} ({(IsKernel ? "Kernel" : "Function")})";
        }
    }

    /// <summary>
    /// Represents a parameter in an AST kernel/function
    /// </summary>
    public class AstParameter
    {
        public string Name { get; set; }
        public AstType Type { get; set; }
        public Attribute? Attribute { get; set; } // Global, Local, Const, etc.

        public AstParameter(string name, AstType type, Attribute? attribute)
        {
            Name = name;
            Type = type;
            Attribute = attribute;
        }
    }
}