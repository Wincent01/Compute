using Mono.Cecil.Cil;

namespace Compute.ILKernel.Instructions
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