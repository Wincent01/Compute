using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version of SubInstruction (subtraction)
    /// </summary>
    [Instruction(Code.Sub)]
    public class AstSubInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var right = ExpressionStack.Pop();
            var left = ExpressionStack.Pop();
            
            var resultType = left.Type;
            
            var subExpression = new BinaryExpression(left, BinaryOperatorType.Subtract, right, resultType);
            
            ExpressionStack.Push(subExpression);
            
            return new NopStatement();
        }
    }
}
