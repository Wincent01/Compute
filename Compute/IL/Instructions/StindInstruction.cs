using Compute.IL.Compiler;
using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Stind_R4, Code.Stind_I4)]
    public class StindInstruction : InstructionBase
    {
        public override string Compile()
        {
            var value = Stack.Pop();
            var dest = Stack.Pop();
            
            switch (Instruction.OpCode.Code)
            {
                case Code.Stind_R4:
                    return $"*{dest} = ({typeof(float).CLString(Source)}) {value}";
                case Code.Stind_I4:
                    return $"*{dest} = ({typeof(int).CLString(Source)}) {value}";
            }

            return "";
        }
    }
}