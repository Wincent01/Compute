using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Dup)]
    public class DupInstruction : InstructionBase
    {
        public override string Compile()
        {
            var element = Stack.Pop();

            Stack.Push(element);
            Stack.Push(element);

            return "";
        }
    }
}