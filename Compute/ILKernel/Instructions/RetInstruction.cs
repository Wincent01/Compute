using System;
using Mono.Cecil.Cil;

namespace Compute.ILKernel.Instructions
{
    [Instruction(Code.Ret)]
    public class RetInstruction : InstructionBase
    {
        public override string Compile()
        {
            var type = Type.GetType(Definition.ReturnType.FullName);

            if (type == typeof(void)) return "return";

            return $"return {Stack.Pop()}";
        }
    }
}