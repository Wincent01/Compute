using System;
using Compute.IL.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Newarr)]
    public class NewarrInstruction : InstructionBase
    {
        public override string Compile()
        {
            var type = ((TypeReference) Instruction.Operand).FindType();
            var length = (int) Stack.Pop();

            var variable = $"tmp{Guid.NewGuid().ToString().Replace('-', 't')}";

            var assignment = $"{type.CLString(Source)} {variable}[{length}]";

            Prefix.Add(assignment);
            
            Stack.Push($"(&{variable})");

            return "";
        }
    }
}