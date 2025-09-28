using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version of LdcInstruction for loading constants
    /// </summary>
    [Instruction(Code.Ldc_I4, Code.Ldc_I4_0, Code.Ldc_I4_1, Code.Ldc_I4_2, Code.Ldc_I4_3, 
                 Code.Ldc_I4_4, Code.Ldc_I4_5, Code.Ldc_I4_6, Code.Ldc_I4_7, Code.Ldc_I4_8, 
                 Code.Ldc_I4_M1, Code.Ldc_I4_S, Code.Ldc_R4, Code.Ldc_R8)]
    public class AstLdcInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            LiteralExpression literal = Instruction.OpCode.Code switch
            {
                Code.Ldc_I4_0 => LiteralExpression.Int32(0),
                Code.Ldc_I4_1 => LiteralExpression.Int32(1),
                Code.Ldc_I4_2 => LiteralExpression.Int32(2),
                Code.Ldc_I4_3 => LiteralExpression.Int32(3),
                Code.Ldc_I4_4 => LiteralExpression.Int32(4),
                Code.Ldc_I4_5 => LiteralExpression.Int32(5),
                Code.Ldc_I4_6 => LiteralExpression.Int32(6),
                Code.Ldc_I4_7 => LiteralExpression.Int32(7),
                Code.Ldc_I4_8 => LiteralExpression.Int32(8),
                Code.Ldc_I4_M1 => LiteralExpression.Int32(-1),
                Code.Ldc_I4 => LiteralExpression.Int32((int)Instruction.Operand),
                Code.Ldc_I4_S => LiteralExpression.Int32((sbyte)Instruction.Operand),
                Code.Ldc_R4 => LiteralExpression.Float32((float)Instruction.Operand),
                Code.Ldc_R8 => new LiteralExpression((double)Instruction.Operand, PrimitiveAstType.Float64),
                _ => throw new System.NotSupportedException($"Ldc opcode {Instruction.OpCode.Code} not supported")
            };
            
            ExpressionStack.Push(literal);
            
            return new Statements.NopStatement(); // Loading constants doesn't produce a statement
        }
    }
}