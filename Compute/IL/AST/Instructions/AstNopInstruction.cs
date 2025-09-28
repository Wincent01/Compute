using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version of NopInstruction
    /// </summary>
    [Instruction(Code.Nop)]
    public class AstNopInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            return new NopStatement(); // Nop instruction doesn't produce a statement
        }
    }
}