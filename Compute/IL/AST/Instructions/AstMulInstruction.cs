using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST instruction for multiplication
    /// </summary>
    [Instruction(Code.Mul)]
    public class AstMulInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var right = ExpressionStack.Pop();
            var left = ExpressionStack.Pop();
            
            // Determine result type using simple promotion rules
            var resultType = DetermineResultType(left.Type, right.Type);
            
            var mulExpression = new BinaryExpression(left, BinaryOperatorType.Multiply, right, resultType);
            
            ExpressionStack.Push(mulExpression);
            
            return new NopStatement();
        }
        
        private AstType DetermineResultType(AstType leftType, AstType rightType)
        {
            // Simple type promotion - in practice you'd implement full .NET rules
            if (leftType.ClrType == typeof(double) || rightType.ClrType == typeof(double))
                return PrimitiveAstType.Float64;
            if (leftType.ClrType == typeof(float) || rightType.ClrType == typeof(float))
                return PrimitiveAstType.Float32;
            if (leftType.ClrType == typeof(long) || rightType.ClrType == typeof(long))
                return PrimitiveAstType.Int64;
            
            return leftType; // Default to left operand type
        }
    }
}