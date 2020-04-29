using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Ldarg,
        Code.Ldarg_0, Code.Ldarg_1,
        Code.Ldarg_2, Code.Ldarg_3,
        Code.Ldarga, Code.Ldarg_S,
        Code.Ldarga_S)
    ]
    public class LdargInstruction : InstructionBase
    {
        public override string Compile()
        {
            switch (Instruction.OpCode.Code)
            {
                case Code.Ldarg_0:
                    Stack.Push(GetArgument(0));
                    break;
                case Code.Ldarg_1:
                    Stack.Push(GetArgument(1));
                    break;
                case Code.Ldarg_2:
                    Stack.Push(GetArgument(2));
                    break;
                case Code.Ldarg_3:
                    Stack.Push(GetArgument(3));
                    break;
                case Code.Ldarg:
                    var index = (int) Instruction.Operand;
                    Stack.Push(GetArgument(index));
                    break;
                case Code.Ldarga:
                    index = (int) Instruction.Operand;
                    Stack.Push($"&{GetArgument(index)}");
                    break;
                case Code.Ldarg_S:
                    index = ((ParameterReference) Instruction.Operand).Index;
                    Stack.Push(GetArgument(index));
                    break;
                case Code.Ldarga_S:
                    index = ((ParameterDefinition) Instruction.Operand).Index;
                    Stack.Push($"&{GetArgument(index)}");
                    break;
            }

            return "";
        }
    }
}