using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Beq, Code.Beq_S)]
    public class BeqInstruction : InstructionBase
    {
        public override string Compile()
        {
            var op = (Instruction) Instruction.Operand;
            
            var a = Stack.Pop();
            var b = Stack.Pop();

            return $"if ({a} == {b}) goto IL{op.Offset}";
        }
    }
}