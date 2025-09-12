using Compute.IL.Compiler;
using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(
        Code.Conv_R4,
        Code.Conv_U, Code.Conv_I8,
        Code.Conv_U8, Code.Conv_R_Un,
        Code.Conv_Ovf_I
    )]
    public class ConvInstruction : InstructionBase
    {
        public override string Compile()
        {
            switch (Instruction.OpCode.Code)
            {
                case Code.Conv_U:
                    Stack.Push($"({typeof(int).CLString(Source)}) ({Stack.Pop()})");
                    break;
                case Code.Conv_I8:
                    Stack.Push($"({typeof(long).CLString(Source)}) ({Stack.Pop()})");
                    break;
                case Code.Conv_U8:
                    Stack.Push($"({typeof(ulong).CLString(Source)}) ({Stack.Pop()})");
                    break;
                case Code.Conv_R4:
                    Stack.Push($"({typeof(float).CLString(Source)}) ({Stack.Pop()})");
                    break;
                case Code.Conv_R_Un:
                    Stack.Push($"({typeof(float).CLString(Source)}) ({Stack.Pop()})");
                    break;
                case Code.Conv_Ovf_I:
                    Stack.Push($"({typeof(int).CLString(Source)}) ({Stack.Pop()})");
                    break;
            }

            return "";
        }
    }
}