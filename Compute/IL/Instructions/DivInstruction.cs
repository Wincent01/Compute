using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Div)]
    public class DivInstruction : InstructionBase
    {
        public override string Compile()
        {
            var a = Stack.Pop();
            var b = Stack.Pop();
            
            Stack.Push($"({b} / {a})");

            return "";
        }
    }
}