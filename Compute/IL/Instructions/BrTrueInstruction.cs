using Compute.IL.Compiler;
using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Brtrue)]
    public class BrTrueInstruction : InstructionBase
    {
        public override string Compile()
        {
            var op = (Instruction) Instruction.Operand;

            return $"if ((({(typeof(int).CLString(Source))}) {Stack.Pop()}) != 0) goto IL{op.Offset}";
        }
    }
}