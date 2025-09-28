using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version of AndInstruction
    /// </summary>
    [Instruction(Code.And)]
    public class AstAndInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var right = ExpressionStack.Pop();
            var left = ExpressionStack.Pop();
            
            // Bitwise AND operation - result type is same as operands
            var resultType = left.Type;
            
            var andExpression = new BinaryExpression(left, BinaryOperatorType.BitwiseAnd, right, resultType);
            
            ExpressionStack.Push(andExpression);
            
            return new NopStatement(); // Bitwise AND doesn't produce a statement
        }
    }
}