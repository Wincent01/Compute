using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST-based version of BoxInstruction
    /// </summary>
    [Instruction(Code.Box)]
    public class AstBoxInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            return new NopStatement(); // No operation needed for boxing in AST
        }
    }
}