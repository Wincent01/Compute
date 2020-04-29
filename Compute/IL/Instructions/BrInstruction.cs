using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Br)]
    public class BrInstruction : InstructionBase
    {
        public override string Compile()
        {
            var op = (Instruction) Instruction.Operand;

            return $"goto IL{op.Offset}";
        }
    }
}