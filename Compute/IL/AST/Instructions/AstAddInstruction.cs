using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version of AddInstruction
    /// </summary>
    [Instruction(Code.Add)]
    public class AstAddInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var right = ExpressionStack.Pop();
            var left = ExpressionStack.Pop();
            
            // Determine the result type (for now, use the left operand's type)
            // In a more sophisticated implementation, you'd implement type promotion rules
            var resultType = left.Type;
            
            var addExpression = new BinaryExpression(left, BinaryOperatorType.Add, right, resultType);
            
            ExpressionStack.Push(addExpression);
            
            return new NopStatement(); // Addition doesn't produce a statement, just pushes result onto stack
        }
    }
}