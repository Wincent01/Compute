using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version of CltInstruction for "compare less than" operations
    /// Handles both signed (Clt) and unsigned (Clt_Un) comparisons
    /// </summary>
    [Instruction(Code.Dup)]
    public class AstDupInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var value = ExpressionStack.Pop();

            ExpressionStack.Push(value);
            ExpressionStack.Push(value);

            return new NopStatement(); // Duplication doesn't produce a statement, just pushes result onto stack
        }
    }
}