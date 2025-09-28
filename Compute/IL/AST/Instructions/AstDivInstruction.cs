using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version of DivInstruction
    /// </summary>
    [Instruction(Code.Div)]
    public class AstDivInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var right = ExpressionStack.Pop();
            var left = ExpressionStack.Pop();
            
            // Bitwise AND operation - result type is same as operands
            var resultType = left.Type;

            var divExpression = new BinaryExpression(left, BinaryOperatorType.Divide, right, resultType);

            ExpressionStack.Push(divExpression);

            return new NopStatement(); // Division doesn't produce a statement
        }
    }
}