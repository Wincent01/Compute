using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version of RemInstruction
    /// </summary>
    [Instruction(Code.Rem)]
    public class AstRemInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var right = ExpressionStack.Pop();
            var left = ExpressionStack.Pop();
            
            // Remainder operation - result type is same as operands
            var resultType = left.Type;
            
            var andExpression = new BinaryExpression(left, BinaryOperatorType.Modulo, right, resultType);
            
            ExpressionStack.Push(andExpression);
            
            return new NopStatement(); // Remainder doesn't produce a statement
        }
    }
}