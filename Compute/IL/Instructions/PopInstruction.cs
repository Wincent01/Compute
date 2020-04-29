using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Pop)]
    public class PopInstruction : InstructionBase
    {
        public override string Compile()
        {
            Stack.Pop();

            return "";
        }
    }
}