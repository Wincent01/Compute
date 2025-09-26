using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Ldflda)]
    public class LdfldaInstruction : InstructionBase
    {
        public override string Compile()
        {
            var instance = Stack.Pop();

            var field = (FieldReference) Instruction.Operand;

            var str = instance.ToString();

            var pointer = str.StartsWith("&(");

            Stack.Push($"&(({instance}){(pointer ? "->" : ".")}{field.Name})");

            return "";
        }
    }
}