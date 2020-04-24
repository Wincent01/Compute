using Compute.IL.Compiler;
using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Ldelem_R4, Code.Ldelem_Any)]
    public class LdelemInstruction : InstructionBase
    {
        public override string Compile()
        {
            var index = Stack.Pop();
            var array = Stack.Pop();

            switch (Instruction.OpCode.Code)
            {
                case Code.Ldelem_Any:
                    Stack.Push($"({array}[{index}])");
                    break;
                case Code.Ldelem_R4:
                    Stack.Push($"(({typeof(float).CLString(Source)}) {array}[{index}])");
                    break;
            }

            return "";
        }
    }
}