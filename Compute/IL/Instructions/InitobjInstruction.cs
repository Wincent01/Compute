using System;
using Compute.IL.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Initobj)]
    public class InitobjInstruction : InstructionBase
    {
        public override string Compile()
        {
            var reference = (TypeReference) Instruction.Operand;

            var type = TypeHelper.Find(reference.FullName);

            Console.WriteLine($"{type} -> {Stack.Pop()}");

            return "";
        }
    }
}