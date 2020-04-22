using Mono.Cecil.Cil;

namespace Compute.ILKernel.Instructions
{
    [Instruction(Code.Brtrue_S)]
    public class BrSTrueInstruction : InstructionBase
    {
        public override string Compile()
        {
            var op = (Instruction) Instruction.Operand;

            return $"if ((({IlKernelCompiler.GenerateType(typeof(byte))}) {Stack.Pop()}) != 0) goto IL{op.Offset}";
        }
    }
}