using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Ldloc, Code.Ldloc_0,
        Code.Ldloc_1, Code.Ldloc_2,
        Code.Ldloc_3, Code.Ldloc_S,
        Code.Ldloca, Code.Ldloca_S)
    ]
    public class LdlocInstruction : InstructionBase
    {
        public override string Compile()
        {
            switch (Instruction.OpCode.Code)
            {
                case Code.Ldloc_0:
                    Stack.Push(GetVariable(0));
                    break;
                case Code.Ldloc_1:
                    Stack.Push(GetVariable(1));
                    break;
                case Code.Ldloc_2:
                    Stack.Push(GetVariable(2));
                    break;
                case Code.Ldloc_3:
                    Stack.Push(GetVariable(3));
                    break;
                case Code.Ldloc:
                    var index = (int) Instruction.Operand;
                    Stack.Push(GetVariable(index));
                    break;
                case Code.Ldloca:
                    index = (int) Instruction.Operand;
                    Stack.Push($"&({GetVariable(index)})");
                    break;
                case Code.Ldloc_S:
                    index = ((VariableDefinition) Instruction.Operand).Index;
                    Stack.Push(GetVariable(index));
                    break;
                case Code.Ldloca_S:
                    index = ((VariableDefinition) Instruction.Operand).Index;
                    Stack.Push($"&({GetVariable(index)})");
                    break;
            }

            return "";
        }
    }
}