using Compute.IL.Compiler;
using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Clt, Code.Clt_Un)]
    public class CltInstruction : InstructionBase
    {
        public override string Compile()
        {
            var value1 = Stack.Pop();
            var value2 = Stack.Pop();

            var type = typeof(int).CLString(Source);

            Stack.Push($"((({type}) {value1}) > (({type}) {value2}))");

            return "";
        }
    }
}