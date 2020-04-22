using Mono.Cecil.Cil;

namespace Compute.ILKernel.Instructions
{
    [Instruction(Code.Brtrue)]
    public class BrTrueInstruction : InstructionBase
    {
        public override string Compile()
        {
            var op = (Instruction) Instruction.Operand;

            return $"if ((({IlKernelCompiler.GenerateType(typeof(int))}) {Stack.Pop()}) != 0) goto IL{op.Offset}";
        }
    }
}