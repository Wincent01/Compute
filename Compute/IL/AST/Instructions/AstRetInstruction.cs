using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST instruction for return statements
    /// </summary>
    [Instruction(Code.Ret)]
    public class AstRetInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            if (ExpressionStack.Count > 0)
            {
                var returnValue = ExpressionStack.Pop();
                return new ReturnStatement(returnValue);
            }
            else
            {
                return new ReturnStatement(null);
            }
        }
    }
}