using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version for loading local variables
    /// </summary>
    [Instruction(Code.Ldstr)]
    public class AstLdstrInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var value = (string)Instruction.Operand;

            var expression = LiteralExpression.String(value);

            ExpressionStack.Push(expression);

            return new NopStatement(); // Loading constants doesn't produce a statement
        }
    }
}
