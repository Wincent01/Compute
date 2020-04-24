using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Mul)]
    public class MulInstruction : InstructionBase
    {
        public override string Compile()
        {
            Stack.Push($"(({Stack.Pop()}) * ({Stack.Pop()}))");

            return "";
        }
    }
}