using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Br_S)]
    public class BrSInstruction : InstructionBase
    {
        public override string Compile()
        {
            var op = (Instruction) Instruction.Operand;

            return $"goto IL{op.Offset}";
        }
    }
}