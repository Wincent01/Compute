using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Stloc_0, 
        Code.Stloc_1, Code.Stloc_2, 
        Code.Stloc_3, Code.Stloc,
        Code.Stloc_S)
    ]
    public class StlocInstruction : InstructionBase
    {
        public override string Compile()
        {
            switch (Instruction.OpCode.Code)
            {
                case Code.Stloc_0:
                    Variables[0] = Stack.Pop();
                    return SetVariable(0, Variables[0]);
                case Code.Stloc_1:
                    Variables[1] = Stack.Pop();
                    return SetVariable(1, Variables[1]);
                case Code.Stloc_2:
                    Variables[2] = Stack.Pop();
                    return SetVariable(2, Variables[2]);
                case Code.Stloc_3:
                    Variables[3] = Stack.Pop();
                    return SetVariable(3, Variables[3]);
                case Code.Stloc:
                    var index = (int) Instruction.Operand;
                    Variables[index] = Stack.Pop();
                    return SetVariable(index, Variables[index]);
                case Code.Stloc_S:
                    index = ((VariableDefinition) Instruction.Operand).Index;
                    Variables[index] = Stack.Pop();
                    return SetVariable(index, Variables[index]);
            }

            return "";
        }
    }
}