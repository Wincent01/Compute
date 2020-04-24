using System;
using Mono.Cecil.Cil;

namespace Compute.IL
{
    [AttributeUsage(AttributeTargets.Class)]
    public class InstructionAttribute : Attribute
    {
        public Code[] Codes { get; }

        public InstructionAttribute(params Code[] codes)
        {
            Codes = codes;
        }
    }
}