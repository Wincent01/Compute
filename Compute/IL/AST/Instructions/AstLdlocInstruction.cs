using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version for loading local variables
    /// </summary>
    [Instruction(Code.Ldloc, Code.Ldloc_0, Code.Ldloc_1, Code.Ldloc_2, Code.Ldloc_3, Code.Ldloc_S)]
    public class AstLdlocInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var index = Instruction.OpCode.Code switch
            {
                Code.Ldloc_0 => 0,
                Code.Ldloc_1 => 1,
                Code.Ldloc_2 => 2,
                Code.Ldloc_3 => 3,
                Code.Ldloc => ((VariableDefinition)Instruction.Operand).Index,
                Code.Ldloc_S => ((VariableDefinition)Instruction.Operand).Index,
                _ => throw new System.NotSupportedException($"Ldloc opcode {Instruction.OpCode.Code} not supported")
            };
            
            if (!Variables.TryGetValue(index, out IExpression? variable) || variable == null)
                throw new System.Exception($"Variable at index {index} not found");
            
            ExpressionStack.Push(variable);
            
            return new NopStatement(); // Loading variables doesn't produce a statement
        }
    }
}