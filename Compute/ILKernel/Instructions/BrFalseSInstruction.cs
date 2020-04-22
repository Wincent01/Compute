using Mono.Cecil.Cil;

namespace Compute.ILKernel.Instructions
{
    [Instruction(Code.Brfalse_S)]
    public class BrFalseSInstruction : InstructionBase
    {
        public override string Compile()
        {
            var op = (Instruction) Instruction.Operand;

            return $"if ((({IlKernelCompiler.GenerateType(typeof(byte))}) {Stack.Pop()}) == 0) goto IL{op.Offset}";
        }
    }
}