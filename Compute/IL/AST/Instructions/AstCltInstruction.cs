using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version of CltInstruction for "compare less than" operations
    /// Handles both signed (Clt) and unsigned (Clt_Un) comparisons
    /// </summary>
    [Instruction(Code.Clt, Code.Clt_Un)]
    public class AstCltInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var right = ExpressionStack.Pop();
            var left = ExpressionStack.Pop();
            
            // Create a less-than comparison expression
            // Note: The comparison result is always int32 (0 or 1) as per IL specification
            var comparison = new BinaryExpression(left, BinaryOperatorType.LessThan, right, PrimitiveAstType.Int32);
            
            ExpressionStack.Push(comparison);
            
            return new NopStatement(); // Comparison doesn't produce a statement, just pushes result onto stack
        }
    }
}