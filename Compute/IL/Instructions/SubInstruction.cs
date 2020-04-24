using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Sub)]
    public class SubInstruction : InstructionBase
    {
        public override string Compile()
        {
            var a = Stack.Pop();
            var b = Stack.Pop();
            
            Stack.Push($"({b} - {a})");

            return "";
        }
    }
}