using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.And)]
    public class AndInstruction : InstructionBase
    {
        public override string Compile()
        {
            var a = Stack.Pop();
            var b = Stack.Pop();
            
            Stack.Push($"({b} & {a})");

            return "";
        }
    }
}