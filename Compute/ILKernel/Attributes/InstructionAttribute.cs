using System;
using Mono.Cecil.Cil;

namespace Compute.ILKernel
{
    [AttributeUsage(AttributeTargets.Class)]
    public class InstructionAttribute : Attribute
    {
        public Code Code { get; }

        public InstructionAttribute(Code code)
        {
            Code = code;
        }
    }
}