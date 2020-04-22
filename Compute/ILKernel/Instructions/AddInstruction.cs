using Mono.Cecil.Cil;

namespace Compute.ILKernel.Instructions
{
    [Instruction(Code.Add)]
    public class AddInstruction : InstructionBase
    {
        public override string Compile()
        {
            var a = Stack.Pop();
            var b = Stack.Pop();
            
            Stack.Push($"({a} + {b})");

            return "";
        }
    }
}