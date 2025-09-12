using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Bge, Code.Bge_S)]
    public class BgeInstruction : InstructionBase
    {
        public override string Compile()
        {
            var op = (Instruction) Instruction.Operand;
            
            var a = Stack.Pop();
            var b = Stack.Pop();

            return $"if ({a} >= {b}) goto IL{op.Offset}";
        }
    }
}