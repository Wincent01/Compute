using Compute.IL.Compiler;
using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Ldc_I4,
        Code.Ldc_I4_0, Code.Ldc_I4_1, 
        Code.Ldc_I4_2, Code.Ldc_I4_3, 
        Code.Ldc_I4_5, Code.Ldc_I4_6,
        Code.Ldc_I4_7, Code.Ldc_I4_8,
        Code.Ldc_I4_M1, Code.Ldc_R4,
        Code.Ldc_R8, Code.Ldc_I4_4,
        Code.Ldc_I4_S)
    ]
    public class LdcInstruction : InstructionBase
    {
        public override string Compile()
        {
            switch (Instruction.OpCode.Code)
            {
                case Code.Ldc_I4:
                    Stack.Push((int) Instruction.Operand);
                    break;
                case Code.Ldc_I4_S:
                    Stack.Push((sbyte) Instruction.Operand);
                    break;
                case Code.Ldc_I4_0:
                    Stack.Push(0);
                    break;
                case Code.Ldc_I4_1:
                    Stack.Push(1);
                    break;
                case Code.Ldc_I4_2:
                    Stack.Push(2);
                    break;
                case Code.Ldc_I4_3:
                    Stack.Push(3);
                    break;
                case Code.Ldc_I4_4:
                    Stack.Push(4);
                    break;
                case Code.Ldc_I4_5:
                    Stack.Push(5);
                    break;
                case Code.Ldc_I4_6:
                    Stack.Push(6);
                    break;
                case Code.Ldc_I4_7:
                    Stack.Push(7);
                    break;
                case Code.Ldc_I4_8:
                    Stack.Push(8);
                    break;
                case Code.Ldc_I4_M1:
                    Stack.Push(-1);
                    break;
                case Code.Ldc_R8:
                    var b = Instruction.Operand.ToString();
                    Stack.Push($"(({typeof(double).CLString(Source)}) {b})");
                    break;
                case Code.Ldc_R4:
                    var f = Instruction.Operand.ToString();
                    if (!f.Contains('.') && !f.Contains('E') && !f.Contains('e')) f = $"{f}.0";
                    Stack.Push($"(({typeof(float).CLString(Source)}) {f}f)");
                    break;
            }

            return "";
        }
    }
}