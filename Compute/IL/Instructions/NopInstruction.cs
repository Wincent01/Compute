using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Nop)]
    public class NopInstruction : InstructionBase
    {
        public override string Compile()
        {
            return "";
        }
    }
}