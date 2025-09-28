using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST instruction for conditional branches (comparison + branch)
    /// </summary>
    [Instruction(Code.Bge, Code.Bge_S, Code.Bge_Un, Code.Bge_Un_S,
                 Code.Bgt, Code.Bgt_S, Code.Bgt_Un, Code.Bgt_Un_S,
                 Code.Ble, Code.Ble_S, Code.Ble_Un, Code.Ble_Un_S,
                 Code.Blt, Code.Blt_S, Code.Blt_Un, Code.Blt_Un_S,
                 Code.Beq, Code.Beq_S, Code.Bne_Un, Code.Bne_Un_S,
                 Code.Brfalse, Code.Brfalse_S, Code.Brtrue, Code.Brtrue_S,
                 Code.Br, Code.Br_S)]
    public class AstBranchInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var opCode = Instruction.OpCode.Code;

            if (opCode == Code.Br || opCode == Code.Br_S)
            {
                // Unconditional branch
                return new BranchStatement(null, Instruction.Operand is Instruction target ? target.Offset : -1);
            }

            // Handle simple boolean branches first
            if (opCode == Code.Brfalse || opCode == Code.Brfalse_S)
            {
                var condition = ExpressionStack.Pop();
                // Branch if false means: if (!condition) return;
                var notCondition = new UnaryExpression(UnaryOperatorType.LogicalNot, condition, PrimitiveAstType.Bool);
                return new BranchStatement(notCondition, Instruction.Operand is Instruction target ? target.Offset : -1);
            }

            if (opCode == Code.Brtrue || opCode == Code.Brtrue_S)
            {
                var condition = ExpressionStack.Pop();
                // Branch if true means: if (condition) return;
                return new BranchStatement(condition, Instruction.Operand is Instruction target ? target.Offset : -1);
            }

            // Handle comparison branches
            if (ExpressionStack.Count >= 2)
            {
                var right = ExpressionStack.Pop();
                var left = ExpressionStack.Pop();

                var comparisonOp = GetComparisonOperator(opCode);
                var comparison = new BinaryExpression(left, comparisonOp, right, PrimitiveAstType.Bool);

                return new BranchStatement(comparison, Instruction.Operand is Instruction target ? target.Offset : -1);
            }
            else
            {
                throw new System.InvalidOperationException("Not enough values on the expression stack for comparison branch.");
            }
        }
        
        private static BinaryOperatorType GetComparisonOperator(Code opCode)
        {
            return opCode switch
            {
                Code.Bge or Code.Bge_S or Code.Bge_Un or Code.Bge_Un_S => BinaryOperatorType.GreaterThanOrEqual,
                Code.Bgt or Code.Bgt_S or Code.Bgt_Un or Code.Bgt_Un_S => BinaryOperatorType.GreaterThan,
                Code.Ble or Code.Ble_S or Code.Ble_Un or Code.Ble_Un_S => BinaryOperatorType.LessThanOrEqual,
                Code.Blt or Code.Blt_S or Code.Blt_Un or Code.Blt_Un_S => BinaryOperatorType.LessThan,
                Code.Beq or Code.Beq_S => BinaryOperatorType.Equal,
                Code.Bne_Un or Code.Bne_Un_S => BinaryOperatorType.NotEqual,
                _ => BinaryOperatorType.Equal
            };
        }
    }
}