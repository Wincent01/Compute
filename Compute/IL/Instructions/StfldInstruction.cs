using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Stfld)]
    public class StfldInstruction : InstructionBase
    {
        public override string Compile()
        {
            var value = Stack.Pop();
            var instance = Stack.Pop();

            var field = (FieldReference) Instruction.Operand;

            var str = instance.ToString();

            var pointer = str.StartsWith("&(");
            
            return $"({instance}){(pointer ? "->" : ".")}{field.Name} = {value}";
        }
    }
}