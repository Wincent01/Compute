using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version for storing to local variables
    /// </summary>
    [Instruction(Code.Stloc, Code.Stloc_0, Code.Stloc_1, Code.Stloc_2, Code.Stloc_3, Code.Stloc_S)]
    public class AstStlocInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var value = ExpressionStack.Pop();
            
            var index = Instruction.OpCode.Code switch
            {
                Code.Stloc_0 => 0,
                Code.Stloc_1 => 1,
                Code.Stloc_2 => 2,
                Code.Stloc_3 => 3,
                Code.Stloc => ((VariableDefinition)Instruction.Operand).Index,
                Code.Stloc_S => ((VariableDefinition)Instruction.Operand).Index,
                _ => throw new System.NotSupportedException($"Stloc opcode {Instruction.OpCode.Code} not supported")
            };
            
            var variable = GetVariable(index);
            
            return new AssignmentStatement(variable, value);
        }
    }
}