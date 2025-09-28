using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version of PopInstruction
    /// </summary>
    [Instruction(Code.Pop)]
    public class AstPopInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var value = ExpressionStack.Pop();

            if (value is FunctionCallExpression funcCall)
            {
                // If the popped value is a function call, we can treat it as a standalone statement
                return new ExpressionStatement(funcCall);
            }

            return new NopStatement(); // Pop doesn't produce a statement, just removes the top stack element
        }
    }
}