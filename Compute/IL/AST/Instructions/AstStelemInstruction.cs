using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST instruction for storing array elements (stelem)
    /// </summary>
    [Instruction(Code.Stelem_R4, Code.Stelem_Any, Code.Stelem_I4)]
    public class AstStelemInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var value = ExpressionStack.Pop();
            var index = ExpressionStack.Pop();
            var array = ExpressionStack.Pop();
            
            var elementType = array.Type;
            var arrayAccess = new ArrayAccessExpression(array, index, elementType);
            
            return new AssignmentStatement(arrayAccess, value);
        }
    }
}