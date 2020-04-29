using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Or)]
    public class OrInstruction : InstructionBase
    {
        public override string Compile()
        {
            var a = Stack.Pop();
            var b = Stack.Pop();
            
            Stack.Push($"({b} | {a})");

            return "";
        }
    }
}