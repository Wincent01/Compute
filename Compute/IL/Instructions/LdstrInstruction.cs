using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Ldstr)]
    public class LdstrInstruction : InstructionBase
    {
        public override string Compile()
        {
            var value = (string) Instruction.Operand;

            Stack.Push($"\"{value}\"");

            return "";
        }
    }
}