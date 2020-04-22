using Mono.Cecil.Cil;

namespace Compute.ILKernel.Instructions
{
    [Instruction(Code.Ceq)]
    public class CeqInstruction : InstructionBase
    {
        public override string Compile()
        {
            var a = Stack.Pop();
            var b = Stack.Pop();
            
            Stack.Push($"({b} == {a})");

            return "";
        }
    }
}