using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Ldelema)]
    public class LdelemaInstruction : InstructionBase
    {
        public override string Compile()
        {
            var index = Stack.Pop();
            var array = Stack.Pop();
            
            Stack.Push($"&({array}[{index}])");

            return "";
        }
    }
}