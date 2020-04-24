using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Shr)]
    public class ShiftRightInstruction : InstructionBase
    {
        public override string Compile()
        {
            var a = Stack.Pop();
            var b = Stack.Pop();
            
            Stack.Push($"({b} >> {a})");

            return "";
        }
    }
}