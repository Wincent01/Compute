using Compute.IL.Compiler;
using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Stelem_R4, Code.Stelem_Any)]
    public class StelemInstruction : InstructionBase
    {
        public override string Compile()
        {
            var value = Stack.Pop();
            var index = Stack.Pop();
            var array = Stack.Pop();
            
            switch (Instruction.OpCode.Code)
            {
                case Code.Stelem_Any:
                    return $"{array}[{index}] = ({value})";
                case Code.Stelem_R4:
                    return $"{array}[{index}] = (({typeof(float).CLString(Source)}) {value})";
            }

            return "";
        }
    }
}