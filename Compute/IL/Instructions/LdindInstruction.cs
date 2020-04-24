using Compute.IL.Compiler;
using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Ldind_R4, Code.Ldind_I4)]
    public class LdindInstruction : InstructionBase
    {
        public override string Compile()
        {
            var obj = Stack.Pop();
            
            switch (Instruction.OpCode.Code)
            {
                case Code.Ldind_R4:
                    Stack.Push($"(({typeof(float).CLString(Source)}) *({obj}))");
                    break;
                case Code.Ldind_I4:
                    var type = typeof(int).CLString(Source);
                    Stack.Push($"(({type}) *(({type}*) ({obj})))");
                    break;
            }

            return "";
        }
    }
}